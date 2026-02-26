using Acxess.Membership.Application.Features.Members.Queries.GetMember;
using Acxess.Membership.Domain.Constants;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMemberToRenew;

internal sealed class GetMemberToRenewHandler(MembershipModuleContext context)
    : IRequestHandler<GetMemberToRenewQuery, Result<List<MemberResponse>>>
{
    public async Task<Result<List<MemberResponse>>> Handle(GetMemberToRenewQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return new List<MemberResponse>();
        }
        var term = request.SearchTerm.Trim();
        var query = context.Members.AsNoTracking();
        
        var isNumeric = int.TryParse(term, out var id);

        query = query.Where(m => !m.IsDeleted);
        
        if (isNumeric)
        {
            query = query.Where(m => m.IdMember == id);
        }
        else 
        {
            query = query.Where(m =>
                m.FirstName.Contains(term) ||
                m.LastName.Contains(term));
        }
        
        var today = DateTime.Now.Date;
        const int gracePeriodDays = Configurations.PRORROGA_DAYS;
        var limitDate = today.AddDays(-gracePeriodDays);

        var members = await query
            .Select(m => new
            {
                Member = m,
                LatestSubscription = m.SubscriptionMemberships
                    .Select(sm => sm.Subscription)
                    .Where(sm => sm.IsActive)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => new
                    {
                        s.EndDate,
                        s.IdSellingPlan,
                        s.SellingPlanName
                    })
                    .FirstOrDefault()
            })
            .Select(x => new MemberResponse(
                x.Member.IdMember,
                x.Member.FirstName,
                x.Member.LastName,
                x.Member.Email ?? string.Empty,
                x.Member.Phone ?? string.Empty,
                x.Member.CreatedAt,
                x.LatestSubscription != null ? (DateTime?)x.LatestSubscription.EndDate.Date : null,
                x.LatestSubscription != null ? x.LatestSubscription.SellingPlanName : null,
                x.LatestSubscription != null ,
                x.LatestSubscription != null && x.LatestSubscription.EndDate < today && x.LatestSubscription.EndDate >= limitDate
            ))
            .ToListAsync(cancellationToken);


        return members ?? Result<List<MemberResponse>>.Failure("Member.Query.NotFound", "Member not found");
    }
}
