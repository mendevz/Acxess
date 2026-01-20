using Acxess.Marketing.Application.Features.Promotions.DTOs;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Marketing.Application.Features.Promotions.Queries.GetPromotions;

public record GetPromotionsQuery(bool IncludeInactives) :  IRequest<Result<List<PromotionDto>>>;
