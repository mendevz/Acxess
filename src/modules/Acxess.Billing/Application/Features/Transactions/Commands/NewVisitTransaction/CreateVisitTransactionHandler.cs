using Acxess.Billing.Domain.Abstractions;
using Acxess.Billing.Domain.Entities;
using Acxess.Billing.Infrastructure.Persistence;
using Acxess.Shared.IntegrationEvents.Catalog;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Billing.Application.Features.Transactions.Commands.NewVisitTransaction;

public class CreateVisitTransactionHandler(
    BillingModuleContext context,
    IBillingUnitOfWork unitOfWork,
    ICatalogIntegrationService catalogService) : IRequestHandler<CreateVisitTransactionCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateVisitTransactionCommand request, CancellationToken cancellationToken)
    {
        if (request.AddOnIds.Count == 0)
            return Result<string>.Failure("Visit.NoItems", "Debes seleccionar al menos un pase o complemento.");

        var transaction = MemberTransaction.Create(
            request.IdTenant,
            null, 
            request.VisitName,
            request.PaymentMethodId,
            request.AmountPaid,
            request.UserId,
            "Pase de Visita" 
        );

        foreach (var addOnId in request.AddOnIds)
        {
            var resultAddOn = await catalogService.GetAddOnPriceAsync(addOnId, cancellationToken);
            
            if (resultAddOn.IsFailure) 
                return Result<string>.Failure(resultAddOn.Error);

            transaction.AddOnItem(
                addOnId, 
                resultAddOn.Value.Name, 
                1, 
                resultAddOn.Value.Price
            );
        }

        context.MemberTransactions.Add(transaction);
        
        var resultSave = await unitOfWork.SaveChangesAsync(cancellationToken);

        return resultSave.IsFailure ? Result<string>.Failure(resultSave.Error) : Result<string>.Success("Visita registrada correctamente.");
    }
}