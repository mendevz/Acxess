using Acxess.Catalog.Domain.Errors;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AddOns.Commands.UpdateAddOn;

public class UpdateAddOnHandler(
    CatalogModuleContext context
) : IRequestHandler<UpdateAddOnCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateAddOnCommand request, CancellationToken cancellationToken)
    {
        var addOn = await context.AddOns.FindAsync([request.IdAddOn], cancellationToken);

        if (addOn is null) return Result<string>.Failure(AddOnsErrors.NotFound);

        addOn.Update(
            request.AddOnKey,
            request.Name,
            request.Price,
            request.ShowInCheckout,
            request.IsVisit,
            request.IsActive);

        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success("Cambios guardados correctamente.");
        
    }
}