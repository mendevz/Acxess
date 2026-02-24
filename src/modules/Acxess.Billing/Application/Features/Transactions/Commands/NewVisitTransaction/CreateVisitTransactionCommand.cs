using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Billing.Application.Features.Transactions.Commands.NewVisitTransaction;

public record CreateVisitTransactionCommand(
    int IdTenant,
    string VisitName,
    int PaymentMethodId,
    decimal AmountPaid,
    int UserId,
    List<int> AddOnIds
) : IRequest<Result<string>>;