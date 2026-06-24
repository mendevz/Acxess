using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Subscriptions.Queries;

public record ExpiringMemberDto(int IdMember, string FullName, string Phone);
public record TenantExpiringDataDto(int IdTenant, List<ExpiringMemberDto> ExpiringMembers);
public record GetExpiringSubscriptionsQuery() : IRequest<Result<List<TenantExpiringDataDto>>>, ITenantRequest
{
    public int IdTenant { get; set; }
}

public class GetExpiringSubscriptionsHandler(
    MembershipModuleContext dbContext,
    ITimeService timeService) : IRequestHandler<GetExpiringSubscriptionsQuery, Result<List<TenantExpiringDataDto>>>
{
    public async Task<Result<List<TenantExpiringDataDto>>> Handle(GetExpiringSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var startOfToday = timeService.GetUtcNow().Date;
        var endOfToday = startOfToday.AddDays(1);

        var expiringData = await dbContext.Subscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.IdTenant == request.IdTenant)
            .Where(s => !s.CancelledAt.HasValue && s.EndDate >= startOfToday && s.EndDate < endOfToday)
            .Where(s => !dbContext.Subscriptions.Any(futureSub =>
                futureSub.IdSubscription != s.IdSubscription &&
                !futureSub.CancelledAt.HasValue && futureSub.EndDate >= startOfToday &&
                futureSub.StartDate <= endOfToday && 
                futureSub.SubscriptionMembers.Any(fsm => s.SubscriptionMembers.Any(sm => sm.IdMember == fsm.IdMember))))
            .SelectMany(s => s.SubscriptionMembers.Select(sm => new
            {
                s.IdTenant,
                MemberId = sm.Member.IdMember,
                FullName = sm.Member.FirstName + " " + sm.Member.LastName,
                Phone = sm.Member.Phone ?? string.Empty
            }))
            .GroupBy(x => x.IdTenant)
            .Select(g => new TenantExpiringDataDto(
                g.Key,
                g.Select(m => new ExpiringMemberDto(m.MemberId, m.FullName, m.Phone)).ToList()
            ))
            .ToListAsync(cancellationToken);

        return expiringData;
    }
}
