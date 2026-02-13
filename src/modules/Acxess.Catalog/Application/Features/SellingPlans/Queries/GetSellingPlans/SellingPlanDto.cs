using Acxess.Catalog.Domain.Enums;

public record SellingPlanDto(
    int IdSellingPlan,
    string Name,
    int TotalMembers,
    int DurationInValue,
    DurationUnit DurationUnit, 
    decimal Price,
    bool IsActive,
    List<int> AccessTiersIds,
    string AccessTiers,
    string DurationDescription
);