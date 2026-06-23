namespace Acxess.Membership.Application.Features.Subscriptions.Commands;

using Acxess.Shared.Abstractions;
using Acxess.Shared.IntegrationServices;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Acxess.Membership.Application.Features.Subscriptions.Queries;

public record SendDailyExpirationRemindersCommand() : IRequest<Result>, ITenantRequest
{
    public int IdTenant { get; set; }
}

public class SendDailyExpirationRemindersHandler(
    IMediator mediator,
    IWhatsAppService whatsAppService,
    IIdentityIntegrationService identityIntegrationService,
    ILogger<SendDailyExpirationRemindersHandler> logger) : IRequestHandler<SendDailyExpirationRemindersCommand, Result>
{
    public async Task<Result> Handle(SendDailyExpirationRemindersCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Executing daily expiration reminders orchestration");

        var expiringResult = await mediator.Send(
            new GetExpiringSubscriptionsQuery()
            {
                IdTenant = request.IdTenant
            }, 
            cancellationToken
        );

        if (expiringResult.IsFailure || expiringResult.Value == null || expiringResult.Value.Count == 0)
        {
            logger.LogInformation("No expiring subscriptions found | Action: Skipped");
            return Result.Success();
        }

        var tenantIdsWithExpirations = expiringResult.Value.Select(x => x.IdTenant).ToList();

        var adminsContacts = await identityIntegrationService.GetTenantAdminsContactsAsync(tenantIdsWithExpirations, cancellationToken);

        if (adminsContacts == null || adminsContacts.Count == 0)
        {
            logger.LogWarning("Tenant admins contacts not found | TenantsCount: {TenantsCount}", tenantIdsWithExpirations.Count);
            return Result.Failure(new Error("Notification.NoAdminsFound", "No se encontraron usuarios administradores con teléfono para los tenants afectados.", ErrorType.Failure));
        }

        var expirationMap = expiringResult.Value.ToDictionary(x => x.IdTenant);
        int totalDispatched = 0;
        int totalFailed = 0;

        foreach (var tenantContact in adminsContacts)
        {
            if (!expirationMap.TryGetValue(tenantContact.TenantId, out var membershipData))
                continue;

            foreach (var admin in tenantContact.Admins)
            {
                if (string.IsNullOrWhiteSpace(admin.PhoneNumber)) continue;

                // Construcción de mensaje formato Minimalista/Profesional
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"*Acxess* | _{tenantContact.TenantName}_");
                messageBuilder.AppendLine($"Hola {admin.FullName}, estas suscripciones vencen hoy:");
                messageBuilder.AppendLine();

                var counter = 1;
                foreach (var socio in membershipData.ExpiringMembers)
                {
                    messageBuilder.AppendLine($"{counter}. {socio.FullName} (ID: {socio.IdMember})");
                    counter++;
                }

                messageBuilder.AppendLine();
                messageBuilder.AppendLine("Sugerimos contactarlos para gestionar su renovación.");
                messageBuilder.AppendLine("—");
                messageBuilder.AppendLine("_Sistema automático. No responder._");

                string completeMessage = messageBuilder.ToString();

                logger.LogInformation("Dispatching WhatsApp message | TenantName: {TenantName}, AdminName: {AdminName}, Phone: {Phone}",
                    tenantContact.TenantName, admin.FullName, admin.PhoneNumber);

                bool sentSuccessfully = await whatsAppService.SendTextMessageAsync(admin.PhoneNumber, completeMessage, cancellationToken);

                if (sentSuccessfully)
                {
                    totalDispatched++;
                }
                else
                {
                    totalFailed++;
                    logger.LogError("WhatsApp message dispatch failed | TenantName: {TenantName}, AdminName: {AdminName}, Phone: {Phone}",
                        tenantContact.TenantName, admin.FullName, admin.PhoneNumber);
                }
            }
        }

        logger.LogInformation("Daily expiration reminders orchestration completed | TotalDispatched: {TotalDispatched}, TotalFailed: {TotalFailed}",
            totalDispatched, totalFailed);

        return Result.Success();
    }
}