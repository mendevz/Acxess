using Acxess.Shared.Enums;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.SellingPlans.Commands.NewSellingPlan;

public record NewSellingPlanCommand
(
    int IdTenant,
    int TotalMembers,
    int Duration,
    DurationSubscriptionUnit DurationSubscriptionUnit,
    string Name,
    decimal Price,
    int CreatedBy,
    List<int> AccessTiersIds
) : IRequest<Result<string>>;
