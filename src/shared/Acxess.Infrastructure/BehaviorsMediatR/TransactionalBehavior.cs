using System.Transactions;
using Acxess.Shared.Exceptions;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Infrastructure.BehaviorsMediatR;
/// <summary>
/// A transactional behavior for MediatR requests that ensures operations are executed within a transaction scope.
/// </summary>
public class TransactionalBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!typeof(Result).IsAssignableFrom(typeof(TResponse)))
        {
            return await next(); 
        }

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            var response = await next();
            var result = response as Result;
            if (result is not null && result.IsSuccess)
            {
                scope.Complete();
            }

            return response;
        }
        catch(IntegrationEventException ex)
        {
            var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(Error)]) 
                                ?? typeof(Result).GetMethod("Failure", [typeof(Error)]);

            if (failureMethod is not null)
            {
                return (TResponse)failureMethod.Invoke(null, [ex.Error])!;
            }

            throw;
        }
        catch (Exception )
        {
            throw;
        }
    }
}
