using Acxess.Catalog.Application.Features.AddOns.Commands.NewAddOn;
using Acxess.Catalog.Application.Features.AddOns.Commands.UpdateAddOn;
using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOnById;
using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOns;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using Acxess.Web.Pages.Catalog.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Acxess.Web.Pages.Catalog.AddOns;
public class IndexModel(
    IMediator mediator,
    ICurrentTenant currentTenant
) : BaseCatalogPageModel<AddOnInput,AddOnDto>
{
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
                return NoSelectedItem();
            case 0:
                Input = new AddOnInput();
                return FormView();
        }
        
        var result = await mediator.Send(new GetAddOnQuery(id??0));
        if (result.IsFailure) return FormView(errorMessage: result.Error.Description);

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

        return FormView();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid) return FormView();

        IRequest<Result<string>> command = Input.IdAddOn == 0 
            ? new NewAddOnCommand(
                currentTenant.Id??0, 
                Input.AddOnKey,
                Input.Name,
                Input.Price,
                false,
                Input.IsActive)
            
            : new UpdateAddOnCommand(
                Input.IdAddOn, 
                Input.AddOnKey,
                Input.Name, 
                Input.Price, 
                Input.ShowInCheckout,
                Input.IsVisit,
                Input.IsActive
            );

        var resultSaved = await mediator.Send(command);
        if (resultSaved.IsFailure)
        {
            return FormView(errorMessage: resultSaved.Error.Description);
        }

        TriggerHtmxRefresh();

        if (Input.IdAddOn != 0) return FormView(successMessage: resultSaved.Value);
        
        Input = new AddOnInput(); 
        
        ModelState.Clear();
        
        return FormView(successMessage: resultSaved.Value);
    }
}
