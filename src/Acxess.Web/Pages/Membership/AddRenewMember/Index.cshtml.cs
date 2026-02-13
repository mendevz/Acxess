using System.Security.Claims;
using System.Text.Json;
using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOnInscription;
using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOns;
using Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlans;
using Acxess.Membership.Application.Features.Members.Commands.NewMember;
using Acxess.Membership.Application.Features.Members.Commands.RenewMember;
using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Membership.Application.Features.Members.Queries.GetMember;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acxess.Web.Pages.Membership.AddRenewMember;

public class IndexModel(
    IMediator mediator,
    ICurrentTenant currentTenant) : PageModel
{

    [BindProperty] public ProcessOrderRequest OrderRequest { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchMember { get; set; } = string.Empty;
    
    public List<SellingPlanDto> PlanItems = [];
    public List<AddOnDto> AddOnsItems = [];
    public List<MemberResponse> Members = [];


    public string InscriptionJson { get; set; } =  string.Empty;
    public string PreselectedMemberJson { get; set; } = string.Empty;

    public async Task OnGet()
    {
        var inscQuery = new GetAddOnInscriptionQuery();
        var result = await mediator.Send(inscQuery);

        if (result.IsSuccess)
        {
            InscriptionJson = JsonSerializer.Serialize(result.Value);   
        }
        
        if (!string.IsNullOrWhiteSpace(SearchMember))
        {
            var memberQuery = new GetMemberQuery(SearchMember);
            var resultMember = await mediator.Send(memberQuery);

            if (resultMember is { IsSuccess: true, Value.Count: > 0 })
            {
                var memberToSelect = resultMember.Value.FirstOrDefault();
                PreselectedMemberJson = JsonSerializer.Serialize(memberToSelect);
            }
        }
    }

    public async Task<IActionResult> OnGetMemberAsync()
    {
        var query = new GetMemberQuery(SearchMember);
        var result = await mediator.Send(query);

        if (result.IsFailure)
        {
            return Partial("_ErrorState", result.Error.Description);
        }
        Members = result.Value;
        
        return Partial("_MemberList", this);
    }

    public async Task<IActionResult> OnGetPlanItemsAsync()
    {
        var query = new GetSellingPlanQuery(false);
        var result = await mediator.Send(query);
        
        if (result.IsFailure)
        {
            return Partial("_ErrorState", result.Error.Description);
        }

        PlanItems = result.Value;
          
        return Partial("_PlanList", this);
    }
    
    public async Task<IActionResult> OnGetAddOnsItemsAsync()
    {
        var query = new GetAddOnsQuery(false);
        var result = await mediator.Send(query);
        if (result.IsFailure)
        {
            return Partial("_ErrorState", result.Error.Description);
        }
      
        AddOnsItems = result.Value;

        return Partial("_AddOnsList", this);
    }
    
    public async Task<IActionResult> OnPostProcessOrderAsync()
    {
        if (!ModelState.IsValid)  return Feedback(); 
        
        var userNumberString = User.FindFirstValue("UserNumber");
        var userNumber = int.TryParse(userNumberString, out var val) ? val : 0;

        if (userNumber == 0)  return Feedback(errorMessage: "No estas autenticado");

        var idTenant = currentTenant.Id ?? 0;
        
        var request = OrderRequest;

        var paymentMethodId = request.PaymentMethod == "cash" ? 1 : 2;

        var beneficiaries = request.AdditionalBeneficiaries.Select(b => new NewMemberDto(
            b.Id,
            b.FirstName,
            b.LastName,
            b.Phone)).ToList();
        
        Result<UpdatedSubMemberResponse> result;

        if (request.Mode == "new")
        {
            var member = new NewMemberDto(0, request.MemberData.FirstName, request.MemberData.LastName, request.MemberData.Phone);
            var newMemberCommand = new NewMemberCommand(
                member,
                request.PlanId??0,
                idTenant,
                request.AddOnIds,
                paymentMethodId,
                request.AmountPaid,
                beneficiaries,
                userNumber);
             result = await mediator.Send(newMemberCommand);
        }
        else
        {
            var renewMemberCommand = new RenewMemberCommand(
                request.MemberData.Id,
                request.PlanId ?? 0,
                idTenant,
                request.AddOnIds,
                paymentMethodId,
                request.AmountPaid,
                beneficiaries,
                userNumber);
            
            result = await mediator.Send(renewMemberCommand);
        }

        if (result.IsFailure)
        {
            return Feedback(errorMessage: result.Error.Description);
        }

        return Feedback(result.Value.Mensaje);
        

    }
    
    private PartialViewResult Feedback(string? successMessage = null, string? errorMessage = null)
    {
        var partial = Partial("_FeedbackMessages", this); 
    
        if (!string.IsNullOrWhiteSpace(successMessage))
            partial.ViewData["SuccessMessage"] = successMessage;

        if (!string.IsNullOrWhiteSpace(errorMessage))
            partial.ViewData["ErrorMessage"] = errorMessage;

        return partial;
    }
    
}