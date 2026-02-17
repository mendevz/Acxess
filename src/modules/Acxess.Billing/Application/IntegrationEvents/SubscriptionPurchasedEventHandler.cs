using Acxess.Billing.Domain.Abstractions;
using Acxess.Billing.Domain.Entities;
using Acxess.Billing.Infrastructure.Persistence;
using Acxess.Shared.Exceptions;
using Acxess.Shared.IntegrationEvents.Membership;
using MediatR;

namespace Acxess.Billing.Application.IntegrationEvents;

public class SubscriptionPurchasedEventHandler(
    BillingModuleContext billingContext,
    IBillingUnitOfWork  unitOfWork) : INotificationHandler<SubcriptionPurchasedDomainEvent> {
    public async Task Handle(SubcriptionPurchasedDomainEvent notification, CancellationToken cancellationToken)
    {
        var transaction = MemberTransaction.Create(
            notification.IdTenant,
            notification.IdMember,
            notification.Member,
            notification.IdPaymentMethod,
            notification.AmountReceived,
            notification.CreatedByUserId,
            "Subscription Purchased");
        
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
        var result = await unitOfWork.SaveChangesAsync(cancellationToken);


        if (result.IsFailure)
        {
            throw new IntegrationEventException(result.Error);
        }

    }
}