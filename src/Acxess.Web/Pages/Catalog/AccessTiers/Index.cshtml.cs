using Acxess.Catalog.Application.Features.AccessTiers.Commands.AddAccessTier;
using Acxess.Catalog.Application.Features.AccessTiers.Commands.UpdateAccessTier;
using Acxess.Catalog.Application.Features.AccessTiers.Queries.GetAccessTiers;
using Acxess.Catalog.Application.Features.AccessTiers.Queries.GetAccesTierById;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Acxess.Web.Pages.Catalog.AccessTiers;
public class IndexModel(IMediator sender) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; } = string.Empty;

    public List<AccessTierDto> Items { get; private set; } = new();

    [BindProperty]
    public AccessTierInput Input {get; set;}= new();

    public void OnGet()
    {

    }

    public async Task<IActionResult> OnGetItemsAsync()
    {
        var query = new GetAccessTiersQuery(true);
        var result = await sender.Send(query);
        
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
                Input = new AccessTierInput();
                break;
            default:
            {
                var query = new GetAccesTierByIdQuery(id ?? 0);
                
                var result = await sender.Send(query);
                
                if (result.IsFailure) return Form(errorMessage: result.Error.Description);

                Input = new AccessTierInput()
                {
                    Description = result.Value.Description,
                    Name = result.Value.Name,
                    IsActive = result.Value.IsActive
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
        
        if (Input.IdAccessTier == 0)
        {
            var command = new AddAccessTierCommand(Input.Name, Input.Description);
            resultSaved = await sender.Send(command);
        }
        else
        {
            var commandUpd = new UpdateAccessTierCommand(Input.IdAccessTier, Input.Name, Input.Description, Input.IsActive);
            resultSaved = await sender.Send(commandUpd);
        }


        if (resultSaved.IsFailure)
        {
            return Form(errorMessage: resultSaved.Error.Description);
        }

        Response.Headers.Append("HX-Trigger", "refreshItems");

        if (Input.IdAccessTier != 0) return Form(successMessage: resultSaved.Value);
        
        Input = new AccessTierInput(); 
        
        ModelState.Clear();
        
        return Form(successMessage: resultSaved.Value);

    }
    
    
    private PartialViewResult Form(string? successMessage = null, string? errorMessage = null)
    {
        var partialView = new PartialViewResult
        {
            ViewName = "_Form",
            ViewData = new ViewDataDictionary<AccessTierInput>(ViewData, Input)
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
