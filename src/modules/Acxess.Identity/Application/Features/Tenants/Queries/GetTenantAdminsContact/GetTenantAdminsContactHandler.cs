using Acxess.Identity.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Identity.Application.Features.Tenants.Queries.GetTenantAdminsContact;

public class GetTenantAdminsContactHandler(
    IdentityModuleContext dbContext) : IRequestHandler<GetTenantAdminsContactQuery, Result<List<TenantContactDataDto>>>
{
    public async Task<Result<List<TenantContactDataDto>>> Handle(GetTenantAdminsContactQuery request, CancellationToken cancellationToken)
    {
        if (request.TenantIds == null || request.TenantIds.Count == 0)
        {
            return new List<TenantContactDataDto>();
        }

        var tenantsData = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => request.TenantIds.Contains(t.IdTenant))
            .Select(t => new TenantContactDataDto(
                t.IdTenant,
                t.Name,
                t.TenantsUsers
                    .Where(tu => tu.User.PhoneNumber != null && tu.User.PhoneNumber != "")
                    .Select(tu => new TenantAdminContactDto(tu.User.FullName, tu.User.PhoneNumber!))
                    .ToList()
            ))
            .ToListAsync(cancellationToken);

        return tenantsData;
    }
}
