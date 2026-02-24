using System;
using Acxess.Catalog.Domain.Enums;
using Acxess.Shared.Abstractions;

namespace Acxess.Catalog.Domain.Entities;

public class SellingPlan : IHasTenant
{

    public static SellingPlan Create(int tenantId, string name, int totalMembers, int durationInValue, DurationUnit durationUnit, decimal price, int createdByUser)
    {
        return new SellingPlan(tenantId, name, totalMembers, durationInValue, durationUnit, price, createdByUser);
    }

    public void  Update(
        string name, 
        int totalMembers, 
        int durationInValue, 
        DurationUnit durationUnit, 
        decimal price,
        bool isActive)
    {
        Name = name;
        TotalMembers = totalMembers;
        DurationInValue = durationInValue;
        DurationUnit = durationUnit;
        Price = price;
        IsActive = isActive;
    }


    private SellingPlan(int tenantId, string name, int totalMembers, int durationInValue, DurationUnit durationUnit, decimal price, int createdByUser)
    {
        IdTenant = tenantId;
        Name = name;
        TotalMembers = totalMembers;
        DurationInValue = durationInValue;
        DurationUnit = durationUnit;
        Price = price;
        CreatedByUser = createdByUser;
    }

    private SellingPlan()
    {
    }
    public int IdSellingPlan { get; private set; }
    public int IdTenant { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int TotalMembers { get; private set; }
    public int DurationInValue { get; private set; }
    public DurationUnit DurationUnit { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.Now;
    public int CreatedByUser { get; private set; }

    public virtual ICollection<PlanAccessTiers> AccessTiers { get; private set; } = new List<PlanAccessTiers>();
    public void AddAccessTier(int accessTierId)
    {
        if (AccessTiers.Any(x => x.IdAccessTier == accessTierId)) return;

        var link =  PlanAccessTiers.Create(this.IdTenant, accessTierId, this.IdSellingPlan);
        
        AccessTiers.Add(link);
    }

    public void SyncAccessTiers(List<int> newTierIds)
    {
        var toRemove = AccessTiers
            .Where(existing => !newTierIds.Contains(existing.IdAccessTier))
            .ToList();

        foreach (var item in toRemove)
        {
            AccessTiers.Remove(item);
        }

        foreach (var tierId in newTierIds)
        {
            AddAccessTier(tierId);
        }
    }
}
