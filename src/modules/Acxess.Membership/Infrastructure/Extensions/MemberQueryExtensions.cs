using Acxess.Membership.Domain.Entities;

namespace Acxess.Membership.Infrastructure.Extensions;

public static class MemberQueryExtensions
{
    public static IQueryable<Member> WhereSubscriptionsExpiringSoon(
        this IQueryable<Member> query,
        DateTime today,
        DateTime daysFromNow)
    {
        return query.Where(m =>
            m.SubscriptionMemberships.Any(sm =>
                sm.Subscription.EndDate >= today
                && sm.Subscription.EndDate <= daysFromNow
                && !sm.Subscription.CancelledAt.HasValue
            )
            && !m.SubscriptionMemberships.Any(sm =>
                sm.Subscription.EndDate > daysFromNow
                && !sm.Subscription.CancelledAt.HasValue
            )
        );
    }

    public static IQueryable<Member> WhereHasSubscriptionActive(this IQueryable<Member> query, DateTime today)
        => query.Where(m => 
            m.SubscriptionMemberships
                .Select(s => s.Subscription)
                .AnySubscriptionActive(today)
        );

    public static IQueryable<Member> WhereHasNotSubscriptionActive(this IQueryable<Member> query, DateTime today)
        => query.Where(m => 
            !m.SubscriptionMemberships
                .Select(s => s.Subscription)
                .AnySubscriptionActive(today)
        );

    public static IQueryable<Member> NewMembersToday(
        this IQueryable<Member> query,
        DateTime today)
    {
        var endOfToday = today.AddDays(1).AddTicks(-1);
        return query.Where(m => 
            m.CreatedAt >= today && m.CreatedAt <= endOfToday
        );
    }

    public static IEnumerable<SubscriptionMembers> WhereActiveAndNotShared(
        this IEnumerable<SubscriptionMembers> subscriptionMemberships,
        DateTime today,
        ICollection<int> sharedSubscriptionIds)
    => subscriptionMemberships.Where(sm =>
        sm.Subscription.IsSubscriptionActive(today)
        && !sharedSubscriptionIds.Contains(sm.IdSubscription)
    );
}
