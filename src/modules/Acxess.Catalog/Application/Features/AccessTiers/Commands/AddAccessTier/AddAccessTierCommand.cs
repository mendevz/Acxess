using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AccessTiers.Commands.AddAccessTier;

public record AddAccessTierCommand
(
    string Name,
    string? Description,
    int? TenantId = null
): IRequest<Result<string>>;
