using System.Security.Claims;
using System.Text.Json;
using Acxess.Billing.Application.Features.Transactions.Commands.NewVisitTransaction;
using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOnInscription;
using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOns;
using Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlans;
using Acxess.Membership.Application.Features.Members.Commands.NewMember;
using Acxess.Membership.Application.Features.Members.Commands.RenewMember;
using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Membership.Application.Features.Members.Queries.GetMember;
using Acxess.Membership.Application.Features.Members.Queries.GetRenewalMemberContext;
using Acxess.Membership.Application.Features.Members.Queries.SearchEligibleMembers;
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
    
    [BindProperty(SupportsGet = true)]
    public int? MemberId { get; set; }
    
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
        
        if (MemberId is > 0)
        {
            var memberQuery = new GetMemberToRenewQuery(MemberId.Value.ToString());
            var resultMember = await mediator.Send(memberQuery);

            if (resultMember is { IsSuccess: true, Value.Count: > 0 })
            {
                var memberToSelect = resultMember.Value.FirstOrDefault(m => m.Id == MemberId.Value);
                if (memberToSelect != null)
                {
                    PreselectedMemberJson = JsonSerializer.Serialize(memberToSelect);
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(SearchMember))
        {
            var memberQuery = new GetMemberToRenewQuery(SearchMember);
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
        var query = new GetMemberToRenewQuery(SearchMember);
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
        if (!ModelState.IsValid)  return Feedback(errorMessage: "Error de validaciones"); 
        
        var userNumberString = User.FindFirstValue("UserNumber");
        var userNumber = int.TryParse(userNumberString, out var val) ? val : 0;

        if (userNumber == 0)  return Feedback(errorMessage: "No estas autenticado");

        var idTenant = currentTenant.Id ?? 0;
        
        var request = OrderRequest;

        var paymentMethodId = request.PaymentMethod == "cash" ? 1 : 2;

        var beneficiaries = request.AdditionalBeneficiaries.Select(b => new NewMemberDto(
            b.Id,
            b.FirstName ?? "",
            b.LastName?? "",
            b.Phone)).ToList();
        
        Result<UpdatedSubMemberResponse> result;
        
        switch (request.Mode)
        {
            case ProcessOrderRequest.VISIT_MEMBER:
            {
                var visitName = string.IsNullOrWhiteSpace(request.MemberData.FirstName) ? "Visita General" : request.MemberData.FirstName;
                var visitCommand = new CreateVisitTransactionCommand(
                    idTenant, visitName, paymentMethodId, request.AmountPaid, userNumber, request.AddOnIds
                );
                var resultVisit = await mediator.Send(visitCommand);
                if (resultVisit.IsFailure) return Feedback(errorMessage: resultVisit.Error.Description);
                return Feedback(
                    successMessage: resultVisit.Value, 
                    targetUrl: Url.Page("/Membership/AddRenewMember/Index")
                );
            }
            case ProcessOrderRequest.NEW_MEMBER:
            {
                var member = new NewMemberDto(0, request.MemberData.FirstName?? "", request.MemberData.LastName?? "", request.MemberData.Phone, request.MemberData.PhotoBase64);
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
                break;
            }
            default:
            {
                var renewMemberCommand = new RenewMemberCommand(
                    request.MemberData.Id,
                    request.PlanId ?? 0,
                    idTenant,
                    request.AddOnIds,
                    paymentMethodId,
                    request.AmountPaid,
                    beneficiaries,
                    userNumber,
                    request.MemberData.PhotoBase64);
            
                result = await mediator.Send(renewMemberCommand);
                break;
            }
        }

        if (result.IsFailure)
            return Feedback(errorMessage: result.Error.Description);

        var targetUrl = Url.Page("/Membership/DigitalExpedient/Index", new { memberId = result.Value.IdMember });
        return Feedback(successMessage: result.Value.Mensaje, targetUrl: targetUrl);
    }
    
    public async Task<IActionResult> OnGetRenewalContextAsync(int id)
    {
        var result = await mediator.Send(new GetRenewalMemberContextQuery(id));
        return Partial("_RenewalSuggestions", result.IsSuccess ? result.Value : null);
    }

    public async Task<IActionResult> OnGetSearchBeneficiaryAsync(string term, int index, int? renewingMemberId = null)
    {
        var result = await mediator.Send(new SearchEligibleMembersQuery(term, renewingMemberId));
        ViewData["TargetIndex"] = index;
        return Partial("_BeneficiarySearchResults", result.IsSuccess ? result.Value : []);
    }
    
    private PartialViewResult Feedback(string? successMessage = null, string? errorMessage = null, string? targetUrl = null)
    {
        var partial = Partial("_FeedbackMessageModal", this); 

        if (!string.IsNullOrWhiteSpace(successMessage))
            partial.ViewData["SuccessMessage"] = successMessage;

        if (!string.IsNullOrWhiteSpace(errorMessage))
            partial.ViewData["ErrorMessage"] = errorMessage;
        
        if (!string.IsNullOrWhiteSpace(targetUrl))
            partial.ViewData["TargetUrl"] = targetUrl; // Pasamos la URL al modal

        return partial;
    }
}