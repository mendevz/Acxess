using Acxess.Marketing.Application.Features.Promotions.DTOs;
using Acxess.Marketing.Domain.Errors;
using Acxess.Marketing.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Marketing.Application.Features.Promotions.Queries.GetPromotionById;

public class GetPromotionByIdHandler(
    MarketingModuleContext context) : IRequestHandler<GetPromotionByIdQuery, Result<PromotionDto>>
{
    public async Task<Result<PromotionDto>> Handle(GetPromotionByIdQuery request, CancellationToken cancellationToken)
    {
        var query = context.Promotions.AsNoTracking()
            .Where(p => p.IdPromotion == request.IdPromotion);

        var promotionDto = await query.Select(p => new PromotionDto
        (
             p.IdPromotion,
             p.Name,
             p.DiscountType,
             p.Discount,
             p.RequiresCoupon,  
             p.AutoApply,
             p.AvailableFrom,
             p.AvailableTo ,
             p.IsActive
        )).FirstOrDefaultAsync(cancellationToken);

        return promotionDto ?? Result<PromotionDto>.Failure(PromotionErrors.NotFound);
    }
}