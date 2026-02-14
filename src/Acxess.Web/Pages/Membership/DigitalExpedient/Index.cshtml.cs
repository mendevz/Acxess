using System.Security.Claims;
using System.Text.Json;
using Acxess.Marketing.Application.Features.Coupons.Commands.AssignCoupon;
using Acxess.Marketing.Application.Features.Promotions.Queries.GetActiveCouponPromotions;
using Acxess.Membership.Application.Features.Members.Queries.GetMemberDetail;
using Acxess.Membership.Application.Features.Members.Queries.GetMemberHistory;
using Acxess.Membership.Application.Features.Members.Queries.GetMembers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Acxess.Web.Pages.Membership.DigitalExpedient;

public class IndexModel(IMediator mediator) : PageModel
{
    [BindProperty]
    public int AssignCouponMemberId { get; set; }
    
    [BindProperty]
    public int? SelectedPromotionId { get; set; }
    
    public List<SelectListItem> ActivePromotions { get; set; } = [];
    
    public MemberDetailDto? SelectedMember { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? SearchMember { get; set; } = string.Empty;

    public MembersResponse? MembersResponse { get; set; }

    public async Task<IActionResult> OnGetMembersAsync()
    {
        var queryMembers = new GetMembersQuery(SearchMember);
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
        var userNumberString = User.FindFirstValue("UserNumber");
        var userNumber = int.TryParse(userNumberString, out var val) ? val : 0;
        
        await LoadPromotionsDropdown();
        
        if (SelectedPromotionId  <= 0)
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
    public void OnGet()
    {
        
    }
}