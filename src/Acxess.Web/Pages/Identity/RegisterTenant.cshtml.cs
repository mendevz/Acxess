using System.ComponentModel.DataAnnotations;
using Acxess.Identity.Application.Features.Tenants.Commands.RegisterTenant;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acxess.Web.Pages.Identity;

public class RegisterTenantInputModel
{
    [Required(ErrorMessage = "El nombre del negocio es obligatorio")]
    [MinLength(3, ErrorMessage = "La longitud del nombre del negocio no es válida")]
    public string NameTenant { get; set; } = string.Empty;

    [Required(ErrorMessage = "El usuario es obligatorio")]
    [MinLength(2, ErrorMessage = "La longitud del usuario no es válida")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del administrador es obligatorio")]
    public string NombreAdmin { get; set; } = string.Empty;

    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "El email es obligatorio")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El password es necesario")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = "America/Mexico_City";
}

public class RegisterTenantModel(IMediator mediator) : PageModel
{
    [BindProperty]
    public RegisterTenantInputModel Input { get; set; } = new RegisterTenantInputModel();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Los datos ingresados no son válidos.");
            return Page();
        }

        var newTenantInfo = new RegisterTenantCommand(
            Input.NameTenant,
            Input.Username,
            Input.NombreAdmin,
            Input.Password,
            Input.TimeZoneId,
            Input.Email
        );
        var result = await mediator.Send(newTenantInfo);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Description);
            return Page();
        }

        return RedirectToPage("/Identity/Login");
    }
}
