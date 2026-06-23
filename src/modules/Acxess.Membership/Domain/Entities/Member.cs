using Acxess.Membership.Domain.Constants;
using Acxess.Membership.Domain.Services;
using Acxess.Shared.Abstractions;
using Acxess.Shared.Enums;
using Acxess.Shared.IntegrationServices;

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
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int CreatedByUser { get; private set; }
    
    private readonly List<Subscription> _ownedSubscriptions = [];
    public virtual IReadOnlyCollection<Subscription> OwnedSubscriptions => _ownedSubscriptions.AsReadOnly();
    
    private readonly List<SubscriptionMembers> _subscriptionMemberships = [];
    public virtual IReadOnlyCollection<SubscriptionMembers> SubscriptionMemberships => _subscriptionMemberships.AsReadOnly();
    
    private Member()
    {
    }

    private Member(
        int idTenant, 
        string firstName, 
        string lastName, 
        int createdByUser, 
        DateTime createdAt,
        string? email, 
        string? phone, 
        string? photoUrl)
    {
        IdTenant = idTenant;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        PhotoUrl = photoUrl;
        CreatedByUser = createdByUser;
        
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Member Create(
        int tenantId, 
        string firstName, 
        string lastName, 
        int createdByUser,
        DateTime createdAt,
        string? phone = null,
        string? email = null,
        string? photoUrl = null)
    {
        var member =  new Member(
            tenantId, 
            firstName, 
            lastName, 
            createdByUser, 
            createdAt,
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
    public bool HasActiveSubscription(DateTime today)
    {
        return _subscriptionMemberships.Any(sm =>  sm.Subscription.IsActive(today));
    }
    
    public void UpdateInformation(
        string firstName, 
        string lastName, 
        DateTime updatedAt,
        string? phone, 
        string? email)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Email = email;
        UpdatedAt = updatedAt;
    }
    
    public void UpdatePhoto(
        string photoUrl,
        DateTime updatedAt)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = updatedAt;
    }

    public void Subscribe(int idPlan,
        string sellingPlanName,
        decimal priceSnapshot,
        int duration,
        int userId,
        DurationSubscriptionUnit durationUnit,
        List<int> beneficiaryIds,
        List<AddOnIntegrationDto> addOns,
        DateTime currentDate)
    {
        var (startDate, endDate) = CalculateSubscriptionDates(duration, durationUnit, currentDate);
        
        var subscription = Subscription.Create(
            this.IdTenant,
            this,
            idPlan,
            currentDate,
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

        foreach (var (addOnId, _, _, addOnPrice) in addOns)
        {
            subscription.AddAddOn(addOnId, addOnPrice);
        }
        
        _ownedSubscriptions.Add(subscription);
        UpdatedAt = currentDate;
    }

    private (DateTime Start, DateTime End) CalculateSubscriptionDates(int duration, DurationSubscriptionUnit unitValue, DateTime today)
    {
        var startDate = today;

        var ownedActiveSubs = _ownedSubscriptions
               .Where(s => s.IsActive(startDate) || s.IsInGracePeriod(startDate));

        var membershipActiveSubs = _subscriptionMemberships
            .Select(sm => sm.Subscription)
            .Where(s => s.IsActive(startDate) || s.IsInGracePeriod(startDate));

        var lastActiveSub = ownedActiveSubs
            .Union(membershipActiveSubs)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefault();

        if (lastActiveSub != null)
        {
            var lastEnd = lastActiveSub.EndDate;
            
            startDate = (lastEnd >= today || today <= lastEnd.AddDays(Configurations.PRORROGA_DAYS))
                ? lastEnd
                : today;
        }

        var endDate = SubscriptionDateCalculator.CalculateEndDate(startDate, duration, unitValue);

        return (startDate, endDate);
    }
}
