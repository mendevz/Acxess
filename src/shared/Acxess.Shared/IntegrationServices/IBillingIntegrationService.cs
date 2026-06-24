namespace Acxess.Shared.IntegrationServices;

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
    string Status,
    List<ItemTransactionDetail> ItemNames 
);

public record ItemTransactionDetail(string Description, decimal Total);
