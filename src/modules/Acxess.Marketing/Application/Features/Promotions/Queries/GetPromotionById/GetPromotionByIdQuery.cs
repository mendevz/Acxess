using Acxess.Marketing.Application.Features.Promotions.DTOs;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Marketing.Application.Features.Promotions.Queries.GetPromotionById;

public record GetPromotionByIdQuery (int IdPromotion) :  IRequest<Result<PromotionDto>>;    