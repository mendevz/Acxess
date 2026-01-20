using Acxess.Marketing.Domain.Enums;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Marketing.Application.Features.Promotions.Commands.NewPromotion;

public record NewPromotionCommand(
    int IdTenant,
    string Name,
    DiscountType DiscountType,
    decimal Discount,
    bool RequiresCoupon,
    bool AutoApply,
    bool IsActive,
    DateTime? AvailableFrom,
    DateTime? AvailableTo,
    int CreatedByUser) : IRequest<Result<string>>;
