using Acxess.Membership.Application.Features.Dashboard.DTOs;
using Acxess.Membership.Application.Formatters;
using Acxess.Membership.Infrastructure.Extensions;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Dashboard.Queries;

public record GetDashboardStatsQuery : IRequest<Result<DashboardStatsDto>>, ITenantRequest
{
    public int IdTenant { get; set; }
}
public class GetDashboardStatsHandler(
    MembershipModuleContext context,
    ITimeService timeService) : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    public async Task<Result<DashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var utcNow = timeService.GetUtcNow();
        var timeZoneId = await timeService.GetTenantTimeZoneIdAsync(request.IdTenant, cancellationToken);

        var localToday = await timeService.GetLocalNowAsync(request.IdTenant, cancellationToken);

        var startOfLocalTodayAsUtc = timeService.ConvertDateFromZoneId(localToday, timeZoneId); 
        var startOfLocalTomorrowAsUtc = startOfLocalTodayAsUtc.AddDays(1);

        var expirationWarningThreshold = utcNow.AddDays(2);
        var recentExpirationThreshold = utcNow.AddDays(-8);


        var membersBaseQuery = context.Members.AsNoTracking().Where(m => !m.IsDeleted);

        var newMembersToday = await membersBaseQuery
            .CreatedBetween(startOfLocalTodayAsUtc, startOfLocalTomorrowAsUtc)
            .CountAsync(cancellationToken);

        var totalMembers = await membersBaseQuery.CountAsync(cancellationToken);

        var activeMembers = await membersBaseQuery
            .WhereHasSubscriptionActive(utcNow)
            .CountAsync(cancellationToken);

        var expiredMembers = totalMembers - activeMembers;

        var expiringSoon = await membersBaseQuery
            .WhereSubscriptionsExpiringSoon(utcNow, expirationWarningThreshold)
            .CountAsync(cancellationToken);

        var topExpiringQuery = await membersBaseQuery
            .Select(m => new 
            {
                Member = m,
                RelevantSub = m.SubscriptionMemberships
                    .Select(sm => sm.Subscription)
                    .Where(s =>
                        (s.CancelledAt.HasValue && s.CancelledAt <= expirationWarningThreshold) ||
                        (!s.CancelledAt.HasValue && s.EndDate <= expirationWarningThreshold)
                    )
                    .OrderByDescending(s => s.CancelledAt ?? s.EndDate)
                    .FirstOrDefault(),
                HasFutureSub = m.SubscriptionMemberships
                    .Select(s => s.Subscription)
                    .Any(s => s.EndDate > expirationWarningThreshold && !s.CancelledAt.HasValue)
            })
            .Where(x => x.RelevantSub != null && !x.HasFutureSub)
            .OrderByDescending(x => x.RelevantSub!.CancelledAt ?? x.RelevantSub!.EndDate)
            .Take(20)
            .ToListAsync(cancellationToken);

        var topExpiring = topExpiringQuery
            .Select(x => 
            {
                var isCanceled = x.RelevantSub!.CancelledAt.HasValue;
                var referenceDateUtc = isCanceled ? x.RelevantSub!.CancelledAt!.Value : x.RelevantSub!.EndDate;

                var referenceDateLocal = timeService.ConvertDateFromZoneId(referenceDateUtc, timeZoneId);

                var daysDiff = (referenceDateLocal.Date - localToday).Days;

                var namePlan = MembershipDisplayFormatters.GetSellingPlanName(x.RelevantSub.SellingPlanName, isCanceled);

                return new ExpiringMemberItem(
                    x.Member.IdMember,
                    $"{x.Member.FirstName} {x.Member.LastName}",
                    namePlan,
                    referenceDateLocal,
                    daysDiff,
                    MembershipDisplayFormatters.GetInitials(x.Member.FirstName, x.Member.LastName),
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