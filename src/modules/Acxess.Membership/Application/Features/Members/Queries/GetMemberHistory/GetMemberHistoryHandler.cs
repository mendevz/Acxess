using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.IntegrationEvents.Billing;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMemberHistory;

public class GetMemberHistoryHandler(
    MembershipModuleContext context,
    IBillingIntegrationService billingService): IRequestHandler<GetMemberHistoryQuery, Result<MemberHistoryDto>>
{
    public async Task<Result<MemberHistoryDto>> Handle(GetMemberHistoryQuery request, CancellationToken cancellationToken)
    {
        var transactions = await billingService.GetMemberTransactionsAsync(request.IdMember, cancellationToken);

        var timeline = transactions.Select(tx => new TimelineItemDto
        {
            Title = "Pago Recibido", // O podrías poner "Renovación" si detectas que es un plan
            Date = tx.Date,
            Amount = tx.TotalAmount,
            Type = "Payment",
            Icon = "check-circle",
            ColorClass = "green",
            Details = tx.ItemNames
        })
        .ToList();

        var subscriptionDates = await context.Subscriptions
            .AsNoTracking()
            .Where(s => s.SubscriptionMembers.Any(sm => sm.IdMember == request.IdMember))
            .Select(s => new { s.EndDate, s.StartDate, s.CancelledAt, s.CreatedAt })
            .ToListAsync(cancellationToken);
        
        foreach (var sub in subscriptionDates)
        {
            timeline.Add(new TimelineItemDto
            {
                Title = "Membresía Activada", 
                Date = sub.CreatedAt,
                Amount = null,
                Type = "SubscriptionStart", // Nuevo Tipo
                ColorClass = "blue",
                Details = [] // O el nombre del plan si lo tienes
            });

            // 2. Evento: Vencimiento (Solo si ya venció)
            if (sub.EndDate < DateTime.Now.Date)
            {
                timeline.Add(new TimelineItemDto
                {
                    Title = "Membresía Vencida",
                    Date = sub.EndDate,
                    Amount = null,
                    Type = "Expiration",
                    ColorClass = "red",
                    Details = []
                });
            }

            if (sub.CancelledAt.HasValue)
            {
                timeline.Add(new TimelineItemDto
                {
                    Title = "Membresía Cancelada",
                    Date = sub.CancelledAt.Value,
                    Amount = null,
                    Type = "Expiration",
                    ColorClass = "red",
                    Details = []
                });
            }
            
        }
        
        var sortedTimeline = timeline.OrderByDescending(x => x.Date).ToList();
        
        var finalItems = request.ShowAll 
            ? sortedTimeline 
            : sortedTimeline.Take(5).ToList();
        
        return Result<MemberHistoryDto>.Success(new MemberHistoryDto { MemberId = request.IdMember, Items = finalItems });
    }
}