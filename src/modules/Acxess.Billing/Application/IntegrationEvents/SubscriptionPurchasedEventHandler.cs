using Acxess.Billing.Domain.Entities;
using Acxess.Billing.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.IntegrationEvents.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Acxess.Billing.Application.IntegrationEvents;

public class SubscriptionPurchasedEventHandler(
    BillingModuleContext billingContext,
    ILogger<SubscriptionPurchasedEventHandler> logger,
    ITimeService timeService) : INotificationHandler<SubscriptionPurchasedIntegrationEvent> {
    public async Task Handle(SubscriptionPurchasedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing subscription purchase for Member {MemberId}. ", 
            notification.IdMember);

        var utcNow = timeService.GetUtcNow();

        var transaction = MemberTransaction.Create(
            notification.IdTenant,
            notification.IdMember,
            notification.Member,
            notification.IdPaymentMethod,
            notification.AmountReceived,
            notification.CreatedByUserId,
            utcNow,
            notification.IsNewMember ? "Nuevo Socio" : "Renovación Socio");
        
        transaction.AddSubscriptionItem(
            notification.IdSubscription, 
            notification.PlanName, 
            notification.PlanPriceSnapshot 
        );
        
        foreach(var addon in notification.AddOns)
        {
            transaction.AddOnItem(
                addon.IdAddOn, 
                addon.Name, 
                1, 
                addon.Price
            );
        }
        
        billingContext.MemberTransactions.Add(transaction);
        await billingContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Successfully registered transaction {TransactionId} for Member {MemberId}. Amount: {Amount}", 
            transaction.IdMemberTransaction, notification.IdMember, notification.AmountReceived);
    }
}