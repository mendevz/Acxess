using Acxess.Shared.Abstractions;

namespace Acxess.Membership.Domain.Entities;

public class SubscriptionAddOns : IHasTenant
{

    private SubscriptionAddOns(){}

    private SubscriptionAddOns(int addOnId, int subscriptionId, decimal priceSnapshot, int idTenant)
    {
        IdTenant = idTenant;
        IdAddOn = addOnId;
        IdSubscription = subscriptionId;
        PriceSnapshot = priceSnapshot;
    }

    public static SubscriptionAddOns Create(int addOnId, int subscriptionId, decimal priceSnapshot, int idTenant)
    {
        return new SubscriptionAddOns(addOnId, subscriptionId, priceSnapshot, idTenant);
    }

    public int IdTenant { get; private set; }
    public int IdSubscriptionAddOn { get; private set; }
    public int IdAddOn { get; private set; }
    public int IdSubscription { get; private set; }
    public decimal PriceSnapshot { get; private set; }
    
    public virtual Subscription Subscription { get; private set; } = null!;
}
