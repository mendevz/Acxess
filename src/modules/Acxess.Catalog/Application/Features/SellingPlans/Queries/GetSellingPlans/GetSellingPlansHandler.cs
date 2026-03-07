using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.Enums;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlans;

public class GetSellingPlansHandler(
    CatalogModuleContext context
) : IRequestHandler<GetSellingPlanQuery, Result<List<SellingPlanDto>>>
{
    public async Task<Result<List<SellingPlanDto>>> Handle(GetSellingPlanQuery request, CancellationToken cancellationToken)
    {
        var query = context.SellingPlans.AsNoTracking();

        if (!request.IncludeInactives)
        {
            query = query.Where(p => p.IsActive);
        }

        var items = await query.Select(p=> new SellingPlanDto(
            p.IdSellingPlan,
            p.Name,
            p.TotalMembers,
            p.DurationInValue,
            p.DurationUnit,
            p.Price,
            p.IsActive,
            p.AccessTiers.Select(link => link.IdAccessTier).ToList(),
            string.Join(", ", p.AccessTiers.Select(link => link.AccessTier.Name)),
            $"{p.DurationUnit.ToFriendlyName( p.DurationInValue)}"
        ))
        .ToListAsync(cancellationToken);

        return items;
    }
}
