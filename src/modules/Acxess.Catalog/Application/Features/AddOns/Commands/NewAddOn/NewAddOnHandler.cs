using Acxess.Catalog.Domain.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AddOns.Commands.NewAddOn;

public class NewAddOnHandler(
    ICatalogUnitOfWork unitOfWork,
    IAddOnRepository addOnRepository
) : IRequestHandler<NewAddOnCommand, Result<string>>
{
    public async Task<Result<string>> Handle(NewAddOnCommand request, CancellationToken cancellationToken)
    {
        var addOn = Domain.Entities.AddOn.Create(
            request.TenantId,
            request.AddOnKey,
            request.Name,
            request.Price,
            request.ShowInCheckout,
            request.IsVisit
        );

        addOnRepository.Add(addOn);

        var result = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (result.IsFailure)
        {
            return Result<string>.Failure(result.Error);
        }

        return "Complementado guardado correctamente";
    }
}
