using Acxess.Marketing.Domain.Abstractions;
using Acxess.Marketing.Domain.Entities;
using Acxess.Marketing.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Marketing.Application.Features.Coupons.Commands.AssignCoupon;

public class AssignCouponHandler(
    IMarketingUnitOfWork  unitOfWork,
    MarketingModuleContext context,
    ICurrentTenant currentTenant) : IRequestHandler<AssignCouponCommand, Result<string>>
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

        var coupon = Coupon.Create(
            currentTenant.Id??0, // O el tenant del contexto
            request.MemberId,
            request.PromotionId,
            request.UserId,
            expiration
        );

        context.Coupons.Add(coupon);
        var result = await unitOfWork.SaveChangesAsync(cancellationToken);

        return result.IsFailure
            ? Result<string>.Failure(result.Error)
            : "Cupon asignado correctamente";
    }
}