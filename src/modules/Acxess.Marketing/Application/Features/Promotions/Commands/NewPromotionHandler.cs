using Acxess.Marketing.Domain.Entities;
using Acxess.Marketing.Domain.Enums;
using Acxess.Marketing.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Marketing.Application.Features.Promotions.Commands;

public record NewPromotionCommand(
    string Name,
    DiscountType DiscountType,
    decimal Discount,
    bool RequiresCoupon,
    bool AutoApply,
    bool IsActive,
    DateTime? AvailableFrom,
    DateTime? AvailableTo,
    int CreatedByUser) : IRequest<Result<string>>, ITenantRequest
{
    public int IdTenant { get; set; }
};

public class NewPromotionHandler(
   MarketingModuleContext context,
   ITimeService timeService) : IRequestHandler<NewPromotionCommand, Result<string>>
{
    public async Task<Result<string>> Handle(NewPromotionCommand request, CancellationToken cancellationToken)
    {
        var utcNow = timeService.GetUtcNow();
        var promotion = Promotion.Create(
            request.IdTenant,
            request.Name,
            request.DiscountType,
            request.Discount,
            request.RequiresCoupon,
            request.AutoApply,
            request.IsActive,
            utcNow,
            request.AvailableFrom,
            request.AvailableTo,
            request.CreatedByUser);

        context.Promotions.Add(promotion);

       await context.SaveChangesAsync(cancellationToken);
        
        return "Promoción registrada correctamente";
    }
}