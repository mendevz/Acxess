using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.IntegrationServices;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Acxess.Membership.Application.Features.Members.Queries;

public record GetMemberHistoryQuery(
    int IdMember,
    bool ShowAll) : IRequest<Result<MemberHistoryDto>>, ITenantRequest
{
    public int IdTenant { get; set; }
}
public class GetMemberHistoryHandler(
    MembershipModuleContext context,
    IBillingIntegrationService billingService,
    ITimeService timeService): IRequestHandler<GetMemberHistoryQuery, Result<MemberHistoryDto>>
{
    public async Task<Result<MemberHistoryDto>> Handle(GetMemberHistoryQuery request, CancellationToken cancellationToken)
    {

        var utcToday = timeService.GetUtcNow();
        var takeCount = request.ShowAll ? int.MaxValue : 5;
        
        var transactions = await billingService.GetMemberTransactionsAsync(request.IdMember, cancellationToken);

        var subscriptionDates = await context.Subscriptions
            .AsNoTracking()
            .Where(s => s.SubscriptionMembers.Any(sm => sm.IdMember == request.IdMember))
            .Select(s => new { s.EndDate, s.StartDate, s.CancelledAt, s.CreatedAt,s.CancellationReason })
            .OrderByDescending(s => s.CreatedAt)
            .Take(takeCount)
            .ToListAsync(cancellationToken);

        var timeZoneId = await timeService.GetTenantTimeZoneIdAsync(request.IdTenant, cancellationToken);

        var timeline = transactions.Select(tx => new TimelineItemDto
        {
            Title = "Pago Recibido", 
            Date = timeService.ConvertDateFromZoneId(tx.Date, timeZoneId),
            Amount = tx.TotalAmount,
            Type = "Payment",
            Icon = "check-circle",
            ColorClass = "green",
            Details = [.. tx.ItemNames.Select(d => $"{d.Description} - {d.Total.ToString("C", new CultureInfo("es-MX"))}")]
        })
        .ToList();

        foreach (var sub in subscriptionDates)
        {
            var createdAtLocal = timeService.ConvertDateFromZoneId(sub.CreatedAt,timeZoneId);
            var endDateAtLocal = timeService.ConvertDateFromZoneId(sub.EndDate, timeZoneId);
   
            timeline.Add(new TimelineItemDto
            {
                Title = "Membresía Activada", 
                Date = createdAtLocal,
                Amount = null,
                Type = "SubscriptionStart", 
                ColorClass = "blue",
                Details = [] 
            });


            if (sub.CancelledAt.HasValue)
            {
                var canceledAtLocal =  timeService.ConvertDateFromZoneId(sub.CancelledAt.Value, timeZoneId);

                timeline.Add(new TimelineItemDto
                {
                    Title = "Membresía Cancelada",
                    Date = canceledAtLocal,
                    Amount = null,
                    Type = "Expiration", 
                    ColorClass = "red",
                    Details = [sub.CancellationReason ?? "Sin motivo especificado"]
                });
            }
            else if (sub.EndDate < utcToday)
            {

                bool hasSeamlessRenewal = subscriptionDates.Any(otherSub =>
                    otherSub.StartDate <= sub.EndDate 
                    && otherSub.EndDate > sub.EndDate
                );

                if (!hasSeamlessRenewal)
                {
                    timeline.Add(new TimelineItemDto
                    {
                        Title = "Membresía Vencida",
                        Date = endDateAtLocal,
                        Amount = null,
                        Type = "Expiration",
                        ColorClass = "red",
                        Details = []
                    });
                }
            }
        }
        
        var finalItems = timeline
            .OrderByDescending(x => x.Date)
            .Take(takeCount)
            .ToList();
        
        return new MemberHistoryDto { 
            MemberId = request.IdMember, 
            Items = finalItems 
        };
    }
}