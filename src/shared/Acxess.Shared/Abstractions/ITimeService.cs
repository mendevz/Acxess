
namespace Acxess.Shared.Abstractions;

public interface ITimeService
{
    DateTime GetUtcNow();
    DateTime ConvertDateFromZoneId(DateTime date, string timeZoneId);
    Task<DateTime> GetLocalNowAsync(int idTenant, CancellationToken cancellationToken = default);
    Task<DateTime> GetLocalTodayAsync(int idTenant, CancellationToken cancellationToken = default);
    Task<DateTime> ConvertUtcToLocalAsync(DateTime utcDateTime, int tenantId, CancellationToken cancellationToken = default);
    Task<string> GetTenantTimeZoneIdAsync( int tenantId, CancellationToken cancellationToken = default);
}
