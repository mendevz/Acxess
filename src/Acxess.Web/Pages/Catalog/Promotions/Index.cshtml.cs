using System.Security.Claims;
using Acxess.Marketing.Application.Features.Promotions.Commands.NewPromotion;
using Acxess.Marketing.Application.Features.Promotions.Commands.UpdatePromotion;
using Acxess.Marketing.Application.Features.Promotions.DTOs;
using Acxess.Marketing.Application.Features.Promotions.Queries.GetPromotionById;
using Acxess.Marketing.Application.Features.Promotions.Queries.GetPromotions;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Acxess.Web.Pages.Catalog.Promotions;

public class IndexModel(
    IMediator mediator,
    ICurrentTenant currentTenant) : PageModel
{
    
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; } = string.Empty;

    public List<PromotionDto> Items { get; private set; } = [];
    [BindProperty] public PromotionInputModel Input { get; set; } = new();
    
    public void OnGet()
    {
        
    }

    public async Task<IActionResult> OnGetItemsAsync()
    {
        var query = new GetPromotionsQuery(true);
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
    
    public async Task<IActionResult> OnGetFormAsync(int? id)
    {   
        await Task.Delay(200);
        switch (id)
        {
            case null:
                return Partial("/Pages/Catalog/Shared/_NoSelectedItem.cshtml");
            case 0:
                Input = new PromotionInputModel();
                break;
            default:
            {
                var query = new GetPromotionByIdQuery(id ?? 0);
                var result = await mediator.Send(query);

                if (result.IsFailure) return Partial("_ErrorState", result.Error.Description);
                
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
                break;
            }
        }

        return Form();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid) return Form();
        
        Result<string> resultSaved;

        if (Input.IdPromotion == 0)
        {
            if (!currentTenant.IsAvailable)
            {
                return Partial("_ErrorState", "No estas autenticado");
            }
            
            var userNumberString = User.FindFirstValue("UserNumber");
            var userNumber = int.TryParse(userNumberString, out var val) ? val : 0;

            if (userNumber == 0)  return Partial("_ErrorState", "No estas autenticado");

            var command = new NewPromotionCommand(
                currentTenant.Id ?? 0,
                Input.Name,
                Input.DiscountType,
                Input.Discount,
                Input.RequiresCoupon,
                Input.AutoApply,
                Input.IsActive,
                Input.AvailableFrom,
                Input.AvailableTo,
                CreatedByUser: userNumber);
            
            resultSaved = await mediator.Send(command);
        }
        else
        {
            var command = new UpdatePromotionCommand(
                Input.IdPromotion,
                Input.Name,
                Input.DiscountType,
                Input.Discount,
                Input.RequiresCoupon,
                Input.AutoApply,
                Input.IsActive,
                Input.AvailableFrom,
                Input.AvailableTo);
            
            resultSaved = await mediator.Send(command);
        }

        if (resultSaved.IsFailure)
        {
            return Form(errorMessage: resultSaved.Error.Description);
        }
        
        Response.Headers.Append("HX-Trigger", "refreshItems");

        if (Input.IdPromotion != 0) return Form(successMessage: resultSaved.Value);
        Input = new PromotionInputModel();
        ModelState.Clear();
        return Form(successMessage: resultSaved.Value);
    }

    private PartialViewResult Form(string? successMessage = null, string? errorMessage = null)
    {
        var partialView = new PartialViewResult
        {
            ViewName = "_Form",
            ViewData = new ViewDataDictionary<PromotionInputModel>(ViewData, Input)
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