using Acxess.Marketing.Domain.Enums;

namespace Acxess.Marketing.Application.Features.Promotions.DTOs;

public record PromotionDto(
    int IdPromotion,
    string Name,
    DiscountType  DiscountType,
    decimal Discount,
    bool RequiresCoupon,
    bool AutoApply,
    DateTime? AvailableFrom,
    DateTime? AvailableTo,
    bool IsActive
);