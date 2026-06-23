using Acxess.Membership.Application.Formatters;
using Acxess.Membership.Infrastructure.Extensions;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Queries;

public record SearchEligibleMembersQuery(string Term, int? RenewingMemberId = null) 
    : IRequest<Result<List<EligibleMemberDto>>>, ITenantRequest
{
    public int IdTenant { get; set; }
}

public record EligibleMemberDto(
    int IdMember,
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    bool IsEligible,
    string IneligibilityReason
);
public class SearchEligibleMembersHandler(
    MembershipModuleContext context,
    ITimeService timeService) : IRequestHandler<SearchEligibleMembersQuery, Result<List<EligibleMemberDto>>>
{
    public async Task<Result<List<EligibleMemberDto>>> Handle(SearchEligibleMembersQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Term) )
            return Result<List<EligibleMemberDto>>.Success([]);
        
        var today =  timeService.GetUtcNow();
        
        List<int> sharedSubscriptionIds = [];
        if (request.RenewingMemberId is > 0)
        {
            sharedSubscriptionIds = await context.SubscriptionMembers
                .AsNoTracking()
                .Where(sm => sm.IdMember == request.RenewingMemberId.Value)
                .Select(sm => sm.IdSubscription)
                .ToListAsync(cancellationToken);
        }
        
        var query = context.Members.AsNoTracking();
        
        var isNumeric = int.TryParse(request.Term, out var id);
        
        if (isNumeric) query = query.Where(m => m.IdMember == id);
        else 
        {
            query = query.Where(m =>
                m.FirstName.Contains(request.Term) ||
                m.LastName.Contains(request.Term));
        }
        
        var candidates = await query
            .Take(10)
            .Select(m => new 
            {
                m.IdMember,
                m.FirstName, 
                m.LastName,
                m.Phone,
                m.Email,
                ActiveSubscription = m.SubscriptionMemberships
                    .Where(sm => !sm.Subscription.CancelledAt.HasValue
                                 && sm.Subscription.EndDate >= today
                                 && !sharedSubscriptionIds.Contains(sm.IdSubscription))
                    .Select(sm => new {
                        sm.Subscription.EndDate,
                        sm.Subscription.IdSellingPlan,
                        sm.Subscription.SellingPlanName
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        
        var result = candidates.Select(c =>
        {
            var isEligible = c.ActiveSubscription == null;

            var reason = MembershipDisplayFormatters
                .MemberElegibleLabel(isEligible, c.ActiveSubscription?.SellingPlanName);

            return new EligibleMemberDto(
                c.IdMember, 
                c.FirstName, 
                c.LastName, 
                c.Phone, 
                c.Email, 
                isEligible, 
                reason);
        }).ToList();

        return result;
    }
}