using Acxess.Shared.Abstractions;

namespace Acxess.Membership.Domain.Entities;

public class SubscriptionMembers : IHasTenant
{

    private SubscriptionMembers()
    {
    }
    private SubscriptionMembers(int memberId, int subscriptionId, bool owner, int idTenant)
    {
        IdMember = memberId;
        IdSubscription = subscriptionId;
        Owner = owner;
        IdTenant = idTenant;
    }
    
    internal static SubscriptionMembers Create(int memberId, int subscriptionId, bool owner, int idTenant)
    {
        return new SubscriptionMembers(
             memberId,
             subscriptionId,
             owner,
             idTenant
        );
    }
    
    internal static SubscriptionMembers CreateForOwner(Member owner, int subscriptionId, int idTenant)
    {
        SubscriptionMembers sm = new(owner.IdMember, subscriptionId, true, idTenant)
        {
            Member = owner
        };

        return sm;
    }

    public int IdTenant { get; }
    public int IdSubscriptionMember { get; private set; }
    public int IdMember { get; private set; }
    public int IdSubscription { get; private set; }
    public bool Owner { get; private set; }
    
    public virtual Member Member { get; private set; } = null!;
    public virtual Subscription Subscription { get; private set; } = null!;
}
