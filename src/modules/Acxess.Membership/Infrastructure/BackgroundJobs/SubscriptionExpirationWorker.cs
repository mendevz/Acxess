using Acxess.Membership.Application.Services;
using Acxess.Membership.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Acxess.Membership.Infrastructure.BackgroundJobs;

public class SubscriptionExpirationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionExpirationWorker> logger): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (LogContext.PushProperty("BackgroundJob", nameof(SubscriptionExpirationWorker)))
        {
            logger.LogInformation("The subscription expiration worker has started.");
       
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1).AddHours(1);
                
                if (now.Hour < 1) 
                {
                    nextRun = now.Date.AddHours(1);
                }

                var delay = nextRun - now;
                logger.LogInformation("Next scheduled run: {NextRun} (in {DelayHours} hours)", 
                        nextRun, delay.TotalHours);
                try 
                {
                    await Task.Delay(delay, stoppingToken);

                    var stopwatch = Stopwatch.StartNew();
                    logger.LogInformation("Starting the deactivation of expired subscriptions...");

                    using var scope = scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                    await service.DeactivateExpiredSubscriptionsAsync(stoppingToken);

                    stopwatch.Stop();
                    logger.LogInformation("Deactivation completed successfully in {ElapsedMilliseconds} ms", 
                        stopwatch.ElapsedMilliseconds);
                }
                catch (OperationCanceledException) { 
                    logger.LogWarning("The worker was cancelled by the system before finishing.");
                }
                catch (Exception ex) 
                {
                    logger.LogError(ex, "Critical error during the automatic execution of the membership deactivation.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
}