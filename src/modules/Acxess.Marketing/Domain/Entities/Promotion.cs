using Acxess.Marketing.Domain.Enums;
using Acxess.Shared.Abstractions;

namespace Acxess.Marketing.Domain.Entities;

public class Promotion : IHasTenant
{
    private Promotion()
    {
    }

    private Promotion( int idTenant, string name, DiscountType discountType, decimal discount, bool requiresCoupon, bool autoApply, DateTime? availableFrom, DateTime? availableTo, bool isActive, int createdByUser)
    {
        
        IdTenant = idTenant;
        Name = name;
        DiscountType = discountType;
        Discount = discount;
        RequiresCoupon = requiresCoupon;
        AutoApply = autoApply;
        AvailableFrom = availableFrom;
        AvailableTo = availableTo;
        IsActive = isActive;
        CreatedByUser = createdByUser;
    }

    public int IdPromotion { get; private set; }
    public int IdTenant { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DiscountType DiscountType { get; private set; }
    public decimal Discount { get; private set; }
    public bool RequiresCoupon { get; private set; } = false;
    public bool AutoApply { get; private set; } = false;
    public DateTime? AvailableFrom { get; private set; }
    public DateTime? AvailableTo { get; private set; }
    public bool IsActive { get; private set; } = true;
    
    private readonly List<Coupon> _coupons = [];
    public virtual IReadOnlyCollection<Coupon> Coupons => _coupons.AsReadOnly();

    public DateTime CreatedAt { get; private set; } = DateTime.Now;
    public int CreatedByUser { get; private set; }

    public static Promotion Create(
        int idTenant,
        string name, 
        DiscountType discountType,
        decimal discount,
        bool requiresCoupon,
        bool autoApply,
        bool isActive,
        DateTime? availableFrom,
        DateTime? availableTo,
        int  createdByUser
        )
    {
        return new Promotion(
            idTenant,
            name,
            discountType,
            discount,
            requiresCoupon, autoApply, availableFrom, availableTo, isActive, createdByUser
        );
    }

    public void Update(
        string name,
        DiscountType discountType,
        decimal discount,
        bool requiresCoupon,
        bool autoApply,
        bool isActive,
        DateTime? availableFrom,
        DateTime? availableTo
    )
    {
        Name = name;
        DiscountType = discountType;
        Discount = discount;
        RequiresCoupon = requiresCoupon;
        AutoApply = autoApply;
        IsActive = isActive;
        AvailableFrom = availableFrom;
        AvailableTo = availableTo;
    }

    
}
