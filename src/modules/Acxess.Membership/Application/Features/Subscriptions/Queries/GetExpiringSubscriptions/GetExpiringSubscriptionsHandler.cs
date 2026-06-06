
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Subscriptions.Queries.GetExpiringSubscriptions;

public class GetExpiringSubscriptionsHandler(
    MembershipModuleContext dbContext) : IRequestHandler<GetExpiringSubscriptionsQuery, Result<List<TenantExpiringDataDto>>>
{
    public async Task<Result<List<TenantExpiringDataDto>>> Handle(GetExpiringSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;

        var expiringData = await dbContext.Subscriptions
            .AsNoTracking() 
            .Where(s => s.IsActive && s.EndDate.Date == today)
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
