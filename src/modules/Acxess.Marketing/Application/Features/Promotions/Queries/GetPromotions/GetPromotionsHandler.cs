using Acxess.Marketing.Application.Features.Promotions.DTOs;
using Acxess.Marketing.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Marketing.Application.Features.Promotions.Queries.GetPromotions;

public class GetPromotionsHandler(
    MarketingModuleContext context) : IRequestHandler<GetPromotionsQuery, Result<List<PromotionDto>>>
{
    public async Task<Result<List<PromotionDto>>> Handle(GetPromotionsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Promotions.AsNoTracking();

        if (!request.IncludeInactives)
        {
            query = query.Where(p => p.IsActive);   
        }
        var promotions = await query.Select(p => new PromotionDto
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
        )).ToListAsync(cancellationToken);

        return promotions;
    }
}