using System.ComponentModel.DataAnnotations;
using Acxess.Marketing.Domain.Enums;

namespace Acxess.Web.Pages.Catalog.Promotions;

public class PromotionInputModel
{
    public int IdPromotion { get; set; }
    
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string Name { get; set; } = String.Empty;

    [Required(ErrorMessage = "El tipo de descuento es obligatorio")]
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;
    
    [Range(0.01, double.MaxValue, ErrorMessage = "El descuento debe ser mayor a 0")]
    public decimal Discount { get; set; }

    public bool RequiresCoupon { get; set; } = false;
    public bool AutoApply { get; set; } = false;
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableTo { get; set; }
    
    public bool IsActive { get; set; } = true;
}