using Acxess.Catalog.Domain.Entities;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.SellingPlans.Commands.NewSellingPlan;

public class NewSellingPlansHandler(CatalogModuleContext context) : IRequestHandler<NewSellingPlanCommand, Result<string>>
{
    
    public async Task<Result<string>> Handle(NewSellingPlanCommand request, CancellationToken cancellationToken)
    {

       var sellingPlan = SellingPlan.Create(
           request.IdTenant,
           request.Name,
           request.TotalMembers,
           request.Duration,
           request.DurationSubscriptionUnit,
           request.Price,
           request.CreatedBy
       );

       if (request.AccessTiersIds.Count != 0)
       {
           foreach (var tierId in request.AccessTiersIds)
           {
               sellingPlan.AddAccessTier(tierId);
           }
       }

       context.SellingPlans.Add(sellingPlan);

       await context.SaveChangesAsync(cancellationToken);   

       return "Plan guardado correctamente";
    }
}
