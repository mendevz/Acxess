using Acxess.Membership.Domain.Entities;
namespace Acxess.Membership.Infrastructure.Extensions;

public static class SubscriptionQueryExtensions
{
    public static bool IsSubscriptionActive(this Subscription s, DateTime today)
        => s.EndDate.Date >= today.Date && !s.CancelledAt.HasValue;

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
