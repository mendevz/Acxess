using Acxess.Marketing.Domain.Abstractions;
using Acxess.Marketing.Domain.Entities;
using Acxess.Marketing.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Marketing.Application.Features.Promotions.Commands.NewPromotion;

public class NewPromotionHandler(
   MarketingModuleContext context,
   IMarketingUnitOfWork unitOfWork) : IRequestHandler<NewPromotionCommand, Result<string>>
{
    public async Task<Result<string>> Handle(NewPromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = Promotion.Create(
            request.IdTenant,
            request.Name,
            request.DiscountType,
            request.Discount,
            request.RequiresCoupon,
            request.AutoApply,
            request.IsActive,
            request.AvailableFrom,
            request.AvailableTo,
            request.CreatedByUser);

        context.Promotions.Add(promotion);

        var resultSaved = await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return resultSaved.IsFailure ? Result<string>.Failure(resultSaved.Error) : "Promoción registrada correctamente";
    }
}