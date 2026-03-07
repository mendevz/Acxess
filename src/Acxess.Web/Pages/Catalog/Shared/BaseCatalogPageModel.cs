using System.Security.Claims;
using Acxess.Shared.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Acxess.Web.Pages.Catalog.Shared;

public class BaseCatalogPageModel<TInput, TDto>() : PageModel 
    where TInput : class, new()
    where TDto : class
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; } = string.Empty;

    [BindProperty]
    public TInput Input { get; set; } = new();

    public List<TDto> Items { get; protected set; } = [];

    public virtual void OnGet() { }
    
    protected virtual PartialViewResult FormView(string? successMessage = null, string? errorMessage = null, string viewName = "_Form")
    {
        var partialView = new PartialViewResult
        {
            ViewName = viewName,
            ViewData = new ViewDataDictionary<TInput>(ViewData, Input)
        };

        if (!string.IsNullOrWhiteSpace(successMessage))
            partialView.ViewData["SuccessMessage"] = successMessage;
        
        if (!string.IsNullOrWhiteSpace(errorMessage))
            partialView.ViewData["ErrorMessage"] = errorMessage;

        return partialView;
    }
    
    protected IActionResult ErrorState(string message) => Partial("_ErrorState", message);
    protected IActionResult NoSelectedItem() => Partial("/Pages/Catalog/Shared/_NoSelectedItem.cshtml");

    protected void TriggerHtmxRefresh(string triggerName = "refreshItems")
    {
        Response.Headers.Append("HX-Trigger", triggerName);
    }

    protected int GetUserNumber()
    {
        var userNumberString = User.FindFirstValue("UserNumber");
        return int.TryParse(userNumberString, out var val) ? val : 0;
    }
}