using Acxess.Shared.Abstractions;
using MediatR;

namespace Acxess.Infrastructure.BehaviorsMediatR;

public class TenantPipelineBehavior<TRequest, TResponse>(ICurrentTenant currentTenant)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (request is ITenantRequest tenantRequest)
        {
            if (tenantRequest.IdTenant == 0)
            {
                // Web/API. Extract from JWT / Cookie
                if (currentTenant.IsAvailable && currentTenant.Id.HasValue)
                {
                    tenantRequest.IdTenant = currentTenant.Id.Value;
                }
                else
                {
                    throw new UnauthorizedAccessException("Se requiere un TenantId válido para ejecutar esta acción.");
                }
            }
        }

        return await next();
    }
}
