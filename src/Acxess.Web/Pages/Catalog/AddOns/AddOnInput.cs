using System.ComponentModel.DataAnnotations;

namespace Acxess.Web.Pages.Catalog.AddOns;

public class AddOnInput
{
    public int IdAddOn { get; set; }
    [Required(ErrorMessage = "Debes de establecer una KEY única")]
    [MaxLength(10)]
    [MinLength(1)]
    public string AddOnKey { get; set; } = string.Empty;
    [Required(ErrorMessage = "Debes de establecer un nombre")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal  Price { get; set; }
    public bool ShowInCheckout {get; set;}

    public bool IsActive {get; set;} = true;
    public bool IsVisit {get; set;} = true;


}
