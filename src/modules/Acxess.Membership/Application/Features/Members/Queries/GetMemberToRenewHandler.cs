using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Membership.Domain.Constants;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Features.Members.Queries;

public sealed record GetMemberToRenewQuery(string? SearchTerm) : IRequest<Result<List<MemberResponse>>>, ITenantRequest
{
    public int IdTenant { get; set; }
}

internal sealed class GetMemberToRenewHandler(
    MembershipModuleContext context,
    ILogger<GetMemberToRenewHandler> logger,
    ITimeService timeService) : IRequestHandler<GetMemberToRenewQuery, Result<List<MemberResponse>>>
{
    public async Task<Result<List<MemberResponse>>> Handle(GetMemberToRenewQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm)) return new List<MemberResponse>();
        
        var query = context.Members.AsNoTracking();
        var utcNow = timeService.GetUtcNow();
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
        var limitDate = utcNow.AddDays(-gracePeriodDays);

        var timeZoneId = await timeService.GetTenantTimeZoneIdAsync(request.IdTenant, cancellationToken);

        var rawMembers = await query
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
                    .Where(sm =>
                        sm.Subscription.EndDate >= limitDate
                        && !sm.Subscription.CancelledAt.HasValue
                    )
                    .OrderByDescending(s => s.Subscription.EndDate)
                    .Select(s => new
                    {
                        s.Subscription.EndDate,
                        s.Subscription.IdSellingPlan,
                        s.Subscription.SellingPlanName
                    })
                    .FirstOrDefault()
            })
            .Take(15)
            .ToListAsync(cancellationToken);

            var members = rawMembers.Select(x => new MemberResponse(
                x.IdMember,
                x.FirstName,
                x.LastName,
                x.Email ?? string.Empty,
                x.Phone ?? string.Empty,
                timeService.ConvertDateFromZoneId(x.CreatedAt, timeZoneId),
                x.LatestSubscription != null 
                    ? timeService.ConvertDateFromZoneId(x.LatestSubscription.EndDate, timeZoneId) 
                    : null, 
                x.LatestSubscription?.SellingPlanName,
                x.LatestSubscription != null 
                    && x.LatestSubscription.EndDate >= utcNow,
                x.LatestSubscription != null 
                    && x.LatestSubscription.EndDate < utcNow 
                    && x.LatestSubscription.EndDate >= limitDate,
                x.PhotoUrl
            )).ToList();


        logger.LogInformation("Query completed. Members to renew obtained. MembersId: {@MemberId}", members.Select(m => m.Id));
        return members;
    }
}
