
namespace Acxess.Catalog.Application.Features.AccessTiers.DTOs;

public record AccessTierDto
(
    int IdAccessTier,
    string Name,
    string Description,
    bool IsActive
);
