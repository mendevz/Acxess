using Acxess.Marketing.Domain.Entities;
using Acxess.Marketing.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Marketing.Application.Features.Coupons.Commands;

public record AssignCouponCommand(
    int MemberId, 
    int PromotionId, 
    int UserId
) : IRequest<Result<string>>, ITenantRequest
{
    public int IdTenant { get; set; }
}

public class AssignCouponHandler(
    MarketingModuleContext context,
    ITimeService timeService) : IRequestHandler<AssignCouponCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AssignCouponCommand request, CancellationToken cancellationToken)
    {
        var promotion = await context.Promotions
            .FirstOrDefaultAsync(p => p.IdPromotion == request.PromotionId, cancellationToken);
        
        if (promotion is null)
            return Result<string>.Failure(Error.NotFound("Promotion.NotFound", "Promoción no encontrada"));

        if (!promotion.RequiresCoupon)
            return Result<string>.Failure(Error.Validation("Promotion.NoCoupon", "Esta promoción no requiere cupón manual."));

        if (!promotion.IsActive)
            return Result<string>.Failure(Error.Validation("Promotion.Inactive", "La promoción no está activa."));
        
        var existingCoupon = await context.Coupons
            .AnyAsync(c => c.IdMember == request.MemberId && 
                           c.IdPromotion == request.PromotionId && 
                           !c.IsRedeemed, cancellationToken);
        if (existingCoupon)
            return Result<string>.Failure(Error.Conflict("Coupon.Exists", "El socio ya tiene un cupón activo para esta promoción."));
        
        var expiration = promotion.AvailableTo; 
        var utcNow = timeService.GetUtcNow();
        var coupon = Coupon.Create(
            request.IdTenant, 
            request.MemberId,
            request.PromotionId,
            request.UserId,
            utcNow,
            expiration
        );

        context.Coupons.Add(coupon);
        await context.SaveChangesAsync(cancellationToken);

        return "Cupon asignado correctamente";
    }
}