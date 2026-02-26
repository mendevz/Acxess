using Acxess.Membership.Domain.Constants;
using Acxess.Membership.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Services;

public interface ISubscriptionService
{
    Task DeactivateExpiredSubscriptionsAsync(CancellationToken ct);
}
public class SubscriptionService(MembershipModuleContext context, ILogger<SubscriptionService> logger) : ISubscriptionService
{
    public async Task DeactivateExpiredSubscriptionsAsync(CancellationToken ct)
    {
        var today = DateTime.Now.Date;
        const int gracePeriodDays = Configurations.PRORROGA_DAYS;
        var limitDate = today.AddDays(-gracePeriodDays);
        
        var expired = await context.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.IsActive && s.EndDate.Date < limitDate.Date)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        foreach (var sub in expired) {
            sub.Deactivate(); 
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Se procesaron {Count} expiraciones", expired.Count);
    }
}