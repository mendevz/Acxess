using Acxess.Catalog.Domain.Entities;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.Enums;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.SellingPlans.Commands;

public record NewSellingPlanCommand
(
    int TotalMembers,
    int Duration,
    DurationSubscriptionUnit DurationSubscriptionUnit,
    string Name,
    decimal Price,
    int CreatedBy,
    List<int> AccessTiersIds
) : IRequest<Result<string>>, ITenantRequest
{
    public int IdTenant { get; set; }
}
public class NewSellingPlansHandler(
    CatalogModuleContext context,
    ITimeService timeService) : IRequestHandler<NewSellingPlanCommand, Result<string>>
{
    public async Task<Result<string>> Handle(NewSellingPlanCommand request, CancellationToken cancellationToken)
    {
        var utcNow = timeService.GetUtcNow();

        var sellingPlan = SellingPlan.Create(
           request.IdTenant,
           request.Name,
           request.TotalMembers,
           request.Duration,
           request.DurationSubscriptionUnit,
           request.Price,
           request.CreatedBy,
           utcNow
       );
       
       sellingPlan.AddAccessTiers(request.AccessTiersIds);

       context.SellingPlans.Add(sellingPlan);

       await context.SaveChangesAsync(cancellationToken);   

       return "Plan guardado correctamente";
    }
}
