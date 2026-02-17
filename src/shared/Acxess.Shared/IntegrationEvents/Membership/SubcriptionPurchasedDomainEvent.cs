using MediatR;

namespace Acxess.Shared.IntegrationEvents.Membership;

public record SubcriptionPurchasedDomainEvent(
    int IdTenant,
    int CreatedByUserId,
    int IdMember,
    int IdSubscription,
    string PlanName,
    decimal PlanPriceSnapshot,
    int IdPaymentMethod,
    decimal AmountReceived,
    string Member,
    List<PurchasedAddOnItem> AddOns
    ) : INotification;
    
    
public record PurchasedAddOnItem(
    int IdAddOn,
    string Name,   
    decimal Price 
);