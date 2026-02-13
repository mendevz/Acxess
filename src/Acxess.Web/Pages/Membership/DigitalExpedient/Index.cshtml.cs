using Acxess.Membership.Application.Features.Members.Queries.GetMembers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acxess.Web.Pages.Membership.DigitalExpedient;

public class IndexModel(IMediator mediator) : PageModel
{
    
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
    public void OnGet()
    {
        
    }
}