using Acxess.Billing.Domain.Entities;
using Acxess.Billing.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.IntegrationServices;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Acxess.Billing.Application.Features.Transactions.Commands;

public record CreateVisitTransactionCommand(
    string VisitName,
    int PaymentMethodId,
    decimal AmountPaid,
    int UserId,
    List<int> AddOnIds
) : IRequest<Result<string>>, ITenantRequest
{
    public int IdTenant { get; set; }
}


public class CreateVisitTransactionHandler(
    BillingModuleContext context,
    ICatalogIntegrationService catalogService,
    ITimeService timeService,
    ILogger<CreateVisitTransactionHandler> logger) : IRequestHandler<CreateVisitTransactionCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateVisitTransactionCommand request, CancellationToken cancellationToken)
    {
        var utcToday = timeService.GetUtcNow();

        if (request.AddOnIds.Count == 0)
        {
            logger.LogWarning("Visit transaction failed: No items selected");
            return Result<string>.Failure("Visit.NoItems", "Debes seleccionar al menos un pase o complemento.");
        }

        var transaction = MemberTransaction.Create(
            request.IdTenant,
            null, 
            request.VisitName,
            request.PaymentMethodId,
            request.AmountPaid,
            request.UserId,
            utcToday,
            "Pase de Visita" 
        );
        
        var addOns = await catalogService.GetAddOnPriceBatchAsync(request.AddOnIds, cancellationToken);

        foreach (var addOn in addOns)
        {
            transaction.AddOnItem(
                addOn.Id, 
                addOn.Name, 
                1, 
                addOn.Price
            );
        }

        context.MemberTransactions.Add(transaction);
        
        await context.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Visit transaction {TransactionId} successfully created", 
            transaction.IdMemberTransaction);

        return "Visita registrada correctamente.";
    }
}