using System.Transactions;
using Acxess.Shared.Exceptions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Acxess.Infrastructure.BehaviorsMediatR;
/// <summary>
/// A transactional behavior for MediatR requests that ensures operations are executed within a transaction scope.
/// </summary>
public class TransactionalBehavior<TRequest, TResponse>(ILogger<TransactionalBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        if (!typeof(Result).IsAssignableFrom(typeof(TResponse)))  return await next(); 
        if (!requestName.EndsWith("Command")) return await next();

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            var response = await next();
            if (response is Result { IsSuccess: true })
            {
                scope.Complete();
            }
            else
            {
                logger.LogWarning("Request {RequestName} returned a failed Result. Transaction will be rolled back.", requestName);
            }
            return response;
        }
        catch(IntegrationEventException ex)
        {
            logger.LogWarning(ex, 
                "Integration event failed for {RequestName}. Rolling back transaction and converting to failed Result. ErrorCode: {ErrorCode} ErrorMessage: {ErrorMessage}", 
                requestName, ex.Error?.Code, ex.Message);
            var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(Error)]) 
                                ?? typeof(Result).GetMethod("Failure", [typeof(Error)]);

            if (failureMethod is not null)
            {
                return (TResponse)failureMethod.Invoke(null, [ex.Error])!;
            }

            throw;
        }
    }
}
