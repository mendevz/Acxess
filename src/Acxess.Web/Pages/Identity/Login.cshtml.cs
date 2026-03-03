using System.ComponentModel.DataAnnotations;
using Acxess.Identity.Application.Features.ApplicationUser.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acxess.Web.Pages.Identity;

public class LoginInputModel
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    [MinLength(2, ErrorMessage = "La longitud del usuario no es válida")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El password es necesario")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
public class LoginModel(
    IMediator mediator
) : PageModel
{
    [BindProperty]
    public LoginInputModel Input { get; set; } = new LoginInputModel();

    public string ReturnUrl { get; set; } = string.Empty;
    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity!.IsAuthenticated)
        {
            return RedirectToPage("/Index");
        }
        ReturnUrl = returnUrl ?? Url.Content("~/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Los datos ingresados no son válidos.");
            return Page();
        } 

        var result = await mediator.Send(new LoginCommand(Input.Username, Input.Password));

        if (result.IsSuccess)
        {
            return LocalRedirect(returnUrl);
        }
        
        ModelState.AddModelError(string.Empty, result.Error.Description);
        return Page();
    }


}
