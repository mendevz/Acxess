using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Marketing.Application.Features.Promotions.Queries.GetActiveCouponPromotions;

public record GetActiveCouponPromotionsQuery : IRequest<Result<List<PromotionSelectDto>>>;