using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Acxess.Marketing.Application.Features.Coupons.Commands.AssignCoupon;
using Acxess.Marketing.Application.Features.Promotions.Queries.GetActiveCouponPromotions;
using Acxess.Membership.Application.Features.Members.Commands.DeleteMember;
using Acxess.Membership.Application.Features.Members.Commands.RestoreMember;
using Acxess.Membership.Application.Features.Members.Commands.UpdateMember;
using Acxess.Membership.Application.Features.Members.Commands.UpdateMemberPhoto;
using Acxess.Membership.Application.Features.Members.Queries.GetMemberById;
using Acxess.Membership.Application.Features.Members.Queries.GetMemberDetail;
using Acxess.Membership.Application.Features.Members.Queries.GetMemberHistory;
using Acxess.Membership.Application.Features.Members.Queries.GetMembers;
using Acxess.Membership.Application.Features.Subscriptions.Commands.CancelSubscription;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Acxess.Web.Pages.Membership.DigitalExpedient;

public class IndexModel(IMediator mediator) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? MemberId { get; set; }
    
    [BindProperty]
    public int AssignCouponMemberId { get; set; }
    
    [BindProperty]
    public int? SelectedPromotionId { get; set; }
    
    [BindProperty]
    public UpdateMemberInputModel EditMemberInput { get; set; } = new();
    
    public List<SelectListItem> ActivePromotions { get; set; } = [];
    
    public MemberDetailDto? SelectedMember { get; set; }
    public MemberHistoryDto? InitialMemberHistory { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; } = string.Empty;
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; } = "all";
    
    public MembersResponse? MembersResponse { get; set; }
    
    public async Task OnGet()
    {
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var membersResult = await mediator.Send(new GetMembersQuery(SearchTerm));
            if (membersResult.IsFailure || !membersResult.Value.Members.Any()) return; 
            
            MembersResponse = membersResult.Value;
            await LoadMemberDetailAndHistory(MembersResponse.Members.First().IdMember);
        }
        else if (MemberId is > 0)
        {
            var membersResult = await mediator.Send(new GetMembersQuery("", "all"));
            if (membersResult.IsSuccess) MembersResponse = membersResult.Value;
            
            await LoadMemberDetailAndHistory(MemberId.Value);
        }
        else
        {
            var membersResult = await mediator.Send(new GetMembersQuery("", "all"));
            if (membersResult.IsSuccess) MembersResponse = membersResult.Value;
        }
    }
    
    private async Task LoadMemberDetailAndHistory(int idMember)
    {
        var detailResult = await mediator.Send(new GetMemberDetailQuery(idMember));
        if (detailResult.IsSuccess)
        {
            SelectedMember = detailResult.Value;
        }
        
        var historyResult = await mediator.Send(new GetMemberHistoryQuery(idMember, ShowAll: false));
        if (historyResult.IsSuccess)
        {
            InitialMemberHistory = historyResult.Value;
        }
    }

    public async Task<IActionResult> OnGetMembersAsync()
    {
        var queryMembers = new GetMembersQuery(SearchTerm, StatusFilter??"all");
        var result = await mediator.Send(queryMembers);

        if (result.IsFailure) return Partial("_ErrorState", result.Error.Description);
        
        MembersResponse = result.Value;
        return Partial("_MembersList", this);

    }
    
    public async Task<IActionResult> OnGetMemberDetailAsync(int id)
    {
        var query = new GetMemberDetailQuery(id);
        var result = await mediator.Send(query);
       
        if (result.IsFailure) return Partial("_ErrorState", result.Error.Description);

        SelectedMember = result.Value;

        return Partial("_MemberDetails", this);
    }
    
    public async Task<IActionResult> OnGetMemberHistoryAsync(int id)
    {
        var result = await mediator.Send(new GetMemberHistoryQuery(id, ShowAll: false));
        return Partial("_MemberHistory", result.Value);
    }
    
    public async Task<IActionResult> OnGetFullHistoryAsync(int id)
    {
        var result = await mediator.Send(new GetMemberHistoryQuery(id, ShowAll: true));
        return Partial("_MemberHistoryModal", result.Value);
    }
    
    public async Task<IActionResult> OnGetEditMemberAsync(int id)
    {
        var query = new GetMemberByIdQuery(id);
        var result = await mediator.Send(query);

        if (result.IsFailure) return Content("Error al cargar datos");
        var member = result.Value;
                
        EditMemberInput = new UpdateMemberInputModel
        {
            Id = member.Id,
            FirstName = member.FirstName, 
            LastName = member.LastName,
            Phone = member.Phone,
            Email = member.Email
        };

        return Partial("_EditMemberModal", this);
    }
    
    public async Task<IActionResult> OnPostEditMemberAsync()
    {
        if (!TryValidateModel(EditMemberInput, nameof(EditMemberInput)))
        {
            return Partial("_EditMemberModal", this);
        }

        var command = new UpdateMemberCommand(
            EditMemberInput.Id,
            EditMemberInput.FirstName,
            EditMemberInput.LastName,
            EditMemberInput.Phone,
            EditMemberInput.Email
        );

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Description);
            return Partial("_EditMemberModal", this);
        }

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new { 
            memberUpdated = true 
        }));

        return Partial("_ActionSuccess", result.Value);
    }
    
    public async Task<IActionResult> OnGetAssignCouponAsync(int id)
    {
        await LoadPromotionsDropdown();
        AssignCouponMemberId = id; 

        return Partial("_AssignCouponModal", this);
    }
    
    private async Task LoadPromotionsDropdown()
    {
        var promotionsResult = await mediator.Send(new GetActiveCouponPromotionsQuery());
        if (promotionsResult.IsSuccess)
        {
            ActivePromotions = promotionsResult.Value
                .Select(p => new SelectListItem(p.DiscountLabel, p.Id.ToString()))
                .ToList();
        }
    }
    
    public async Task<IActionResult> OnPostAssignCouponAsync()
    {
        
        ModelState.ClearValidationState(nameof(EditMemberInput));
        ModelState.MarkFieldValid(nameof(EditMemberInput));
        
        var userNumberString = User.FindFirstValue("UserNumber");
        var userNumber = int.TryParse(userNumberString, out var val) ? val : 0;
        
        await LoadPromotionsDropdown();
        
        if (SelectedPromotionId is null or <= 0)
        {
            ModelState.AddModelError(string.Empty, "Debes seleccionar una promoción válida.");
            return Partial("_AssignCouponModal", this);
        }
        
        var command = new AssignCouponCommand(AssignCouponMemberId, SelectedPromotionId ?? 0, userNumber);
        var result = await mediator.Send(command);

        if (result.IsSuccess) return Partial("_ActionSuccess", result.Value);
        
        ModelState.AddModelError(string.Empty, result.Error.Description);
        return Partial("_AssignCouponModal", this);

    }
    
    public async Task<IActionResult> OnGetCancelSubscriptionModalAsync(int id)
    {
        await OnGetMemberDetailAsync(id);
        
        if (SelectedMember == null || !SelectedMember.HasActiveSubscription)
            return Content(""); 

        return Partial("_CancelSubscriptionModal", this);
    }
    
    public async Task<IActionResult> OnPostCancelSubscriptionAsync(int subscriptionId, int memberId, string reason)
    {
        var userNumberString = User.FindFirstValue("UserNumber");
        var userNumber = int.TryParse(userNumberString, out var val) ? val : 0;

        var command = new CancelSubscriptionCommand(subscriptionId, reason, userNumber);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Description);
            await OnGetMemberDetailAsync(memberId);
            return Partial("_CancelSubscriptionModal", this);
        }

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new { 
            memberUpdated = true, 
            reloadMembersList = true
        }));

        return Partial("_ActionSuccess", "Suscripción cancelada correctamente.");
    }
    
    public async Task<IActionResult> OnGetDeleteMemberModalAsync(int id)
    {
        await OnGetMemberDetailAsync(id);
        return Partial("_DeleteMemberModal", this);
    }
    
    public async Task<IActionResult> OnPostDeleteMemberAsync(int id)
    {
        var userNumber = int.Parse(User.FindFirstValue("UserNumber") ?? "0");
        var result = await mediator.Send(new DeleteMemberCommand(id, userNumber));

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Description);
            await OnGetMemberDetailAsync(id);
            return Partial("_DeleteMemberModal", this); 
        }

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new { 
            memberUpdated = true, 
            reloadMembersList = true
        }));

        return Partial("_ActionSuccess", "Socio eliminado correctamente.");
    }
    
    public async Task<IActionResult> OnPostRestoreMemberAsync(int id)
    {
        var result = await mediator.Send(new RestoreMemberCommand(id));

        if (result.IsFailure) return BadRequest(result.Error.Description);

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new { memberUpdated = true, reloadMembersList = true }));
        return Partial("_ActionSuccess", "Socio restaurado exitosamente.");
    }
    
    public async Task<IActionResult> OnPostUpdatePhotoAsync(int id, string photoBase64)
    {
        if (string.IsNullOrWhiteSpace(photoBase64)) return BadRequest("No se recibió ninguna imagen.");

        var result = await mediator.Send(new UpdateMemberPhotoCommand(id, photoBase64));

        if (result.IsFailure) return Content("Error al actualizar la foto.");

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new { 
            memberUpdated = true, 
            reloadMembersList = true 
        }));
        return Content(""); 
    }
}