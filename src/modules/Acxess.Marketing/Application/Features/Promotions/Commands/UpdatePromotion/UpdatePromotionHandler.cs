using Acxess.Marketing.Domain.Abstractions;
using Acxess.Marketing.Domain.Errors;
using Acxess.Marketing.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Marketing.Application.Features.Promotions.Commands.UpdatePromotion;

public class UpdatePromotionHandler(
    MarketingModuleContext context,
    IMarketingUnitOfWork unitOfWork) : IRequestHandler<UpdatePromotionCommand, Result<string>>
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

        var resultUpdated = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (resultUpdated.IsFailure)
        {
            return Result<string>.Failure(resultUpdated.Error);
        }

        return "Promoción actualizada correctamente";
    }
}