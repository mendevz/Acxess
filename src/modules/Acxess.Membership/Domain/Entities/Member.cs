using Acxess.Membership.Domain.Constants;
using Acxess.Shared.Abstractions;
using Acxess.Shared.Domain;
using Acxess.Shared.IntegrationEvents.Membership;

namespace Acxess.Membership.Domain.Entities;

public class Member : Entity, IHasTenant
{
    public int IdMember { get; private set; }
    public int IdTenant { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsDeleted { get; private set; } = false;
    public DateTime CreatedAt { get; private set; } = DateTime.Now;
    public DateTime UpdatedAt { get; private set; } = DateTime.Now;
    public int CreatedByUser { get; private set; }
    
    private readonly List<Subscription> _ownedSubscriptions = [];
    public virtual IReadOnlyCollection<Subscription> OwnedSubscriptions => _ownedSubscriptions.AsReadOnly();
    
    private readonly List<SubscriptionMembers> _subscriptionMemberships = [];
    public virtual IReadOnlyCollection<SubscriptionMembers> SubscriptionMemberships => _subscriptionMemberships.AsReadOnly();
    
    private Member()
    {
        
    }

    private Member(int tenantId, string firstName, string lastName, string? email, string? phone, int createdByUser)
    {
        IdTenant = tenantId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        CreatedByUser = createdByUser;
        
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }

    public static Member Create(
        int tenantId, 
        string firstName, 
        string lastName, 
        int createdByUser,
        string? phone = null,
        string? email = null )
    {
        var member =  new Member(
            tenantId, firstName, lastName, email, phone, createdByUser);
        
        member.AddDomainEvent(new MemberCreatedDomainEvent(member.IdMember));
        return member;
    }
    
    public void Delete(int userId)
    {
        if (IsDeleted) return; 
        IsDeleted = true;
    }
    
    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
    }
    
    public bool HasActiveSubscription()
    {
        return _subscriptionMemberships.Any(sm => sm.Subscription.IsActive && sm.Subscription.EndDate > DateTime.Now);
    }
    
    public void UpdateInformation(string firstName, string lastName, string? phone, string? email)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Email = email;
        UpdatedAt = DateTime.Now;
    }

    public void Subscribe(
        int idPlan, 
        string sellingPlanName,
        decimal priceSnapshot, 
        int duration, 
        int durationUnit, 
        int userId,
        List<int> beneficiaryIds,
        List<(int Id, string Name, decimal Price)> addOns )
    {
        var (startDate, endDate) = CalculateSubscriptionDates(duration, durationUnit);
        
        var subscription = Subscription.Create(
            this.IdTenant,
            this,
            idPlan,
            startDate,
            endDate,
            priceSnapshot,
            userId,
            sellingPlanName
        );
        
        foreach (var beneficiaryId in beneficiaryIds)
        {
            subscription.AddMember(beneficiaryId, isOwner: false);
        }
        
        foreach (var (addOnId, name, addOnPrice) in addOns)
        {
            subscription.AddAddOn(addOnId, addOnPrice);
        }
        
        _ownedSubscriptions.Add(subscription);
        UpdatedAt = DateTime.Now;
    }

    private (DateTime Start, DateTime End) CalculateSubscriptionDates(int duration, int unit)
    {
        var today = DateTime.Now.Date; 
        var startDate = today;

        var lastSubscription = _subscriptionMemberships
            .Select(sm => sm.Subscription)
            .Where(sm => sm.IsActive)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefault();

        if (lastSubscription != null)
        {
            var lastEnd = lastSubscription.EndDate.Date;

            if (lastEnd >= today || today <= lastEnd.AddDays(Configurations.PRORROGA_DAYS) && lastSubscription.IsActive)
                startDate = lastEnd; 
            else
                startDate = today; 
        }

        var endDate = unit switch
        {
            1 => startDate.AddDays(duration),
            2 => startDate.AddMonths(duration),
            _ => startDate.AddYears(duration)
        };

        return (startDate, endDate);
    }
    
}
