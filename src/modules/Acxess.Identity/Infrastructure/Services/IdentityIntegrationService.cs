using Acxess.Identity.Infrastructure.Persistence;
using Acxess.Shared.IntegrationServices;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Identity.Infrastructure.Services;

public class IdentityIntegrationService(IdentityModuleContext dbContext) : IIdentityIntegrationService
{
    public async Task<List<TenantTimeZoneInfo>> GetActiveTenantsWithTimeZonesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => new TenantTimeZoneInfo(t.IdTenant, t.TimeZoneId))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TenantAdminContactIntegrationDto>> GetTenantAdminsContactsAsync(List<int> tenantIds, CancellationToken cancellationToken = default)
    {
        if (tenantIds == null || tenantIds.Count == 0) return [];

        var tenantsData = await dbContext.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.IdTenant))
            .Select(t => new TenantAdminContactIntegrationDto(
                t.IdTenant,
                t.Name,
                t.TenantsUsers
                    .Where(tu => tu.User.PhoneNumber != null && tu.User.PhoneNumber != "")
                    .Select(tu => new AdminContactIntegrationDto(tu.User.FullName, tu.User.PhoneNumber!))
                    .ToList()
            ))
            .ToListAsync(cancellationToken);

        return tenantsData;
    }
}
