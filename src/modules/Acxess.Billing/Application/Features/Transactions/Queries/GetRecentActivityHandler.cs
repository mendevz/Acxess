
using Acxess.Billing.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Acxess.Billing.Application.Features.Transactions.Queries;


public record RecentActivityDto(
    string Title,
    string Ticket,
    decimal Total,
    DateTime Date,
    string Status
    );

public record GetRecentActivityQuery(int Count) : IRequest<List<RecentActivityDto>>, ITenantRequest
{
    public int IdTenant { get; set; }
}
public class GetRecentActivityHandler(
    BillingModuleContext context,
    ITimeService timeService) : IRequestHandler<GetRecentActivityQuery, List<RecentActivityDto>>
{
    public async Task<List<RecentActivityDto>> Handle(GetRecentActivityQuery request, CancellationToken cancellationToken)
    {
        var transactionsRaw = await context.MemberTransactions
           .AsNoTracking()
           .OrderByDescending(t => t.TransactionDate)
           .Take(request.Count)
           .Select(t => new
           {
               t.Notes ,
               t.Member ,
               t.Total,
               t.TransactionDate
           })
           .ToListAsync(cancellationToken);

        var timeZoneId = await timeService.GetTenantTimeZoneIdAsync(request.IdTenant, cancellationToken);

        var transactions = transactionsRaw
           .Select(t => new RecentActivityDto(
               t.Notes ?? "Pago Recibido",
               t.Member ?? "",
               t.Total,
               timeService.ConvertDateFromZoneId(t.TransactionDate, timeZoneId),
               "payment"
           ))
           .ToList();

        return transactions;
    }
}
