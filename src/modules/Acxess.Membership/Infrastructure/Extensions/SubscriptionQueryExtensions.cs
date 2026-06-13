using Acxess.Membership.Domain.Entities;
namespace Acxess.Membership.Infrastructure.Extensions;

public static class SubscriptionQueryExtensions
{
    public static IOrderedQueryable<Subscription> OrderByLatestAlertDate(this IQueryable<Subscription> query)
        => query.OrderByDescending(s => s.CancelledAt ?? s.EndDate);

    public static bool AnySubscriptionActive(this IEnumerable<Subscription> subscriptions, DateTime today)
        => subscriptions.Any(s => s.IsSubscriptionActive(today: today));

    public static bool IsSubscriptionActive(this Subscription s, DateTime today)
        => s.EndDate >= today && !s.CancelledAt.HasValue;

    public static IQueryable<Subscription> WhereInAlertWindowExpiring(
       this IQueryable<Subscription> query, DateTime recentExpirationThreshold,DateTime expirationWarningThreshold)
       => query.Where(s =>
           (!s.CancelledAt.HasValue && s.EndDate >= recentExpirationThreshold && s.EndDate <= expirationWarningThreshold) ||
           (s.CancelledAt.HasValue && s.CancelledAt >= recentExpirationThreshold && s.CancelledAt <= expirationWarningThreshold)
       );

    public static bool AnyConflictingSubscriptionMember(
        this IEnumerable<Subscription> subscriptions,
        DateTime today,
        int excludeMemberId)
    {
        return subscriptions.Any(s =>
            s.IsSubscriptionActive(today)
            && s.SubscriptionMembers.All(peer => peer.IdMember != excludeMemberId)
        );
    }
}
