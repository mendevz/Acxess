using Acxess.Membership.Application.Features.Subscriptions.Commands;
using Acxess.Shared.IntegrationServices;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Acxess.Membership.Infrastructure.BackgroundJobs;

public class WhatsAppDailyReportWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<WhatsAppDailyReportWorker> logger) : BackgroundService
{
    private const int TargetHour = 8;
    private const int TargetMinute = 30;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string JobName = nameof(WhatsAppDailyReportWorker);

        using (LogContext.PushProperty("BackgroundJob", JobName))
        {
            logger.LogInformation("Background job started | JobName: {JobName} ", JobName);

            logger.LogInformation("Waiting 30 seconds for database migrations to stabilize...");
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Executing WhatsApp daily reports job");

                    using var scope = scopeFactory.CreateScope();
                    var identityIntegration = scope.ServiceProvider.GetRequiredService<IIdentityIntegrationService>();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var activeTenants = await identityIntegration.GetActiveTenantsWithTimeZonesAsync(stoppingToken);

                    if (activeTenants.Count == 0)
                    {
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                        continue;
                    }

                    var utcNow = timeProvider.GetUtcNow().UtcDateTime;
                    DateTime? nearestRunUtc = null;
                    List<int> tenantsToProcessNext = []; // Guardaremos los IDs en lugar de solo el string
                    List<string> timeZonesToProcess = [];

                    foreach (var tenant in activeTenants)
                    {
                        var safeTzId = string.IsNullOrEmpty(tenant.TimeZoneId) ? "America/Mexico_City" : tenant.TimeZoneId;
                        var zoneInfo = TimeZoneInfo.FindSystemTimeZoneById(safeTzId);
                        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, zoneInfo);

                        var nextLocalRun = new DateTime(localTime.Year, localTime.Month, localTime.Day, TargetHour, TargetMinute, 0, DateTimeKind.Unspecified);

                        if (localTime >= nextLocalRun)
                            nextLocalRun = nextLocalRun.AddDays(1);

                        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocalRun, zoneInfo);

                        if (nearestRunUtc == null || nextUtc < nearestRunUtc)
                        {
                            nearestRunUtc = nextUtc;
                            timeZonesToProcess.Clear();
                            tenantsToProcessNext.Clear();

                            timeZonesToProcess.Add(safeTzId);
                            tenantsToProcessNext.Add(tenant.TenantId);
                        }
                        else if (nextUtc == nearestRunUtc)
                        {
                            if (!timeZonesToProcess.Contains(safeTzId)) timeZonesToProcess.Add(safeTzId);
                            tenantsToProcessNext.Add(tenant.TenantId);
                        }
                    }

                    var delay = nearestRunUtc!.Value - timeProvider.GetUtcNow().UtcDateTime;

                    if (delay > TimeSpan.Zero)
                    {
                        logger.LogInformation("Sleeping for {DelayHours:F2} hours. Next zones: {Zones}",
                            delay.TotalHours, string.Join(", ", timeZonesToProcess));
                        await Task.Delay(delay, stoppingToken);
                    }

                    logger.LogInformation("Woke up! Processing {TenantCount} tenants.", tenantsToProcessNext.Count);
                    var stopwatch = Stopwatch.StartNew();

                    foreach (var tenantId in tenantsToProcessNext)
                    {
                        var command = new SendDailyExpirationRemindersCommand { IdTenant = tenantId };
                        var result = await mediator.Send(command, stoppingToken);

                        if (result.IsFailure)
                        {
                            logger.LogError("Send WhatsApp daily reports failed For TenantId:{TenantId} | ErrorCode: {ErrorCode}, DurationMs: {DurationMs}",
                                    tenantId, result.Error.Code, stopwatch.ElapsedMilliseconds);
                        }
                    }

                    stopwatch.Stop();

                    logger.LogInformation("Batch completed in {DurationMs} ms", stopwatch.ElapsedMilliseconds);
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
