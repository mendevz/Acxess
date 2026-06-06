namespace Acxess.Membership.Infrastructure.BackgroundJobs;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Acxess.Membership.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;

public class SubscriptionExpirationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string JobName = nameof(SubscriptionExpirationWorker);

        using (LogContext.PushProperty("BackgroundJob", JobName))
        {
            logger.LogInformation("Background job started | JobName: {JobName}", JobName);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1).AddHours(1);

                if (now.Hour < 1)
                {
                    nextRun = now.Date.AddHours(1);
                }

                var delay = nextRun - now;

                logger.LogInformation("Scheduled next execution | NextRun: {NextRun}, DelayHours: {DelayHours:F2}",
                    nextRun, delay.TotalHours);

                try
                {
                    await Task.Delay(delay, stoppingToken);

                    logger.LogInformation("Executing subscription expiration job");
                    var stopwatch = Stopwatch.StartNew();

                    using var scope = scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                    await service.DeactivateExpiredSubscriptionsAsync(stoppingToken);

                    stopwatch.Stop();

                    logger.LogInformation("Subscription expiration job completed | DurationMs: {DurationMs}",
                        stopwatch.ElapsedMilliseconds);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Background job canceled | JobName: {JobName}", JobName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception in background job | JobName: {JobName}", JobName);
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
}