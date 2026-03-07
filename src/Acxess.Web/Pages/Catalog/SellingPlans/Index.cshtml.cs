using Acxess.Catalog.Application.Features.AccessTiers.Queries.GetAccessTiers;
using Acxess.Catalog.Application.Features.SellingPlans.Commands.NewSellingPlan;
using Acxess.Catalog.Application.Features.SellingPlans.Commands.UpdateSellingPlan;
using Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlanById;
using Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlans;
using Acxess.Shared.Abstractions;
using Acxess.Shared.Enums;
using Acxess.Shared.ResultManager;
using Acxess.Web.Pages.Catalog.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Acxess.Web.Pages.Catalog.SellingPlans;

public class IndexModel(
    IMediator mediator,
    ICurrentTenant currentTenant) : BaseCatalogPageModel<SellingPlanInputModel, SellingPlanDto>
{

    public List<AccessTierDto> AccessTiers = [];

    public async Task<IActionResult> OnGetItemsAsync()
    {
        var result = await mediator.Send( new GetSellingPlanQuery(true));
        if (result.IsFailure)  return ErrorState(result.Error.Description);
        
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
        if (id is null) return NoSelectedItem();

        await LoadAccessTiers();

        if (id == 0)
        {
            Input = new SellingPlanInputModel();
            return FormView();
        }
        
        var query = new GetSellingPlanByIdQuery(id??0);
        var result = await mediator.Send(query);

        if (result.IsFailure)  return ErrorState(result.Error.Description);

        var item = result.Value;
            
        Input = new SellingPlanInputModel 
        { 
            IdSellingPlan = item.IdSellingPlan,
            Name = item.Name,
            TotalMembers = item.TotalMembers,
            Price = item.Price,
            IsActive = item.IsActive,
            DurationInValue = item.DurationInValue,
            DurationUnit = (int)item.DurationSubscriptionUnit,
            AccessTiersIds = item.AccessTiersIds
        };

        return FormView();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        await LoadAccessTiers();

        if (!ModelState.IsValid)  return FormView();


        IRequest<Result<string>> command = Input.IdSellingPlan == 0 
            ? new NewSellingPlanCommand(
                currentTenant.Id ?? 0,
                Input.TotalMembers,
                Input.DurationInValue,
                (DurationSubscriptionUnit)Input.DurationUnit,
                Input.Name,
                Input.Price,
                GetUserNumber(),
                Input.AccessTiersIds
            )
            : new UpdateSellingPlanCommand(
                Input.IdSellingPlan,
                Input.TotalMembers,
                Input.DurationInValue,
                (DurationSubscriptionUnit)Input.DurationUnit,
                Input.Name,
                Input.Price,
                Input.AccessTiersIds,
                Input.IsActive
            );

        var resultSaved = await mediator.Send(command);


        if (resultSaved.IsFailure)
        {
            return FormView(errorMessage: resultSaved.Error.Description);
        }

        TriggerHtmxRefresh();

        if (Input.IdSellingPlan != 0) return FormView(successMessage: resultSaved.Value);
        Input = new SellingPlanInputModel();
        ModelState.Clear();
        return FormView(successMessage: resultSaved.Value);
    }
    
    protected override PartialViewResult FormView(string? successMessage = null, string? errorMessage = null, string viewName = "_Form")
    {
        var partialView = base.FormView(successMessage, errorMessage, viewName);

        partialView.ViewData.TemplateInfo.HtmlFieldPrefix = "Input";
        partialView.ViewData["AvailableTiers"] = AccessTiers;

        return partialView;
    }
}

