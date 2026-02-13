using System.ComponentModel.DataAnnotations;

namespace Acxess.Web.Pages.Membership.AddRenewMember;

public class AddRenewMemberInput
{
    public int Id { get; set; }
    [Required(ErrorMessage = "El nombre del socio es obligatorio.")]
    public string FirstName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Los apellidos del socio son obligatorios.")]
    public string LastName { get; set; }  = string.Empty;
    public string? Email { get; set; }  = string.Empty;
    public string? Phone { get; set; }   = string.Empty;
}