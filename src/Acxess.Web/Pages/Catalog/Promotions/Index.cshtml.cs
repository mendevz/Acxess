using System.Security.Claims;
using Acxess.Marketing.Application.Features.Promotions.Commands.NewPromotion;
using Acxess.Marketing.Application.Features.Promotions.Commands.UpdatePromotion;
using Acxess.Marketing.Application.Features.Promotions.DTOs;
using Acxess.Marketing.Application.Features.Promotions.Queries.GetPromotionById;
using Acxess.Marketing.Application.Features.Promotions.Queries.GetPromotions;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using Acxess.Web.Pages.Catalog.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Acxess.Web.Pages.Catalog.Promotions;

public class IndexModel(
    IMediator mediator,
    ICurrentTenant currentTenant) : BaseCatalogPageModel<PromotionInputModel, PromotionDto>
{
    public async Task<IActionResult> OnGetItemsAsync()
    {
        var result = await mediator.Send(new GetPromotionsQuery(true));
        
        if (result.IsFailure)  return ErrorState(result.Error.Description);
        
        Items = string.IsNullOrEmpty(Search) 
            ? result.Value 
            : result.Value.Where(x => x.Name.Contains(Search, StringComparison.OrdinalIgnoreCase)).ToList();

        return Partial("_List", this);
    }
    
    public async Task<IActionResult> OnGetFormAsync(int? id)
    {   
        switch (id)
        {
            case null:
                return NoSelectedItem();
            case 0:
                Input = new PromotionInputModel();
                return FormView();
        }
        
        var result = await mediator.Send(new GetPromotionByIdQuery((int)id));
        if (result.IsFailure) return ErrorState(result.Error.Description);
                
        var item = result.Value;
                
        Input = new PromotionInputModel()
        {
            IdPromotion = item.IdPromotion,
            Name =  item.Name,
            DiscountType =  item.DiscountType,
            Discount =  item.Discount,
            AutoApply =  item.AutoApply,
            IsActive =  item.IsActive,  
            AvailableFrom =  item.AvailableFrom,
            AvailableTo =  item.AvailableTo,    
            RequiresCoupon =   item.RequiresCoupon
        };

        return FormView();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid) return FormView();
        
        if (!currentTenant.IsAvailable)  return ErrorState("No estas autenticado");
    
        IRequest<Result<string>> command = Input.IdPromotion == 0
            ? new NewPromotionCommand(
                currentTenant.Id ?? 0,
                Input.Name,
                Input.DiscountType,
                Input.Discount,
                Input.RequiresCoupon,
                Input.AutoApply,
                Input.IsActive,
                Input.AvailableFrom,
                Input.AvailableTo,
                CreatedByUser: GetUserNumber())
            :  new UpdatePromotionCommand(
                Input.IdPromotion,
                Input.Name,
                Input.DiscountType,
                Input.Discount,
                Input.RequiresCoupon,
                Input.AutoApply,
                Input.IsActive,
                Input.AvailableFrom,
                Input.AvailableTo);

        var resultSaved = await mediator.Send(command);

        if (resultSaved.IsFailure)
        {
            return FormView(errorMessage: resultSaved.Error.Description);
        }

        TriggerHtmxRefresh();

        if (Input.IdPromotion != 0) return FormView(successMessage: resultSaved.Value);
        Input = new PromotionInputModel();
        ModelState.Clear();
        return FormView(successMessage: resultSaved.Value);
    }


}