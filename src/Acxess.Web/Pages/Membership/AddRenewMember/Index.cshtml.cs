using Acxess.Billing.Application.Features.Transactions.Commands;
using Acxess.Catalog.Application.Features.AddOns.DTOs;
using Acxess.Catalog.Application.Features.AddOns.Queries;
using Acxess.Catalog.Application.Features.SellingPlans.DTOs;
using Acxess.Catalog.Application.Features.SellingPlans.Queries;
using Acxess.Membership.Application.Features.Members.Commands;
using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Membership.Application.Features.Members.Queries;
using Acxess.Membership.Application.Features.Subscriptions.Queries;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace Acxess.Web.Pages.Membership.AddRenewMember;

public class IndexModel(IMediator mediator) : PageModel
{

    [BindProperty] public ProcessOrderRequest OrderRequest { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? SearchMember { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public int? MemberId { get; set; }
    
    public List<SellingPlanDto> PlanItems { get; private set; } = [];
    public List<AddOnDto> AddOnsItems { get; private set; } = [];
    public List<MemberResponse> Members { get; private set; } = [];
    public string InscriptionJson { get; private set; } = string.Empty;
    public string PreselectedMemberJson { get; private set; } = string.Empty;

    public async Task OnGet()
    {
        await LoadCatalogsAsync();
        await LoadPreselectedMemberAsync();
    }
    
    private async Task LoadCatalogsAsync()
    {
        var inscResult = await mediator.Send(new GetAddOnInscriptionQuery());
        if (inscResult.IsSuccess) InscriptionJson = JsonSerializer.Serialize(inscResult.Value);   
        
        var planResult = await mediator.Send(new GetSellingPlanQuery(false));
        if (planResult.IsSuccess) PlanItems = planResult.Value;
        
        var addOnsResult = await mediator.Send(new GetAddOnsQuery(false));
        if (addOnsResult.IsSuccess) AddOnsItems = addOnsResult.Value;
    }

    private async Task LoadPreselectedMemberAsync()
    {
        var queryParam = MemberId > 0 ? MemberId.Value.ToString() : 
            !string.IsNullOrWhiteSpace(SearchMember) ? SearchMember : null;

        if (queryParam == null) return;

        var resultMember = await mediator.Send(new GetMemberToRenewQuery(queryParam));
        if (resultMember is { IsSuccess: true, Value.Count: > 0 })
        {
            var memberToSelect = MemberId > 0 
                ? resultMember.Value.FirstOrDefault(m => m.Id == MemberId.Value) 
                : resultMember.Value.FirstOrDefault();

            if (memberToSelect != null)
            {
                PreselectedMemberJson = JsonSerializer.Serialize(memberToSelect);
            }
        }
    }

    public async Task<IActionResult> OnGetMemberAsync()
    {
        var result = await mediator.Send(new GetMemberToRenewQuery(SearchMember));
        if (result.IsFailure) return Partial("_ErrorState", result.Error.Description);
        
        Members = result.Value;
        return Partial("_MemberList", this);
    }

    public async Task<IActionResult> OnPostProcessOrderAsync()
    {
        if (!ModelState.IsValid) return Feedback(errorMessage: "Error de validaciones"); 
        
        var userNumberString = User.FindFirstValue("UserNumber");

        if (!int.TryParse(userNumberString, out var userNumber) || userNumber == 0)
            return Feedback(errorMessage: "No estás autenticado");
            
        var paymentMethodId = OrderRequest.PaymentMethod == "cash" ? 1 : 2;
        var beneficiaries = MapBeneficiaries(OrderRequest.AdditionalBeneficiaries);
        
        return OrderRequest.Mode switch
        {
            ProcessOrderRequest.VISIT_MEMBER => await ProcessVisitAsync(paymentMethodId, userNumber),
            ProcessOrderRequest.NEW_MEMBER => await ProcessNewMemberAsync(paymentMethodId, userNumber, beneficiaries),
            _ => await ProcessRenewMemberAsync(paymentMethodId, userNumber, beneficiaries)
        };
    }
    
    private async Task<IActionResult> ProcessVisitAsync( int paymentMethodId, int userNumber)
    {
        var visitName = string.IsNullOrWhiteSpace(OrderRequest.MemberData.FirstName) 
            ? "Visita General" 
            : OrderRequest.MemberData.FirstName;

        var command = new CreateVisitTransactionCommand(
           visitName, 
           paymentMethodId, 
           OrderRequest.AmountPaid, 
           userNumber, 
           OrderRequest.AddOnIds);
        
        var result = await mediator.Send(command);
        
        return result.IsFailure 
            ? Feedback(errorMessage: result.Error.Description)
            : Feedback(successMessage: result.Value);
    }
    
    private async Task<IActionResult> ProcessNewMemberAsync(int paymentMethodId, int userNumber, List<NewMemberDto> beneficiaries)
    {
        var member = new NewMemberDto(0, OrderRequest.MemberData.FirstName ?? "", OrderRequest.MemberData.LastName ?? "", OrderRequest.MemberData.Phone, OrderRequest.MemberData.PhotoBase64);
        var command = new NewMemberCommand(
            member, 
            OrderRequest.PlanId ?? 0, 
            OrderRequest.AddOnIds, 
            paymentMethodId, 
            OrderRequest.AmountPaid, 
            beneficiaries, 
            userNumber,
            OrderRequest.RequireInscription,
            OrderRequest.IdempotencyToken);
        
        return  HandleMemberResultAsync(await mediator.Send(command));
    }

    private async Task<IActionResult> ProcessRenewMemberAsync( int paymentMethodId, int userNumber, List<NewMemberDto> beneficiaries)
    {
        var command = new RenewMemberCommand(
            OrderRequest.MemberData.Id, 
            OrderRequest.PlanId ?? 0, 
            OrderRequest.AddOnIds, 
            paymentMethodId, 
            OrderRequest.AmountPaid, 
            beneficiaries, 
            userNumber,
            OrderRequest.IdempotencyToken,
            OrderRequest.MemberData.PhotoBase64);
        
        return  HandleMemberResultAsync(await mediator.Send(command));
    }
    
    private PartialViewResult HandleMemberResultAsync(Result<UpdatedSubMemberResponse> result)
    {
        if (result.IsFailure) return Feedback(errorMessage: result.Error.Description);

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

    public async Task<IActionResult> OnGetCalculateExpirationAsync(int planId, int? memberId)
    {
        if (planId <= 0) return Content("---");

        var query = new CalculateExpirationQuery(planId, memberId);
        var result = await mediator.Send(query);

        if (result.IsFailure) return Content("---");

        var dto = result.Value;

        var culture = new System.Globalization.CultureInfo("es-MX");

        string startStr = dto.StartDate.ToString("dd MMM yyyy", culture);
        string endStr = dto.EndDate.ToString("dd MMM yyyy", culture);

        string notaVigenciaHtml = "<div id='nota-vigencia' hx-swap-oob='true'></div>"; // Vacío por defecto

        if (dto.IsRenewal)
        {
            notaVigenciaHtml = $@"            
            <div id='nota-vigencia' hx-swap-oob='true' class='text-[11px] text-blue-600/80 dark:text-blue-400 mt-2 italic border-t border-blue-200 dark:border-blue-800 pt-2 leading-tight'>                
                * Al estar vigente, los días del nuevo plan se sumarán a partir del <span class='font-bold'>{startStr}</span>.            
            </div>";
        }

        string htmlResponse = $@"        
        <strong id='fecha-inicio' hx-swap-oob='true' class='text-base md:text-lg text-blue-900 dark:text-blue-200'>            
            {startStr}        
        </strong>        
        {notaVigenciaHtml}        
        {endStr}";

        return Content(htmlResponse, "text/html");
    }

    private static List<NewMemberDto> MapBeneficiaries(IEnumerable<AddRenewMemberInput> requestBeneficiaries)
    {
        return requestBeneficiaries.Select(b => new NewMemberDto(
            b.Id,
            b.FirstName ?? "",
            b.LastName ?? "",
            b.Phone)).ToList();
    }
}
