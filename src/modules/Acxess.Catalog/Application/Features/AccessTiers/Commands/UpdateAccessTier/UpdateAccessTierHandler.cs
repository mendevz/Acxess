using Acxess.Catalog.Domain.Errors;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AccessTiers.Commands.UpdateAccessTier;

public class UpdateAccessTierHandler(
    CatalogModuleContext context
) : IRequestHandler<UpdateAccessTierCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateAccessTierCommand request, CancellationToken cancellationToken)
    {
        var accessTier = await context.AccessTiers.FindAsync([request.Id], cancellationToken);
        
        if (accessTier is null) return Result<string>.Failure(AccessTiersErrors.NotFound);

        if (!request.IsActive) accessTier.Deactivate();
        else accessTier.Activate();
        
        accessTier.Update(request.Name, request.Description);
        
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success("Nivel de Acceso actualizado correctamente.");
    }
}
