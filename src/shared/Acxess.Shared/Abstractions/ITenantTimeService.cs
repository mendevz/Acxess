
namespace Acxess.Shared.Abstractions;

public interface ITenantTimeService
{
    DateTime GetUtcNow();
    Task<DateTime> GetLocalNowAsync(CancellationToken cancellationToken = default);
    Task<DateTime> GetLocalTodayAsync(CancellationToken cancellationToken = default);
}
