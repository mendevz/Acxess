using Acxess.Membership.Application.Features.Subscriptions.Commands.SendDailyExpirationReminders;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Acxess.Membership.Infrastructure.BackgroundJobs;

public class WhatsAppDailyReportWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<WhatsAppDailyReportWorker> logger) : BackgroundService
{
    private const int TargetHour = 8;
    private const int TargetMinute = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string JobName = nameof(WhatsAppDailyReportWorker);

        using (LogContext.PushProperty("BackgroundJob", JobName))
        {
            logger.LogInformation("Background job started | JobName: {JobName}", JobName);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddHours(TargetHour).AddMinutes(TargetMinute);

                if (now >= nextRun)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                logger.LogInformation("Scheduled next execution | NextRun: {NextRun}, DelayHours: {DelayHours:F2}",
                    nextRun, delay.TotalHours);

                try
                {
                    await Task.Delay(delay, stoppingToken);

                    logger.LogInformation("Executing WhatsApp daily reports job");
                    var stopwatch = Stopwatch.StartNew();

                    using var scope = scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var result = await mediator.Send(new SendDailyExpirationRemindersCommand(), stoppingToken);

                    stopwatch.Stop();

                    if (result.IsSuccess)
                    {

                        logger.LogInformation("WhatsApp daily reports job completed | DurationMs: {DurationMs}",
                            stopwatch.ElapsedMilliseconds);
                    }
                    else
                    {

                        logger.LogError("WhatsApp daily reports job failed | ErrorCode: {ErrorCode}, DurationMs: {DurationMs}",
                            result.Error.Code, stopwatch.ElapsedMilliseconds);
                    }

                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Background job canceled | JobName: {JobName}", JobName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception in background job | JobName: {JobName}", JobName);
                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                }
            }
        }
    }
}
