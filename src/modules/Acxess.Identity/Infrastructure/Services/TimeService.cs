using Acxess.Identity.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Acxess.Identity.Infrastructure.Services;

public class TimeService(
    TimeProvider timeProvider,
    IdentityModuleContext identityModuleContext,
    IDistributedCache cache) : ITimeService
{

    public DateTime GetUtcNow() => timeProvider.GetUtcNow().UtcDateTime;

    public DateTime ConvertDateFromZoneId(DateTime date, string timeZoneId)
    {
        var zoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        if (date.Kind == DateTimeKind.Unspecified)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(date, zoneInfo);
    }
    public async Task<DateTime> ConvertUtcToLocalAsync(DateTime utcDateTime, int idTenant, CancellationToken cancellationToken = default)
    {
        var timeZoneId = await GetTenantTimeZoneIdAsync(idTenant, cancellationToken);

        return ConvertDateFromZoneId(utcDateTime, timeZoneId);
    }

    public async Task<DateTime> GetLocalNowAsync(int idTenant, CancellationToken cancellationToken = default)
    {
        var utcNow = GetUtcNow();

        var timeZoneId = await GetTenantTimeZoneIdAsync(idTenant, cancellationToken);

        return ConvertDateFromZoneId(utcNow, timeZoneId);
    }

    public async Task<DateTime> GetLocalTodayAsync(int idTenant, CancellationToken cancellationToken = default)
    {
        var localNow = await GetLocalNowAsync(idTenant,cancellationToken);
        return localNow.Date;
    }

    public async Task<string> GetTenantTimeZoneIdAsync(int idTenant, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"acxess:tenant:{idTenant}:timezone";

        var timeZoneId = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(timeZoneId))
        {
            timeZoneId = await identityModuleContext.Tenants
            .Where(t => t.IdTenant == idTenant)
            .Select(t => t.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

            timeZoneId ??= "America/Mexico_City";

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            };

            await cache.SetStringAsync(cacheKey, timeZoneId, cacheOptions, cancellationToken);
        }

        return timeZoneId;
    }
}
