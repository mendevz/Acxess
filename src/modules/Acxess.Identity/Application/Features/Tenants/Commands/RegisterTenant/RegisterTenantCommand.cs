using Acxess.Shared.ResultManager;
using MediatR;
namespace Acxess.Identity.Application.Features.Tenants.Commands.RegisterTenant;

public record RegisterTenantCommand(
    string NameTenant,
    string UsernameAdmin,
    string FullNameAdmin,
    string PasswordAdmin,
    string TimeZoneId,
    string? EmailAdmin
) : IRequest<Result>;
