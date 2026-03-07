using Acxess.Catalog.Application.Features.AccessTiers.Commands.AddAccessTier;
using Acxess.Catalog.Application.Features.AccessTiers.Commands.UpdateAccessTier;
using Acxess.Catalog.Application.Features.AccessTiers.Queries.GetAccessTiers;
using Acxess.Catalog.Application.Features.AccessTiers.Queries.GetAccesTierById;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using Acxess.Web.Pages.Catalog.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Acxess.Web.Pages.Catalog.AccessTiers;
public class IndexModel(
    IMediator sender,
    ICurrentTenant currentTenant) : BaseCatalogPageModel<AccessTierInput, AccessTierDto>
{

    public async Task<IActionResult> OnGetItemsAsync()
    {
        var query = new GetAccessTiersQuery(true);
        var result = await sender.Send(query);
        
        if (result.IsFailure) return ErrorState(result.Error.Description);
        
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
                Input = new AccessTierInput();
                return FormView();
        }

        var result = await sender.Send(new GetAccesTierByIdQuery((int)id));
        if (result.IsFailure) return FormView(errorMessage: result.Error.Description);

        Input = new AccessTierInput()
        {
            Description = result.Value.Description,
            Name = result.Value.Name,
            IsActive = result.Value.IsActive
        };
        return FormView();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid) return FormView();
        
        IRequest<Result<string>> request = Input.IdAccessTier == 0
            ? new AddAccessTierCommand(Input.Name, currentTenant.Id ?? 0, Input.Description)
            : new UpdateAccessTierCommand(Input.IdAccessTier, Input.Name, Input.Description, Input.IsActive);
        
        var resultSaved = await sender.Send(request);

        if (resultSaved.IsFailure)  return FormView(errorMessage: resultSaved.Error.Description);

        TriggerHtmxRefresh();

        if (Input.IdAccessTier != 0) return FormView(successMessage: resultSaved.Value);
        
        Input = new AccessTierInput(); 
        ModelState.Clear();
        return FormView(successMessage: resultSaved.Value);
    }
}
