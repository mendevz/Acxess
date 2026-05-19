using Acxess.Membership.Application.Features.Members.Queries.GetMember;
using Acxess.Membership.Domain.Constants;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMemberToRenew;

internal sealed class GetMemberToRenewHandler(
    MembershipModuleContext context,
    ILogger<GetMemberToRenewHandler> logger) : IRequestHandler<GetMemberToRenewQuery, Result<List<MemberResponse>>>
{
    public async Task<Result<List<MemberResponse>>> Handle(GetMemberToRenewQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm)) return new List<MemberResponse>();
        
        var query = context.Members.AsNoTracking();
        var today = DateTime.Now.Date;
        var term = request.SearchTerm.Trim();
        var isNumeric = int.TryParse(term, out var id);

        query = query.Where(m => !m.IsDeleted);
        
        if (isNumeric)
            query = query.Where(m => m.IdMember == id);
        else
        {
            var searchTerms = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            query = searchTerms.Aggregate(query, (current, t) => current.Where(m => m.FirstName.Contains(t) || m.LastName.Contains(t)));
        }
        
        const int gracePeriodDays = Configurations.PRORROGA_DAYS;
        var limitDate = today.AddDays(-gracePeriodDays);

        var members = await query
            .OrderBy(m => m.IdMember)
            .Select(m => new
            {
                m.IdMember,
                m.FirstName,
                m.LastName,
                m.Email,
                m.Phone,
                m.CreatedAt,
                m.PhotoUrl,
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
            .Take(15)
            .Select(x => new MemberResponse(
                x.IdMember,
                x.FirstName,
                x.LastName,
                x.Email ?? string.Empty,
                x.Phone ?? string.Empty,
                x.CreatedAt,
                x.LatestSubscription != null ? (DateTime?)x.LatestSubscription.EndDate.Date : null,
                x.LatestSubscription != null ? x.LatestSubscription.SellingPlanName : null,
                x.LatestSubscription != null ,
                x.LatestSubscription != null && x.LatestSubscription.EndDate < today && x.LatestSubscription.EndDate >= limitDate,
                x.PhotoUrl
            ))
            .ToListAsync(cancellationToken);
        
        logger.LogInformation("Query completed. Members to renew obtained. MembersId: {@MemberId}", members.Select(m => m.Id));
        return members;
    }
}
