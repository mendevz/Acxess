using Acxess.Identity.Domain.Entities;
using Acxess.Identity.Infrastructure.Persistence;
using Acxess.Shared.Constants;
using Acxess.Shared.IntegrationEvents.Identity;
using Acxess.Shared.ResultManager;
using Amazon.Util.Internal;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Acxess.Identity.Application.Features.Tenants.Commands.RegisterTenant;

public class RegisterTenantHandler(
    IdentityModuleContext context,
    UserManager< Domain.Entities.ApplicationUser> userManager,
    TimeProvider timeProvider,
    IMediator mediator
) : IRequestHandler<RegisterTenantCommand, Result>
{
    public async Task<Result> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var tenant = Tenant.Create(request.NameTenant, request.TimeZoneId, utcNow);

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync(cancellationToken);

        var idTenant = tenant.IdTenant;

        var userAdmin = Domain.Entities.ApplicationUser.Create(
            request.UsernameAdmin,
            request.EmailAdmin ?? string.Empty,
            request.FullNameAdmin
        );

        var createUserResult = await userManager.CreateAsync(userAdmin, request.PasswordAdmin);

        if (!createUserResult.Succeeded)
        {
            var errors = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
            return Result.Failure("ApplicationUser.NotCreated", $"Failed to create admin user: {errors}");
        }

        var addToRoleResult = await userManager.AddToRoleAsync(userAdmin, ApplicationRoles.SuperAdmin);

        if (!addToRoleResult.Succeeded)
        {
            var errors = string.Join("; ", addToRoleResult.Errors.Select(e => e.Description));
            return Result.Failure("ApplicationUser.RoleNotAssigned", $"Failed to assign role to admin user: {errors}");
        }
        
        var tenantUser = TenantsUsers.Create(tenant.IdTenant, userAdmin.UserNumber);
        context.TenantsUsers.Add(tenantUser);
        await context.SaveChangesAsync(cancellationToken);

        await mediator.Publish(new TenantCreatedIntegrationEvent(idTenant), cancellationToken);

        return Result.Success();
    }
}
