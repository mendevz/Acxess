using System.Security.Claims;
using Acxess.Catalog.Application.Features.AccessTiers.Queries.GetAccessTiers;
using Acxess.Catalog.Application.Features.SellingPlans.Commands.NewSellingPlan;
using Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlanById;
using Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlans;
using Acxess.Catalog.Domain.Enums;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Acxess.Web.Pages.Catalog.SellingPlans;

public class IndexModel(IMediator mediator) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; } = string.Empty;

    public List<AccessTierDto> AccessTiers = [];
    public List<SellingPlanDto> Items = [];

    [BindProperty]
    public SellingPlanInputModel Input {get; set;} = new();

    public void OnGet() { }

    public async Task<IActionResult> OnGetItemsAsync()
    {
        var query = new GetSellingPlanQuery(true);
        var result = await mediator.Send(query);
        
        if (result.IsFailure)
        {
            return Partial("_ErrorState", result.Error.Description);
        }
        
        Items = string.IsNullOrEmpty(Search) 
            ? result.Value 
            : result.Value.Where(x => x.Name.Contains(Search, StringComparison.OrdinalIgnoreCase)).ToList();

        return Partial("_List", this);
    }

    public async Task LoadAccessTiers()
    {
        
        var resultAccessTiers = await mediator.Send(new GetAccessTiersQuery(false));

        if (resultAccessTiers.IsSuccess)
        {
            AccessTiers = resultAccessTiers.Value;
        }

    }

    public async Task<IActionResult> OnGetFormAsync(int? id)
    {   
        if (id is null) return Partial("/Pages/Catalog/Shared/_NoSelectedItem.cshtml");

        await LoadAccessTiers();

        if (id == 0)
        {
            Input = new SellingPlanInputModel();
        }
        else
        {
            var query = new GetSellingPlanByIdQuery(id??0);
            var result = await mediator.Send(query);

            if (result.IsFailure)
            {
                return Partial("_ErrorState", result.Error.Description);
            }

            var item = result.Value;
            
            Input = new SellingPlanInputModel 
            { 
                IdSellingPlan = item.IdSellingPlan,
                Name = item.Name,
                TotalMembers = item.TotalMembers,
                Price = item.Price,
                IsActive = item.IsActive,
                DurationInValue = item.DurationInValue,
                DurationUnit = (int)item.DurationUnit,
                AccessTiersIds = item.AccessTiersIds
            };
        }

        return Form();
    }

    private PartialViewResult Form(string? successMessage = null, string? errorMessage = null)
    {
        var partialView = new PartialViewResult
        {
            ViewName = "_Form",
            ViewData = new ViewDataDictionary<SellingPlanInputModel>(ViewData, Input)
        };

        partialView.ViewData.TemplateInfo.HtmlFieldPrefix = "Input";
        partialView.ViewData["AvailableTiers"] = AccessTiers;

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


    public async Task<IActionResult> OnPostSaveAsync()
    {
        await LoadAccessTiers();

        if (!ModelState.IsValid)  return Form();

        var userNumberString = User.FindFirstValue("UserNumber");
        var userNumber = int.TryParse(userNumberString, out var val) ? val : 0;

        if (userNumber == 0)  return Unauthorized(); 

        Result<string> resultSaved;

        if (Input.IdSellingPlan == 0)
        {
             var command =  new NewSellingPlanCommand(
                Input.TotalMembers,
                Input.DurationInValue,
                (DurationUnit)Input.DurationUnit,
                Input.Name,
                Input.Price,
                userNumber,
                Input.AccessTiersIds
            );

            resultSaved = await mediator.Send(command);
        }
        else
        {
            var command =  new UpdateSellingPlanCommand(
                Input.IdSellingPlan,
                Input.TotalMembers,
                Input.DurationInValue,
                (DurationUnit)Input.DurationUnit,
                Input.Name,
                Input.Price,
                Input.AccessTiersIds
            );
            resultSaved =  await mediator.Send(command);
        }

        if (resultSaved.IsFailure)
        {
            return Form(errorMessage: resultSaved.Error.Description);
        }

        Response.Headers.Append("HX-Trigger", "refreshItems");

        if (Input.IdSellingPlan != 0) return Form(successMessage: resultSaved.Value);
        Input = new SellingPlanInputModel();
        ModelState.Clear();
        return Form(successMessage: resultSaved.Value);
    }
}

