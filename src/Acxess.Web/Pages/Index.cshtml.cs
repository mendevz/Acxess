using Acxess.Billing.Application.Features.Transactions.Queries;
using Acxess.Membership.Application.Features.Dashboard.DTOs;
using Acxess.Membership.Application.Features.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acxess.Web.Pages;

public class IndexModel(IMediator mediator) : PageModel
{
    public DashboardStatsDto Stats { get; private set; } = new();
    public List<RecentActivityDto> RecentActivity { get; private set; } = [];

    public async Task OnGet()
    {
        var statsResult = await mediator.Send(new GetDashboardStatsQuery());
        if (statsResult.IsSuccess)
        {
            Stats = statsResult.Value;
        }

        var recentActiivyQuery = new GetRecentActivityQuery(7);
        RecentActivity = await mediator.Send(recentActiivyQuery);
    }
}
