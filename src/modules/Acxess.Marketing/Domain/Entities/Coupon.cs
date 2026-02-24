using Acxess.Shared.Abstractions;

namespace Acxess.Marketing.Domain.Entities;

public class Coupon : IHasTenant
{
    private Coupon() {}

    private Coupon(int idTenant, int idMember, int idPromotion, int createdByUser, DateTime? expiresOn = null)
    {
        IdTenant = idTenant;
        IdMember = idMember;
        IdPromotion = idPromotion;
        CreatedByUser = createdByUser;
        ExpiresOn = expiresOn;
    }

    public int IdCoupon { get; private set; }
    public int IdTenant { get; private set; }
    public int IdMember { get; private set; }
    public int IdPromotion { get; private set; }
    public bool IsRedeemed { get; private set; } = false;
    public DateTime? ExpiresOn { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.Now;
    public int CreatedByUser { get; private set; }
    
    public virtual Promotion Promotion { get; private set; } = null!;
    
    public static Coupon Create(int idTenant, int idMember, int idPromotion, int userId, DateTime? expiresOn = null)
    {
        return new Coupon(idTenant, idMember, idPromotion, userId, expiresOn);
    }
}
