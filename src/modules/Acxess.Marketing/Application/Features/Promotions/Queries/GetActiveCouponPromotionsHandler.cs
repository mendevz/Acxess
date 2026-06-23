using Acxess.Marketing.Application.Features.Promotions.DTOs;
using Acxess.Marketing.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Marketing.Application.Features.Promotions.Queries;

public record GetActiveCouponPromotionsQuery : IRequest<Result<List<PromotionSelectDto>>>, ITenantRequest
{
    public int IdTenant { get; set; }
}
public class GetActiveCouponPromotionsHandler(
    MarketingModuleContext context,
    ITimeService timeService) :IRequestHandler<GetActiveCouponPromotionsQuery, Result<List<PromotionSelectDto>>>
{
    public async Task<Result<List<PromotionSelectDto>>> Handle(GetActiveCouponPromotionsQuery request, CancellationToken cancellationToken)
    {
        var today = timeService.GetUtcNow();
        
        var promotions = await context.Promotions
            .AsNoTracking()
            .Where(p => 
                p.IsActive && 
                p.RequiresCoupon && 
                (!p.AvailableFrom.HasValue || p.AvailableFrom <= today) &&
                (!p.AvailableTo.HasValue || p.AvailableTo >= today)
            )
            .OrderBy(p => p.Name)
            .Select(p => new PromotionSelectDto(
                p.IdPromotion, 
                p.Name,
                p.DiscountType == Domain.Enums.DiscountType.Percent 
                    ? $"{p.Name} ({p.Discount:0}%)" 
                    : $"{p.Name} (${p.Discount:0.00})"
            ))
            .ToListAsync(cancellationToken);
        
        return promotions;
    }
}