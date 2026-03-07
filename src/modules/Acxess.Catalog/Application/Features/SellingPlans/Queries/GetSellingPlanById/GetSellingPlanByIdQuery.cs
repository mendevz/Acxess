using System;
using Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlans;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlanById;

public record GetSellingPlanByIdQuery(int IdSellingPlan) : IRequest<Result<SellingPlanDto>>;

