using Acxess.Catalog.Domain.Errors;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Catalog.Application.Features.SellingPlans.Commands.UpdateSellingPlan;

public class UpdateSellingPlanHandler(
    CatalogModuleContext context
) : IRequestHandler<UpdateSellingPlanCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateSellingPlanCommand request, CancellationToken cancellationToken)
    {
        var sellingPlan = await context.SellingPlans
            .Include(sp => sp.AccessTiers)
            .FirstOrDefaultAsync(sp => sp.IdSellingPlan == request.IdSellingPlan, cancellationToken);

        if (sellingPlan is null) return Result<string>.Failure(SellingPlansErrors.NotFound);

        sellingPlan.Update(
            request.Name, 
            request.TotalMembers,
            request.Duration,
            request.DurationSubscriptionUnit,
            request.Price,
            request.IsActive
        );

        sellingPlan.SyncAccessTiers(request.AccessTiersIds);

        await context.SaveChangesAsync(cancellationToken);

        return "Plan actualizado correctamente";
    }
}
