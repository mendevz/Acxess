using System.Diagnostics;
using System.Security.Claims;
using Acxess.Shared.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using IResult = Acxess.Shared.ResultManager.IResult;

namespace Acxess.Infrastructure.BehaviorsMediatR;

public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentTenant currentTenant,                 
    IHttpContextAccessor httpContextAccessor) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        var correlationId = httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
        var tenantId = currentTenant.IsAvailable ? currentTenant.Id.ToString() : "None Tenant";
        
        var logProperties = new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "TenantId", tenantId! },
            { "UserId", userId }
        };

        using (logger.BeginScope(logProperties))
        {
            logger.LogInformation("Executing Command/Query: {RequestName} with Payload: {@Request}", requestName, request);
        
            var timer = Stopwatch.StartNew();
            
            try
            {
                var response = await next();
            
                timer.Stop();
            
                if (response is IResult { IsFailure: true } result)
                {
                    logger.LogWarning(
                        "Command/Query rejected by Domain: {RequestName} in {ElapsedMilliseconds} ms. Details: {@Error}", 
                        requestName, timer.ElapsedMilliseconds, result.Error);
                    
                    return response;
                }
            
                logger.LogInformation("Command/Query Completed: {RequestName} in {ElapsedMilliseconds} ms", 
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
}