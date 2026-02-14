using Acxess.Catalog.Domain.Constants;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.IntegrationEvents.Catalog;
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
                (int)p.DurationUnit))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Result<AddOnIntegrationDto>> GetAddOnPriceAsync(int addOnId, CancellationToken ct = default)
    {
        var addOn = await context.AddOns
            .AsNoTracking()
            .Where(a => a.IdAddOn == addOnId)
            .Select(a => new AddOnIntegrationDto(
                    a.Name,
                    a.Price
                ))
            .FirstOrDefaultAsync(ct);

        return addOn ?? Result<AddOnIntegrationDto>.Failure("NotFound", "AddOn not found.");
    }

    public async Task<Result<List<string>>> GetAddOnNamesAsync(List<int> addOnIds, bool includesInscription, CancellationToken ct = default)
    {
        
        var query = context.AddOns
            .AsNoTracking()
            .Where(a => addOnIds.Contains(a.IdAddOn));
        
        if (!includesInscription)
        {
            query = query.Where(a => a.AddOnKey != AddOnDefaults.Inscription.Key);
        }
        
        var addOnNames = await query
            .Select(a => a.Name)
            .ToListAsync(ct);

        return addOnNames;

    }
}