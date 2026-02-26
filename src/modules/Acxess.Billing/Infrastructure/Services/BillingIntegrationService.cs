using System.Globalization;
using Acxess.Billing.Infrastructure.Persistence;
using Acxess.Shared.IntegrationEvents.Billing;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Billing.Infrastructure.Services;

public class BillingIntegrationService(BillingModuleContext context) : IBillingIntegrationService
{
    public async Task<MemberFinancialStatsDto> GetMemberStatsAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var transactions = await context.MemberTransactions
            .AsNoTracking()
            .Where(t => t.IdMember == memberId)
            .Select(t => new { t.Total, t.TransactionDate })
            .ToListAsync(cancellationToken);

        if (transactions.Count == 0)
        {
            return new MemberFinancialStatsDto(0, 0, 0, "Sin Historial", "gray");
        }

        var totalSpent = transactions.Sum(t => t.Total);
        var totalTx = transactions.Count;
        var avgTicket = totalSpent / totalTx;

        var behavior = "Puntual";
        var color = "green";

        if (transactions.Count < 2) return new MemberFinancialStatsDto(totalSpent, avgTicket, totalTx, behavior, color);
        
        var gaps = new List<double>();
        
        for (var i = 1; i < transactions.Count; i++)
        {
            var diff = (transactions[i].TransactionDate - transactions[i - 1].TransactionDate).TotalDays;
            if (diff > 1)   gaps.Add(diff);
        }

        if (gaps.Count <= 0) return new MemberFinancialStatsDto(totalSpent, avgTicket, totalTx, behavior, color);
        
        var avgGap = gaps.Average();
            
        var recentGaps = gaps.TakeLast(3).ToList();
        var latePayments = recentGaps.Count(g => g > 35); 

        switch (latePayments)
        {
            case 0:
                behavior = "Puntual";
                color = "green";
                break;
            case 1:
                behavior = "Regular";
                color = "yellow";
                break;
            default:
                behavior = "Impuntual"; 
                color = "red";
                break;
        }

        return new MemberFinancialStatsDto(totalSpent, avgTicket, totalTx, behavior, color);
    }

    public async Task<List<MemberTransactionSummaryDto>> GetMemberTransactionsAsync(int memberId, CancellationToken cancellationToken = default)
    {
        return await context.MemberTransactions
            .AsNoTracking()
            .Where(t => t.IdMember == memberId)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new MemberTransactionSummaryDto(
                t.IdMemberTransaction,
                t.TransactionDate,
                t.Total,
                "Pagado", 
                t.Details.Select(d => $"{d.Description ?? "item"} - {d.TotalLine.ToString("C", new CultureInfo("es-MX"))}" ).ToList()
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RecentActivityDto>> GetRecentActivityAsync(int count, CancellationToken cancellationToken = default)
    {
        return await context.MemberTransactions
            .AsNoTracking()
            .OrderByDescending(t => t.TransactionDate)
            .Take(count)
            .Select(t => new RecentActivityDto(
                "Pago Recibido",
                t.Member ?? "",
                t.Total,
                t.TransactionDate,
                "payment"
            ))
            .ToListAsync(cancellationToken);
    }
}