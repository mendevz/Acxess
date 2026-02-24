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
        if (result.IsFailure)
        {
            return Partial("_ErrorState", result.Error.Description);
        }
      
        Items = string.IsNullOrEmpty(Search) 
                ? result.Value 
                : result.Value.Where(x => x.Name.Contains(Search, StringComparison.OrdinalIgnoreCase)).ToList();

        return Partial("_Table", this);
    }

     public async Task<IActionResult> OnGetFormAsync(int? id)
    {   
        switch (id)
        {
            case null:
                return Partial("/Pages/Catalog/Shared/_NoSelectedItem.cshtml");
            case 0:
                Input = new AddOnInput();
                break;
            default:
            {
                var query = new GetAddOnQuery(id??0);
                var result = await mediator.Send(query);

                if (result.IsFailure)
                {
                    return Form(errorMessage: result.Error.Description);
                }

                var item = result.Value;
            
                Input = new AddOnInput 
                { 
                    IdAddOn = item.IdAddOn,
                    AddOnKey = item.AddOnKey, 
                    Name = item.Name, 
                    Price = item.Price, 
                    IsActive = item.IsActive,
                    ShowInCheckout = item.ShowInCheckout ,
                    IsVisit = item.IsVisit
                };
                break;
            }
        }

        return Form();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid) return Form();

        Result<string> resultSaved;


        if (Input.IdAddOn == 0)
        {
            var command = new NewAddOnCommand(
                currentTenant.Id??0, 
                Input.AddOnKey,
                Input.Name,
                Input.Price,
                false,
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
                Input.ShowInCheckout,
                Input.IsVisit,
                Input.IsActive
                );

            resultSaved = await mediator.Send(commandUpd);
        }

        if (resultSaved.IsFailure)
        {
            return Form(errorMessage: resultSaved.Error.Description);
        }

        Response.Headers.Append("HX-Trigger", "refreshItems");

        if (Input.IdAddOn != 0) return Form(successMessage: resultSaved.Value);
        
        Input = new AddOnInput(); 
        
        ModelState.Clear();
        
        return Form(successMessage: resultSaved.Value);

    }
    
    private PartialViewResult Form(string? successMessage = null, string? errorMessage = null)
    {
        var partialView = new PartialViewResult
        {
            ViewName = "_Form",
            ViewData = new ViewDataDictionary<AddOnInput>(ViewData, Input)
        };

        if (!string.IsNullOrWhiteSpace(successMessage))
        {
            partialView.ViewData["SuccessMessage"] = successMessage;
        }
        
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            partialView.ViewData["ErrorMessage"] = errorMessage;
        }

        return partialView;
    }

}
