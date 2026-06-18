using Acxess.Membership.Infrastructure.Extensions;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Features.Subscriptions.Commands;

public record CancelSubscriptionCommand(int SubscriptionId, string Reason, int UserId) : IRequest<Result<string>>;
public class CancelSubscriptionHandler(
    MembershipModuleContext context,
    ILogger<CancelSubscriptionHandler> logger) : IRequestHandler<CancelSubscriptionCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;

        var memberId = await context.SubscriptionMembers
            .Where(sm => sm.IdSubscription == request.SubscriptionId)
            .Select(sm => sm.IdMember)
            .FirstOrDefaultAsync(cancellationToken);

        if (memberId == 0)
        {
            logger.LogError("Subscription not found or does not have an assigned member. SubscriptionId: {SubscriptionId}", request.SubscriptionId);
            return Result<string>.Failure(Error.NotFound("Subscription.NotFound", "Subscription not found or does not have an assigned member."));
        }
        
        var activeSubscriptionsToCancel = await context.SubscriptionMembers
            .Include(sm => sm.Subscription)
            .Where(sm => sm.IdMember == memberId && sm.Subscription.IsSubscriptionActive(today))
            .Select(sm => sm.Subscription)
            .ToListAsync(cancellationToken);

        if (activeSubscriptionsToCancel.Count == 0)
        {
            logger.LogWarning("No was found active subscriptions to cancel. MemberId: {MemberId}", memberId);
            return "There isn't active subscriptions";
        }

        foreach (var sub in activeSubscriptionsToCancel)
        {
            sub.Cancel(request.Reason, request.UserId);
        }

        await context.SaveChangesAsync(cancellationToken);
        
        foreach (var sub in activeSubscriptionsToCancel)
        {
            logger.LogInformation(
                "Subscription cancelled successful. SubscriptionId: {SubscriptionId}, MemberId: {MemberId}, Reason: {Reason}",  
                sub.IdSubscription,
                memberId,
                request.Reason);   
        }

        return  "Subscription cancelled.";
    }
}