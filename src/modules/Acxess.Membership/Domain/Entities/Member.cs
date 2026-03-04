using Acxess.Membership.Domain.Constants;
using Acxess.Shared.Abstractions;
using Acxess.Shared.Enums;
using Acxess.Shared.IntegrationServices.Catalog;


namespace Acxess.Membership.Domain.Entities;

public class Member : IHasTenant
{
    public int IdMember { get; private set; }
    public int IdTenant { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? PhotoUrl { get; private set; }
    public bool IsDeleted { get; private set; }
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

    private Member(int idTenant, string firstName, string lastName, int createdByUser, string? email, string? phone, string? photoUrl)
    {
        IdTenant = idTenant;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        PhotoUrl = photoUrl;
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
        string? email = null,
        string? photoUrl = null)
    {
        var member =  new Member(
            tenantId, 
            firstName, 
            lastName, 
            createdByUser, 
            email, 
            phone,
            photoUrl);
        
        return member;
    }
    
    public void Delete(int userId)
    {
        IsDeleted = true;
    }
    
    public void Restore()
    {
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
    
    public void UpdatePhoto(string photoUrl)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = DateTime.Now;
    }

    public void Subscribe(int idPlan,
        string sellingPlanName,
        decimal priceSnapshot,
        int duration,
        int userId,
        DurationSubscriptionUnit durationUnit,
        List<int> beneficiaryIds,
        List<AddOnIntegrationDto> addOns)
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

    private (DateTime Start, DateTime End) CalculateSubscriptionDates(int duration, DurationSubscriptionUnit unitValue)
    {
        var today = DateTime.Now.Date; 
        var startDate = today;

        var lastActiveSub = _subscriptionMemberships
            .Select(sm => sm.Subscription)
            .Where(sm => sm.IsActive)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefault();

        if (lastActiveSub != null)
        {
            var lastEnd = lastActiveSub.EndDate.Date;
            
            startDate = (lastEnd >= today || today <= lastEnd.AddDays(Configurations.PRORROGA_DAYS))
                ? lastEnd
                : today;
        }

        var endDate = unitValue switch
        {
            DurationSubscriptionUnit.Days => startDate.AddDays(duration),
            DurationSubscriptionUnit.Months => startDate.AddMonths(duration),
            DurationSubscriptionUnit.Years => startDate.AddYears(duration),
            _ =>  startDate
        };

        return (startDate, endDate);
    }
    
}
