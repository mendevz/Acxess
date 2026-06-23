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
            m.SubscriptionMemberships.Any(sm =>
                sm.Subscription.EndDate >= today && !sm.Subscription.CancelledAt.HasValue
            )
        );

    public static IQueryable<Member> WhereHasNotSubscriptionActive(this IQueryable<Member> query, DateTime today)
        => query.Where(m => 
            !m.SubscriptionMemberships.Any(sm =>
                sm.Subscription.EndDate >= today && !sm.Subscription.CancelledAt.HasValue
            )
        );

    public static IQueryable<Member> CreatedBetween(
        this IQueryable<Member> query,
        DateTime startUtc,
        DateTime endUtc)
    {
        return query.Where(m =>
            m.CreatedAt >= startUtc && m.CreatedAt < endUtc
        );
    }
}
