using Acxess.Billing.Domain.Entities;
using Acxess.Billing.Infrastructure.Persistence;
using Acxess.Shared.IntegrationServices.Catalog;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Acxess.Billing.Application.Features.Transactions.Commands.NewVisitTransaction;

public class CreateVisitTransactionHandler(
    BillingModuleContext context,
    ICatalogIntegrationService catalogService,
    ILogger<CreateVisitTransactionHandler> logger) : IRequestHandler<CreateVisitTransactionCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateVisitTransactionCommand request, CancellationToken cancellationToken)
    {
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