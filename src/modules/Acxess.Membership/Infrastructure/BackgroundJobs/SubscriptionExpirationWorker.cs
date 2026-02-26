using Acxess.Membership.Application.Services;
using Acxess.Membership.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Infrastructure.BackgroundJobs;

public class SubscriptionExpirationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionExpirationWorker> logger): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(1);
            
            if (now.Hour < 1) 
            {
                nextRun = now.Date.AddHours(1);
            }

            var delay = nextRun - now;
            logger.LogInformation("Siguiente ejecución programada para: {NextRun} (en {Delay} horas)", nextRun, delay.TotalHours);

            try 
            {
                await Task.Delay(delay, stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                await service.DeactivateExpiredSubscriptionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) 
            {
                logger.LogError(ex, "Error durante la ejecución automática del worker.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}