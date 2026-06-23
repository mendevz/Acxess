using Acxess.Membership.Application.Features.Dashboard.DTOs;
using Acxess.Membership.Application.Features.Dashboard.Queries;
using Acxess.Shared.IntegrationServices;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acxess.Web.Pages;

public class IndexModel(IMediator mediator, IBillingIntegrationService billingService) : PageModel
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
        RecentActivity = await billingService.GetRecentActivityAsync(5);
    }
}
