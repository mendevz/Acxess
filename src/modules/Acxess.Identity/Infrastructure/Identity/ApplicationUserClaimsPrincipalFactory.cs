using System.Security.Claims;
using System.Text.Json;
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
        
      var availableTenants = await context.Set<TenantsUsers>()
            .Include(tu => tu.Tenant)
            .AsSplitQuery()
            .Where(tu => tu.UserNumber == user.UserNumber && tu.Tenant.IsActive)
            .Select(tu => new { id = tu.Tenant.IdTenant, name = tu.Tenant.Name })
            .ToListAsync();
        
      if (availableTenants.Count != 0)
      {
          var firstTenant = availableTenants.First();
          identity.AddClaim(new Claim("IdTenant", firstTenant.id.ToString()));
          identity.AddClaim(new Claim("TenantName", firstTenant.name));

          var tenantsJson = JsonSerializer.Serialize(availableTenants);
          identity.AddClaim(new Claim("AvailableTenants", tenantsJson));
      }
        identity.AddClaim(new Claim("UserNumber", user.UserNumber.ToString()));
        identity.AddClaim(new Claim("UserName", user.UserName ?? string.Empty));
        identity.AddClaim(new Claim("FullName", user.FullName ?? string.Empty));
        identity.AddClaim(new Claim("Email", user.Email ?? string.Empty));

        return identity;
    }
}
