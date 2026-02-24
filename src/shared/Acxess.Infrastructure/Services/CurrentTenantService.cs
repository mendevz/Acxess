using Acxess.Shared.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Acxess.Infrastructure.Services;

public class CurrentTenantService(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
{
    public int? Id  {
        get{
            var user = httpContextAccessor.HttpContext?.User;

            var idTenantClaim = user?.FindFirst("IdTenant")?.Value;

            if (int.TryParse(idTenantClaim, out var tenantId))
                return tenantId;

            return null;
        }
    }

    public bool IsAvailable => Id.HasValue;
}
