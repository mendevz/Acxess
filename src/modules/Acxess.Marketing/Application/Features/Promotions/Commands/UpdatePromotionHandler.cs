using Acxess.Marketing.Domain.Enums;
using Acxess.Marketing.Domain.Errors;
using Acxess.Marketing.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Marketing.Application.Features.Promotions.Commands;

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
public class UpdatePromotionHandler(MarketingModuleContext context) : IRequestHandler<UpdatePromotionCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotionToUpdate = await context.Promotions.FirstOrDefaultAsync(
            p => p.IdPromotion == request.IdPromotion, cancellationToken);

        if (promotionToUpdate is null)
        {
            return Result<string>.Failure(PromotionErrors.NotFound);
        }
        
        promotionToUpdate.Update(
            request.Name,
            request.DiscountType,
            request.Discount,
            request.RequiresCoupon,
            request.AutoApply,
            request.IsActive,
            request.AvailableFrom,
            request.AvailableTo);

        await context.SaveChangesAsync(cancellationToken);

        return "Promoción actualizada correctamente";
    }
}