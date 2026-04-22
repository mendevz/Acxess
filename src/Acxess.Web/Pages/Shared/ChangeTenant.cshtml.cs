using System.Security.Claims;
using Acxess.Identity.Domain.Entities;
using Acxess.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Web.Pages.Shared;

[Authorize]
public class ChangeTenantModel(
    SignInManager<ApplicationUser> signInManager,
    IdentityModuleContext context) : PageModel
{
    public async Task<IActionResult> OnPostAsync(int newTenantId, string returnUrl)
    {
        var user = await signInManager.UserManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var tenantToSwitch = await context.Set<TenantsUsers>()
            .Where(tu => tu.UserNumber == user.UserNumber 
                         && tu.IdTenant == newTenantId 
                         && tu.Tenant.IsActive)
            .Select(tu => tu.Tenant) // EF hará el JOIN automáticamente aquí
            .OrderByDescending(t => t.IdTenant)
            .FirstOrDefaultAsync();

        if (tenantToSwitch == null) 
            return Forbid(); 

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        var oldIdClaim = identity.FindFirst("IdTenant");
        var oldNameClaim = identity.FindFirst("TenantName");
        
        if (oldIdClaim != null) identity.RemoveClaim(oldIdClaim);
        if (oldNameClaim != null) identity.RemoveClaim(oldNameClaim);

        identity.AddClaim(new Claim("IdTenant", tenantToSwitch.IdTenant.ToString()));
        identity.AddClaim(new Claim("TenantName", tenantToSwitch.Name));

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme, 
            new ClaimsPrincipal(identity));

        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
}