using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Marketing.Application.Features.Coupons.Commands.AssignCoupon;

public record AssignCouponCommand(int MemberId, int PromotionId, int UserId): IRequest<Result<string>>;