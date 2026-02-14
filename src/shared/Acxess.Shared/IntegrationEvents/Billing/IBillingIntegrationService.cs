namespace Acxess.Shared.IntegrationEvents.Billing;

public interface IBillingIntegrationService
{
    Task<MemberFinancialStatsDto> GetMemberStatsAsync(int memberId, CancellationToken cancellationToken = default);
    Task<List<MemberTransactionSummaryDto>> GetMemberTransactionsAsync(int memberId, CancellationToken cancellationToken = default);
}

public record MemberFinancialStatsDto(
    decimal TotalSpent, 
    decimal AverageTicket, 
    int TotalTransactions,
    string PaymentBehaviorLabel,
    string PaymentBehaviorColor 
);

public record MemberTransactionSummaryDto(
    int TransactionId,
    DateTime Date,
    decimal TotalAmount,
    string Status, // "Paid", "Pending"
    List<string> ItemNames // Lista de nombres de lo que compró (Plan + AddOns)
);