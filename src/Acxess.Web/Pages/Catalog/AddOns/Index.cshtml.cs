using Acxess.Catalog.Application.Features.AddOns.Commands.NewAddOn;
using Acxess.Catalog.Application.Features.AddOns.Commands.UpdateAddOn;
using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOnById;
using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOns;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Acxess.Web.Pages.Catalog.AddOns;
public class IndexModel(
    IMediator mediator,
    ICurrentTenant currentTenant
) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; } = string.Empty;
    public List<AddOnDto> Items {get; private set;} = [];

    [BindProperty]
    public AddOnInput Input {get; set;}= new();
    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetItemsAsync()
    {
        var query = new GetAddOnsQuery(true);
        var result = await mediator.Send(query);

        if (result.IsSuccess)
        {
            Items = string.IsNullOrEmpty(Search) 
                ? result.Value 
                : result.Value.Where(x => x.Name.Contains(Search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Partial("_Table", this);
    }

     public async Task<IActionResult> OnGetFormAsync(int? id)
    {   
        if (id is null) return Partial("/Pages/Catalog/Shared/_NoSelectedItem.cshtml");

        if (id == 0)
        {
            Input = new AddOnInput();
        }
        else
        {
            var query = new GetAddOnQuery(id??0);
            var result = await mediator.Send(query);

            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error.Description);
                return Partial("_Form", Input);
            }

            var item = result.Value;
            
            Input = new AddOnInput 
            { 
                IdAddOn = item.IdAddOn,
                AddOnKey = item.AddOnKey, 
                Name = item.Name, 
                Price = item.Price, 
                IsActive = item.IsActive,
                ShowInCheckout = item.ShowInCheckout 
            };
        }

        return Partial("_Form", Input);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid) return Partial("_Form", Input);

        Result<string> resultSaved;


        if (Input.IdAddOn == 0)
        {
            var command = new NewAddOnCommand(
                currentTenant.Id??0, 
                Input.AddOnKey,
                Input.Name,
                Input.Price,
                Input.IsActive);

            resultSaved = await mediator.Send(command);
        }
        else
        {
            var commandUpd = new UpdateAddOnCommand(
                Input.IdAddOn, 
                Input.AddOnKey,
                Input.Name, 
                Input.Price, 
                Input.IsActive);

            resultSaved = await mediator.Send(commandUpd);
        }

        if (resultSaved.IsFailure)
        {
            ModelState.AddModelError(string.Empty, resultSaved.Error.Description);
            return Partial("_Form", Input); 
        }

        Response.Headers.Append("HX-Trigger", "refreshItems");

        if(Input.IdAddOn == 0){ 
            Input = new AddOnInput();
            ModelState.Clear();    
        } 

        var partialView = new PartialViewResult
        {
            ViewName = "_Form",
            ViewData = new ViewDataDictionary<AddOnInput>(ViewData, Input)
        };
        
        partialView.ViewData["SuccessMessage"] = resultSaved.Value;

        return partialView;

    }

}
