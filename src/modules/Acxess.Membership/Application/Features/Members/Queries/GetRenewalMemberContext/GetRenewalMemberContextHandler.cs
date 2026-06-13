using Acxess.Membership.Application.Formatters;
using Acxess.Membership.Domain.Errors;
using Acxess.Membership.Infrastructure.Extensions;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Features.Members.Queries.GetRenewalMemberContext;

public class GetRenewalMemberContextHandler(
    MembershipModuleContext context,
    ILogger<GetRenewalMemberContextHandler> logger): IRequestHandler<GetRenewalMemberContextQuery, Result<RenewalContextDto>>
{
    public async Task<Result<RenewalContextDto>> Handle(GetRenewalMemberContextQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;
        
        var memberContext = await context.Members
            .AsNoTracking()
            .Where(m => m.IdMember == request.MemberId)
            .Select(m => new
            {
                m.IdMember,
                m.FirstName,
                m.LastName,
                LastGroupSub = m.SubscriptionMemberships
                    .OrderByDescending(sm => sm.Subscription.EndDate)
                    .Select(sm => new { sm.IdSubscription, sm.Subscription.IdSellingPlan })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        if (memberContext is null) 
            return Result<RenewalContextDto>.Failure(MemberError.NotFound);
            
        List<SuggestedBeneficiaryDto> suggestions = [];

        if (memberContext.LastGroupSub != null)
        {
            var previousMembersRaw = await context.SubscriptionMembers
                .AsNoTracking()
                .Where(sm => sm.IdSubscription == memberContext.LastGroupSub.IdSubscription && sm.IdMember != request.MemberId)
                .Select(sm => new
                {
                    sm.IdMember,
                    sm.Member.FirstName,
                    sm.Member.LastName,
                    Phone = sm.Member.Phone ?? string.Empty,
                    Email = sm.Member.Email ?? string.Empty,
                    HasConflictingSub = sm.Member.SubscriptionMemberships
                        .Select(s => s.Subscription)
                        .AnyConflictingSubscriptionMember(today, request.MemberId)
                })
                .ToListAsync(cancellationToken);


            suggestions.AddRange(previousMembersRaw.Select(item => new SuggestedBeneficiaryDto(
                item.IdMember,
                item.FirstName,
                item.LastName,
                item.Phone,
                item.Email,
                !item.HasConflictingSub,
                MembershipDisplayFormatters.MemberElegibleLabel(!item.HasConflictingSub, null)
            )));
        }
        
        logger.LogInformation("Member to renew context retrieved. {@MemberToRenew}", new
        {
            MemberId = memberContext.IdMember,
            FullName = $"{memberContext.FirstName} {memberContext.LastName}",
            LastPlanId =  memberContext.LastGroupSub?.IdSubscription,
            LastBeneficiaries = suggestions.Count
        });
        
        return Result<RenewalContextDto>.Success(new RenewalContextDto
        {
            MemberId = memberContext.IdMember,
            FullName = $"{memberContext.FirstName} {memberContext.LastName}",
            LastSubscriptionId = memberContext.LastGroupSub?.IdSubscription,
            LastPlanName = memberContext.LastGroupSub != null ? $"Plan anterior ({memberContext.LastGroupSub.IdSellingPlan})" : null,
            SuggestedBeneficiaries = suggestions
        });
                
    }
}