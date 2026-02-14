using Acxess.Shared.Abstractions;

namespace Acxess.Marketing.Domain.Entities;

public class AppliedPromotion : IHasTenant
{
    private AppliedPromotion() {}

    private AppliedPromotion( int idTenant, int idMemberTransactionDetail, decimal appliedAmount, int? idPromotion = null, int? idCoupon = null, string? notes = null)
    {
        IdMemberTransactionDetail = idMemberTransactionDetail;
        IdPromotion = idPromotion;
        IdCoupon = idCoupon;
        AppliedAmount = appliedAmount;
        Notes = notes;
        IdTenant = idTenant;
    }

    public int IdAppliedPromotion { get; private set; }
    public int IdMemberTransactionDetail { get; private set; }
    public int? IdPromotion { get; private set; }
    public int? IdCoupon { get; private set; }
    public decimal AppliedAmount { get; private set; }
    public string? Notes { get; private set; }
    
    
    public virtual Promotion? Promotion { get; private set; }
    public virtual Coupon? Coupon { get; private set; }

    public int IdTenant { get; private set; }
}
