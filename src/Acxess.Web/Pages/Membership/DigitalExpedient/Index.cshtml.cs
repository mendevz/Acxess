using Acxess.Membership.Application.Features.Members.Queries.GetMemberDetail;
using Acxess.Membership.Application.Features.Members.Queries.GetMemberHistory;
using Acxess.Membership.Application.Features.Members.Queries.GetMembers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acxess.Web.Pages.Membership.DigitalExpedient;

public class IndexModel(IMediator mediator) : PageModel
{
    
    
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
    public void OnGet()
    {
        
    }
}