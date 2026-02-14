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

        // Lógica simple de comportamiento (puedes hacerla tan compleja como quieras)
        // Ej: Si sus últimos 3 pagos tienen más de 35 días de diferencia entre sí...
        var behavior = "Puntual"; 
        var color = "green";
        
        // (Aquí podrías meter lógica real comparando fechas)

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
                "Pagado", // Asumiendo que solo guardas pagadas por ahora
                t.Details.Select(d => $"{d.Description ?? "item"} - {d.TotalLine.ToString("C")}" ).ToList()
            ))
            .ToListAsync(cancellationToken);
    }
}