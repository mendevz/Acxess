using System.ComponentModel.DataAnnotations;
using Acxess.Web.Utils.Validators;

namespace Acxess.Web.Pages.Catalog.SellingPlans;

public class SellingPlanInputModel
{
    public int IdSellingPlan { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Rango mínimo de valor 1 - 5")]
    public int TotalMembers { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    [Range(1, 5, ErrorMessage = "Rango mínimo de valor 1 - 5")]
    public int DurationInValue { get; set; } = 1;
    public int DurationUnit { get; set; } = 2;

    [Required]
    [EnsureMinimumElements(1)]
    public List<int> AccessTiersIds { get; set; } = []; 
}