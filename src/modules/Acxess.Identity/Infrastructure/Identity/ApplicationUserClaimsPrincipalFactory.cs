using System.Security.Claims;
using Acxess.Identity.Domain.Entities;
using Acxess.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Acxess.Identity.Infrastructure.Identity;

public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor,
    IdentityModuleContext  context) 
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.IdTenant.HasValue)
        {
            identity.AddClaim(new Claim("IdTenant", user.IdTenant.Value.ToString()));
            
            var tenantName = await context.Set<Tenant>()
                .Where(t => t.IdTenant == user.IdTenant.Value)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(tenantName))
            {
                identity.AddClaim(new Claim("TenantName", tenantName));
            }
        }

        identity.AddClaim(new Claim("UserNumber", user.UserNumber.ToString()));
        identity.AddClaim(new Claim("UserName", user.UserName ?? string.Empty));
        identity.AddClaim(new Claim("FullName", user.FullName ?? string.Empty));
        identity.AddClaim(new Claim("Email", user.Email ?? string.Empty));

        return identity;
    }
}
