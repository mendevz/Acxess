using System.Diagnostics;
using MediatR;

namespace Acxess.Infrastructure.BehaviorsMediatR;

public class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public static readonly ActivitySource ActivitySource = new("Acxess.Application");
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(typeof(TRequest).Name);
        return await next();
    }
}