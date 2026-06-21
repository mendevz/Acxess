using Acxess.Identity.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Amazon.Runtime.Internal.Util;
using Amazon.Util.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Acxess.Identity.Infrastructure.Services;

public class TenantTimeService(
    TimeProvider timeProvider,
    ICurrentTenant currentTenant,
    IdentityModuleContext identityModuleContext,
    IDistributedCache cache) : ITenantTimeService
{
    public async Task<DateTime> GetLocalNowAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = GetUtcNow();
        var tenantId = currentTenant.Id ?? 0;

        var cacheKey = $"acxess:tenant:{tenantId}:timezone";

        var timeZoneId = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(timeZoneId))
        {
            timeZoneId = await identityModuleContext.Tenants
            .Where(t => t.IdTenant == tenantId)
            .Select(t => t.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

            timeZoneId ??= "America/Mexico_City";

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            };

            await cache.SetStringAsync(cacheKey, timeZoneId, cacheOptions, cancellationToken);
        }

        var zoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, zoneInfo);
    }

    public async Task<DateTime> GetLocalTodayAsync(CancellationToken cancellationToken = default)
    {
        var localNow = await GetLocalNowAsync(cancellationToken);
        return localNow.Date;
    }

    public DateTime GetUtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}
