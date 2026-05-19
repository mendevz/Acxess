using Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlans;
using Acxess.Catalog.Domain.Errors;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.Enums;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlanById;

public class GetSellingPlanByIdHandler(CatalogModuleContext context ) : IRequestHandler<GetSellingPlanByIdQuery, Result<SellingPlanDto>>
{
    public async Task<Result<SellingPlanDto>> Handle(GetSellingPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var query = context.SellingPlans.AsNoTracking()
            .Where(p => p.IdSellingPlan == request.IdSellingPlan);

        var planData = await context.SellingPlans.AsNoTracking()
        .Where(p => p.IdSellingPlan == request.IdSellingPlan)
        .Select(p => new
        {
            p.IdSellingPlan,
            p.Name,
            p.TotalMembers,
            p.DurationInValue,
            p.DurationUnit,
            p.Price,
            p.IsActive,
            Tiers = p.AccessTiers.Select(link => new { link.IdAccessTier, link.AccessTier.Name }).ToList()
        })
        .FirstOrDefaultAsync(cancellationToken);

        if (planData is null)
        {
            return Result<SellingPlanDto>.Failure(SellingPlansErrors.NotFound);
        }

        var dto = new SellingPlanDto(
            planData.IdSellingPlan,
            planData.Name,
            planData.TotalMembers,
            planData.DurationInValue,
            planData.DurationUnit,
            planData.Price,
            planData.IsActive,
            [.. planData.Tiers.Select(t => t.IdAccessTier)],
            string.Join(", ", planData.Tiers.Select(t => t.Name)),
            $"{planData.DurationInValue} {planData.DurationUnit.ToFriendlyName(planData.DurationInValue)}"
        );

        return dto;
    }
}
