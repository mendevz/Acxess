using Acxess.Catalog.Domain.Constants;
using Acxess.Catalog.Domain.Errors;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.IntegrationServices;
using Acxess.Shared.ResultManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Catalog.Infrastructure.Services;

public class CatalogIntegrationService(
    CatalogModuleContext context,
    ILogger<CatalogIntegrationService> logger) : ICatalogIntegrationService
{
    public async Task<Result<PlanIntegrationDto>> GetPlanInfoAsync(int planId, CancellationToken ct = default)
    {
        var sellingPlan = await context.SellingPlans
            .AsNoTracking()
            .Where(p => p.IdSellingPlan == planId)
            .Select(p => new PlanIntegrationDto(
                p.IdSellingPlan, 
                p.Name, 
                p.Price, 
                p.DurationInValue, 
                p.DurationUnit,
                p.TotalMembers))
            .FirstOrDefaultAsync(ct);

        if (sellingPlan is null)
        {
            logger.LogWarning( "SellingPlanId: {SellingPlanId} not found or inactive.", planId);
            return Result<PlanIntegrationDto>.Failure(SellingPlansErrors.NotFound);
        }

        return sellingPlan;
    }

    public async Task<List<AddOnIntegrationDto>> GetAddOnPriceBatchAsync(List<int> addOnIds,
        CancellationToken ct = default)
    {
        if (addOnIds.Count == 0) return [];
        
        var uniqueIds = addOnIds.Distinct().ToList();
        
        var addOns = await context.AddOns
            .AsNoTracking()
            .Where(a => uniqueIds.Contains(a.IdAddOn))
            .Select(a => new AddOnIntegrationDto(
                a.IdAddOn,
                a.AddOnKey,
                a.Name,
                a.Price
            ))
            .ToListAsync(ct);
        
        return addOns;
    }

    public async Task<List<string>> GetAddOnNamesAsync(List<int> addOnIds, CancellationToken ct = default)
    {
        var query = context.AddOns
            .AsNoTracking()
            .Where(a => addOnIds.Contains(a.IdAddOn));
       
        query = query.Where(a => a.AddOnKey != AddOnDefaults.Inscription.Key);
        
        var addOnNames = await query
            .Select(a => a.Name)
            .ToListAsync(ct);

        return addOnNames;
    }
}