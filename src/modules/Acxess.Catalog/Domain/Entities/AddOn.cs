using Acxess.Shared.Abstractions;

namespace Acxess.Catalog.Domain.Entities;

public class AddOn : IHasTenant
{
    private AddOn()
    {
    }

    public int IdAddOn { get; private set; }
    public int IdTenant { get; private set; }
    public string AddOnKey { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public bool ShowInCheckout { get; private set; }
    
    public bool IsVisit { get; private set; }
    public bool IsActive { get; private set; } = true;

    private AddOn(int tenantId, string addOnKey, string name, decimal price, bool isVisit, bool isActive, bool showInCheckout = false)
    {
        IdTenant = tenantId;
        AddOnKey = addOnKey;
        Name = name;
        Price = price;
        IsVisit = isVisit;
        IsActive = isActive;
        ShowInCheckout = showInCheckout;
    }

    public static AddOn Create(int tenantId, string addOnKey, string name, decimal price,  bool showInCheckout = false, bool isVisit = false)
    {
        return new AddOn(tenantId, addOnKey, name, price, isVisit, true, showInCheckout);
    }

    public void Update(
        string key,
        string name, 
        decimal price,
        bool showInCheckout,
        bool isVisit,
        bool isActive
    )
    {
        AddOnKey = key;
        Name = name;
        Price = price;
        ShowInCheckout =  showInCheckout;
        IsVisit = isVisit;
        IsActive = isActive;
    }
}
