using Acxess.Billing.Domain.Enums;
using Acxess.Shared.Abstractions;

namespace Acxess.Billing.Domain.Entities;

public class MemberTransaction : IHasTenant
{
    public int IdMemberTransaction { get; private set; }
    public int IdTenant { get; private set; }
    public int? IdMember { get; private set; }
    public string? Member { get; private set; }
    public int IdPaymentMethod { get; private set; }
    public decimal Total { get; private set; }
    public decimal Received { get; private set; }
    public decimal Difference { get; private set; }
    public string? Notes { get; private set; }
    public DateTime TransactionDate { get; private set; } = DateTime.Now;
    public int CreatedByUser { get; private set; }
    
    private readonly List<MemberTransactionDetail> _details = [];
    public virtual IReadOnlyCollection<MemberTransactionDetail> Details => _details.AsReadOnly();

    private MemberTransaction()
    {
    }
    private MemberTransaction(
        int idTenant, 
        int? idMember, 
        string? member,
        int idPaymentMethod, 
        DateTime date, 
        string? notes, 
        int createdByUser, 
        decimal received)
    {
        IdTenant = idTenant;
        IdMember = idMember;
        Member = member; 
        IdPaymentMethod = idPaymentMethod;
        TransactionDate = date;
        Notes = notes;
        CreatedByUser = createdByUser;
        Received = received;
        Total = 0;
    }

    public static MemberTransaction Create(
        int idTenant, int? idMember, string? member, int idPaymentMethod, decimal received, int userId, string? notes = null)
    {
        return new MemberTransaction(
            idTenant, 
            idMember,member, 
            idPaymentMethod, 
            DateTime.Now, 
            notes, 
            userId, 
            received);
    }
    
    public void AddSubscriptionItem(int subscriptionId, string planName, decimal price)
    {
        var detail = new MemberTransactionDetail(
            IdMemberTransaction, 
            IdTenant,
            ItemTransactionType.Subscription,
            description: planName,
            quantity: 1, 
            unitPrice: price,
            idSubscription: subscriptionId,
            idItem: null
        );

        _details.Add(detail);
        RecalculateTotal();
    }
    
    public void AddOnItem(int itemId, string productName, int quantity, decimal unitPrice)
    {
        var detail = new MemberTransactionDetail(
            IdMemberTransaction,
            IdTenant,
            ItemTransactionType.AddOn, 
            description: productName,
            quantity: quantity,
            unitPrice: unitPrice,
            idSubscription: null,
            idItem: itemId
        );

        _details.Add(detail);
        RecalculateTotal();
    }
    
    private void RecalculateTotal()
    {
        Total = _details.Sum(d => d.TotalLine);
        Difference  = Total - Received;
    }
}
