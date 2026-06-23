using Acxess.Membership.Domain.Services;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.IntegrationServices;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Subscriptions.Queries;

public record SubscriptionExpirationDto(
    DateTime StartDate,
    DateTime EndDate,
    bool IsRenewal
);
public record CalculateExpirationQuery(
    int PlanId,
    int? MemberId) : IRequest<Result<SubscriptionExpirationDto>>, ITenantRequest
{
    public int IdTenant { get; set; }
}
public class CalculateExpirationHandler(
    MembershipModuleContext membershipContext,
    ICatalogIntegrationService catalogIntegration,
    ITimeService timeService) : IRequestHandler<CalculateExpirationQuery, Result<SubscriptionExpirationDto>>
{
    public async Task<Result<SubscriptionExpirationDto>> Handle(CalculateExpirationQuery request, CancellationToken cancellationToken)
    {
        var planData = await catalogIntegration.GetPlanInfoAsync(request.PlanId, cancellationToken);

        var utcToday = timeService.GetUtcNow();
        var startDate = utcToday;
        var isRenewal = false;

        if (request.MemberId.HasValue && request.MemberId.Value > 0)
        {
            var maxExpiration = await membershipContext.Members
                .AsNoTracking()
                .Where(m => m.IdMember == request.MemberId.Value)
                .SelectMany(m => m.SubscriptionMemberships)
                .Where(sm => !sm.Subscription.CancelledAt.HasValue && sm.Subscription.EndDate >= utcToday)
                .MaxAsync(sm => (DateTime?)sm.Subscription.EndDate, cancellationToken);

            if (maxExpiration.HasValue)
            {
                isRenewal = true;
                startDate = maxExpiration.Value;
            }
        }

        var localStartDate = await timeService.ConvertUtcToLocalAsync(startDate, request.IdTenant, cancellationToken);

        var endDate = SubscriptionDateCalculator.CalculateEndDate(
            localStartDate,
            planData.Value.Duration,
            planData.Value.DurationUnit);

        return new SubscriptionExpirationDto(localStartDate, endDate, isRenewal);
    }
}
