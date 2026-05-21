using Acxess.Shared.Enums;
using Acxess.Shared.ResultManager;

namespace Acxess.Shared.IntegrationServices.Catalog;

public interface ICatalogIntegrationService
{
    Task<Result<PlanIntegrationDto>> GetPlanInfoAsync(int planId, CancellationToken ct = default);
    Task<List<AddOnIntegrationDto>> GetAddOnPriceBatchAsync(List<int> addOnIds, CancellationToken ct = default);
    Task<List<string>> GetAddOnNamesAsync(List<int> addOnIds, CancellationToken ct = default);
}

public record PlanIntegrationDto(
    int Id,
    string Name, 
    decimal Price, 
    int Duration, 
    DurationSubscriptionUnit DurationUnit,
    int TotalMembers);

public record AddOnIntegrationDto(int Id, string Name, decimal Price);