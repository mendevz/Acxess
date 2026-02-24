using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;

namespace Acxess.Membership.Domain.Entities;

public class Subscription : IHasTenant
{
    private Subscription(
        int tenantId, 
        int ownerMemberId, 
        int sellingPlanId, 
        DateTime startDate,
        DateTime endDate, 
        decimal priceSnapshot,
        string sellingPlanName,
        int createdByUser, 
        string? notes = null)
    {
        IdTenant = tenantId;
        IdMemberOwner = ownerMemberId;
        IdSellingPlan = sellingPlanId;
        PriceSnapshot = priceSnapshot;
        CreatedByUser = createdByUser;
        Notes = notes;
        StartDate = startDate;
        EndDate = endDate;
        CreatedAt =  DateTime.Now;
        IsActive = true;
        SellingPlanName = sellingPlanName;
    }

    private Subscription()
    {
    }
    public int IdSubscription { get; private set; }
    public int IdTenant { get; private set; }
    public int IdMemberOwner { get; private set; }
    public int IdSellingPlan { get; private set; }
    public string SellingPlanName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal PriceSnapshot { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; } =  DateTime.Now;
    public int CreatedByUser { get; private set; }
    
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public int? CancelledBy { get; private set; }
    
    public virtual Member OwnerMember { get; private set; } = null!;
    
    private readonly List<SubscriptionMembers> _subscriptionMembers = [];
    public virtual IReadOnlyCollection<SubscriptionMembers> SubscriptionMembers => _subscriptionMembers.AsReadOnly();
    
    private readonly List<SubscriptionAddOns> _addOns = [];
    public virtual IReadOnlyCollection<SubscriptionAddOns> AddOns => _addOns.AsReadOnly();

    public static Subscription Create(
        int tenantId, 
        Member owner, 
        int sellingPlanId,
        DateTime startDate, 
        DateTime endDate, 
        decimal priceSnapshot, 
        int userId,
        string sellingPlanName)
    {
        Subscription subscription = new (
            tenantId, 
            owner.IdMember, 
            sellingPlanId, 
            startDate, 
            endDate, 
            priceSnapshot, 
            sellingPlanName, userId)
            {
                OwnerMember = owner
            };
        
        subscription.AddOwnerMember(owner);
        
        return subscription;
    }
    
    private void AddOwnerMember(Member owner)
    {
        var membership = Membership.Domain.Entities.SubscriptionMembers.CreateForOwner(owner, this.IdSubscription, this.IdTenant);
        
        _subscriptionMembers.Add(membership);
    }
    
    public void AddMember(int memberId, bool isOwner)
    {
        if (_subscriptionMembers.Any(m => m.IdMember == memberId)) return;

        var membership = Membership.Domain.Entities.SubscriptionMembers.Create(memberId, IdSubscription, isOwner, this.IdTenant);
        
        _subscriptionMembers.Add(membership);
    }
    
    public void AddAddOn(int addOnId, decimal priceSnapshot)
    {
        var addon =  SubscriptionAddOns.Create(addOnId, IdSubscription, priceSnapshot, this.IdTenant);
        
        _addOns.Add(addon);
    }
    
    public Result Cancel(string reason, int userId)
    {
        if (!IsActive)
        {
           return Result.Failure("Subsctiprion.IsCanceled","Subscription is already cancel");
        }

        IsActive = false;
        
        CancelledAt = DateTime.Now;
        CancellationReason = reason;
        CancelledBy = userId;

        // AddDomainEvent(new SubscriptionCancelledEvent(this.Id));
        return Result.Success();
    }
    public void Deactivate()
    {
        if (!IsActive)
        {
           return ;
        }

        IsActive = false;
        CancelledBy = 1;
    }
    
    public void MarkAsExpired()
    {
        if (IsActive && EndDate < DateTime.Now) IsActive = false;
    }
}
