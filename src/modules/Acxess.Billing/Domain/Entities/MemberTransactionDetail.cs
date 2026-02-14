
using Acxess.Billing.Domain.Enums;
using Acxess.Shared.Abstractions;

namespace Acxess.Billing.Domain.Entities;

public class MemberTransactionDetail : IHasTenant
{
    public int IdMemberTransactionDetail { get; private set; }
    public int IdMemberTransaction { get; private set; }
    public int? IdSubscription { get; private set; }
    public int? IdItem { get; private set; }
    public ItemTransactionType ItemTransactionType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalLine { get; private set; }
    public int IdTenant { get; private set; }
    
    public virtual MemberTransaction Transaction { get; private set; } = null!;

    private MemberTransactionDetail() { }
    internal MemberTransactionDetail(
        int idMemberTransaction, 
        int idTenant,
        ItemTransactionType type, 
        string description, 
        int quantity, 
        decimal unitPrice, 
        int? idSubscription, 
        int? idItem)
    {
        IdTenant = idTenant;
        IdMemberTransaction = idMemberTransaction;
        ItemTransactionType = type;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        IdSubscription = idSubscription;
        IdItem = idItem;
        TotalLine = quantity * unitPrice;
    }

  
}
