using Acxess.Marketing.Domain.Enums;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Marketing.Application.Features.Promotions.Commands.UpdatePromotion;

public record UpdatePromotionCommand
(
    int IdPromotion,    
    string Name,
    DiscountType DiscountType,
    decimal Discount,
    bool RequiresCoupon,
    bool AutoApply,
    bool IsActive,
    DateTime? AvailableFrom,
    DateTime? AvailableTo) : IRequest<Result<string>>;