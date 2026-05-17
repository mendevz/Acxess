using Acxess.Catalog.Domain.Constants;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.Enums;
using Acxess.Shared.IntegrationServices.Catalog;
using Acxess.Shared.ResultManager;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Catalog.Infrastructure.Services;

public class CatalogIntegrationService(CatalogModuleContext context) : ICatalogIntegrationService
{
    public async Task<PlanIntegrationDto?> GetPlanInfoAsync(int planId, CancellationToken ct = default)
    {
        return await context.SellingPlans
            .AsNoTracking()
            .Where(p => p.IdSellingPlan == planId)
            .Select(p => new PlanIntegrationDto(
                p.IdSellingPlan, 
                p.Name, 
                p.Price, 
                p.DurationInValue, 
                p.DurationUnit))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Result<List<AddOnIntegrationDto>>> GetAddOnPriceBatchAsync(List<int> addOnIds,
        CancellationToken ct = default)
    {
        if ( addOnIds.Count == 0)
        {
            return Result<List<AddOnIntegrationDto>>.Success([]);
        }
        
        var uniqueIds = addOnIds.Distinct().ToList();
        
        var addOns = await context.AddOns
            .AsNoTracking()
            .Where(a => uniqueIds.Contains(a.IdAddOn))
            .Select(a => new AddOnIntegrationDto(
                a.IdAddOn,
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