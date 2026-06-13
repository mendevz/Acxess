using Acxess.Membership.Application.Formatters;
using Acxess.Membership.Infrastructure.Extensions;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Dashboard.Queries.GetDashboardStats;

public class GetDashboardStatsHandler(
    MembershipModuleContext context) : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    public async Task<Result<DashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;
        
        var expirationWarningThreshold = today.AddDays(2); 
        var recentExpirationThreshold = today.AddDays(-8);

        var membersBaseQuery = context.Members.AsNoTracking().Where(m => !m.IsDeleted);

        var newMembersToday = await membersBaseQuery
            .NewMembersToday(today)
            .CountAsync(cancellationToken);

        var totalMembers = await membersBaseQuery.CountAsync(cancellationToken);

        var activeMembers = await membersBaseQuery
            .WhereHasSubscriptionActive(today)
            .CountAsync(cancellationToken);

        var expiredMembers = totalMembers - activeMembers;

        var expiringSoon = await membersBaseQuery
            .WhereSubscriptionsExpiringSoon(today, expirationWarningThreshold)
            .CountAsync(cancellationToken);

        var topExpiringQuery = await membersBaseQuery
            .Select(m => new 
            {
                Member = m,
                FullName = $"{m.FirstName} {m.LastName}",
                InitialsName = MembershipDisplayFormatters.GetInitials(m.FirstName, m.LastName),
                RelevantSub = m.SubscriptionMemberships
                    .Select(sm => sm.Subscription)
                    .AsQueryable()
                    .WhereInAlertWindowExpiring(recentExpirationThreshold, expirationWarningThreshold)
                    .OrderByLatestAlertDate()
                    .FirstOrDefault(),
                HasFutureSub = m.SubscriptionMemberships
                .Select(s => s.Subscription)
                .AnySubscriptionActive(expirationWarningThreshold)
            })
            .Where(x => x.RelevantSub != null && !x.HasFutureSub)
            .OrderByDescending(x => x.RelevantSub!.CancelledAt ?? x.RelevantSub!.EndDate)
            .Take(20)
            .ToListAsync(cancellationToken);

        var topExpiring = topExpiringQuery
            .Select(x => 
            {
                var isCanceled = x.RelevantSub!.CancelledAt.HasValue;
                var referenceDate = isCanceled ? x.RelevantSub!.CancelledAt!.Value : x.RelevantSub!.EndDate;
                var daysDiff = (referenceDate.Date - today).Days;
                
                var namePlan = MembershipDisplayFormatters.GetSellingPlanName(x.RelevantSub.SellingPlanName, isCanceled);

                return new ExpiringMemberItem(
                    x.Member.IdMember,
                    x.FullName,
                    namePlan, 
                    referenceDate,
                    daysDiff, 
                    x.InitialsName,
                    x.Member.PhotoUrl
                );
            })
            .ToList();

        return Result<DashboardStatsDto>.Success(new DashboardStatsDto
        {
            NewMembersToday = newMembersToday,
            TotalMembers = totalMembers,
            ActiveMembers = activeMembers,
            ExpiredMembers = expiredMembers,
            ExpiringSoon = expiringSoon,
            TopExpiringMembers = topExpiring,
            GrowthPercentage = 0
        });
    }
}