using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.IntegrationServices.Billing;
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
        
        var today = DateTime.Now.Date;
        var takeCount = request.ShowAll ? int.MaxValue : 5;
        
        var transactions = await billingService.GetMemberTransactionsAsync(request.IdMember, cancellationToken);

        var subscriptionDates = await context.Subscriptions
            .AsNoTracking()
            .Where(s => s.SubscriptionMembers.Any(sm => sm.IdMember == request.IdMember))
            .Select(s => new { s.EndDate, s.StartDate, s.CancelledAt, s.CreatedAt,s.CancellationReason })
            .OrderByDescending(s => s.CreatedAt)
            .Take(takeCount)
            .ToListAsync(cancellationToken);
        
        var timeline = transactions.Select(tx => new TimelineItemDto
        {
            Title = "Pago Recibido", 
            Date = tx.Date,
            Amount = tx.TotalAmount,
            Type = "Payment",
            Icon = "check-circle",
            ColorClass = "green",
            Details = tx.ItemNames
        })
        .ToList();

       
        
        foreach (var sub in subscriptionDates)
        {
            timeline.Add(new TimelineItemDto
            {
                Title = "Membresía Activada", 
                Date = sub.CreatedAt,
                Amount = null,
                Type = "SubscriptionStart", 
                ColorClass = "blue",
                Details = [] 
            });
            if (sub.CancelledAt.HasValue)
            {
                timeline.Add(new TimelineItemDto
                {
                    Title = "Membresía Cancelada",
                    Date = sub.CancelledAt.Value,
                    Amount = null,
                    Type = "Expiration", 
                    ColorClass = "red",
                    Details = [sub.CancellationReason ?? "Sin motivo especificado"]
                });
            }
            else if (sub.EndDate < DateTime.Now.Date)
            {

                bool hasSeamlessRenewal = subscriptionDates.Any(otherSub =>
                    otherSub.StartDate <= sub.EndDate && otherSub.EndDate > sub.EndDate);

                if (!hasSeamlessRenewal)
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
            }
        }
        
        var finalItems = timeline
            .OrderByDescending(x => x.Date)
            .Take(takeCount)
            .ToList();
        
        return Result<MemberHistoryDto>.Success(new MemberHistoryDto { MemberId = request.IdMember, Items = finalItems });
    }
}