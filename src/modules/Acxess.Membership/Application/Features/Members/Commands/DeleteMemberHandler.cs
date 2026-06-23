using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Features.Members.Commands;

public record DeleteMemberCommand(int MemberId, int UserId) : IRequest<Result<string>>, ITenantRequest
{
    public int IdTenant { get; set; }
}
public class DeleteMemberHandler(
    MembershipModuleContext context,
    ITimeService timeService,
    ILogger<DeleteMemberHandler> logger) : IRequestHandler<DeleteMemberCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await context.Members
            .Include(m => m.SubscriptionMemberships)
            .ThenInclude(sm => sm.Subscription)
            .FirstOrDefaultAsync(m => m.IdMember == request.MemberId, cancellationToken);

        if (member is null)
        {
            logger.LogError("Member not found. MemberId: {MemberId}", request.MemberId);
            return Result<string>.Failure(Error.NotFound("Member.NotFound", "Member Not Found."));
        }

        var today =  timeService.GetUtcNow();
        var hasActiveSub = member.HasActiveSubscription(today);
        
        if (hasActiveSub)
        {
            logger.LogWarning("The member was not removed because has an active subscription.  MemberId: {MemberId} ", request.MemberId);
            return Result<string>.Failure(Error.Conflict("Member.HasActiveSubscription", 
                "The member was not removed because has an active subscription."));
        }
        
        member.Delete(request.UserId);

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Member deleted successful. MemberId: {MemberId}", request.MemberId);   

        return "Member deleted successful";
    }
}