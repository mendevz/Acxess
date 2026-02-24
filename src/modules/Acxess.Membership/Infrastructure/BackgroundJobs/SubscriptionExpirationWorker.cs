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
        while (stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Iniciando revisión de suscripciones vencidas...");
                
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();
                
                var today = DateTime.Now.Date;

                var expiredSubscriptions = await context.Subscriptions
                    .Where(s => s.IsActive && s.EndDate < today)
                    .ToListAsync(stoppingToken);
                
                if (expiredSubscriptions.Count != 0)
                {
                    foreach (var sub in expiredSubscriptions)
                    {
                        sub.Deactivate(); 
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    
                    logger.LogInformation($"Se desactivaron {expiredSubscriptions.Count} suscripciones.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error crítico al procesar expiraciones.");
            }
            
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(1);
        
            if (now.Hour < 1) 
            {
                nextRun = now.Date.AddHours(1); 
            }

            var delay = nextRun - now;
            logger.LogInformation($"Siguiente corte programado en {delay.TotalHours:F2} horas.");
            await Task.Delay(delay, stoppingToken);
        }
    }
}