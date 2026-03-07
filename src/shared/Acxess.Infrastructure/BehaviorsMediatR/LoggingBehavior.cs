using System.Diagnostics;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Acxess.Infrastructure.BehaviorsMediatR;

public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger) 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        logger.LogInformation("Executing Command/Query: {RequestName}", requestName);
        
        var timer = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            
            timer.Stop();
            
            if (typeof(Result).IsAssignableFrom(typeof(TResponse)))
            {
                var isFailureProperty = typeof(TResponse).GetProperty("IsFailure") ?? typeof(Result).GetProperty("IsFailure");
                var isFailure = (bool)(isFailureProperty?.GetValue(response) ?? false);

                if (isFailure)
                {
                    var errorProperty = typeof(TResponse).GetProperty("Error") ?? typeof(Result).GetProperty("Error");
                    var error = errorProperty?.GetValue(response);

                    logger.LogWarning("Command/Query rejected by Domain: {RequestName} in {ElapsedMilliseconds} ms. Details: {@Error}", 
                        requestName, timer.ElapsedMilliseconds, error);
                        
                    return response;
                }
            }
            
            logger.LogInformation("Completed: {RequestName} in {ElapsedMilliseconds} ms", 
                requestName, timer.ElapsedMilliseconds);
                
            return response;
        }
        catch (Exception ex)
        {
            timer.Stop();
            
            logger.LogError(ex, "System Exception in {RequestName} after {ElapsedMilliseconds} ms", 
                requestName, timer.ElapsedMilliseconds);
                
            throw; 
        }
    }
}