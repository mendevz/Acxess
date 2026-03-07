using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AccessTiers.Commands.AddAccessTier;

public record AddAccessTierCommand
(
    string Name,
    int TenantId,
    string? Description): IRequest<Result<string>>;
