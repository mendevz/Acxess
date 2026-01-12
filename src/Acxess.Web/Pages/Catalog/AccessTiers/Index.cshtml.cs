using System.Text.Json;
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
            Input = new AccessTierInput();
        }
        else
        {
            var query = new GetAccesTierByIdQuery(id??0);
            var result = await sender.Send(query);

            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error.Description);
                return Partial("_Form", Input);
            }

            var item = result.Value;
            
            Input = new AccessTierInput 
            { 
                IdAccessTier = item.IdAccessTier, 
                Name = item.Name, 
                Description = item.Description, 
                IsActive = item.IsActive 
            };
        }

        return Partial("_Form", Input);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {

        //    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        //     foreach(var err in errors) Console.WriteLine($"ERROR BACKEND: {err}");

        if (!ModelState.IsValid) return Partial("_Form", Input);

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
            ModelState.AddModelError(string.Empty, resultSaved.Error.Description);
            return Partial("_Form", Input); 
        }

        Response.Headers.Append("HX-Trigger", "refreshItems");

        if(Input.IdAccessTier == 0) Input = new AccessTierInput(); 

        var partialView = new PartialViewResult
        {
            ViewName = "_Form",
            ViewData = new ViewDataDictionary<AccessTierInput>(ViewData, Input)
        };
        
        partialView.ViewData["SuccessMessage"] = resultSaved.Value;

        return partialView;

    }
}
