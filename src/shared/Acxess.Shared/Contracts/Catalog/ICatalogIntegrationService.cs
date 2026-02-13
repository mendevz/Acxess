using Acxess.Shared.ResultManager;

namespace Acxess.Shared.Contracts.Catalog;

public interface ICatalogIntegrationService
{
    Task<PlanIntegrationDto?> GetPlanInfoAsync(int planId, CancellationToken ct = default);
    Task<Result<AddOnIntegrationDto>> GetAddOnPriceAsync(int addOnId, CancellationToken ct = default);
}

public record PlanIntegrationDto(int Id, string Name, decimal Price, int Duration, int DurationUnit);

public record AddOnIntegrationDto(string Name, decimal Price);