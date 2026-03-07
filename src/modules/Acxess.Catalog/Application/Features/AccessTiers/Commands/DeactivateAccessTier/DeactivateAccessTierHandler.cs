using Acxess.Catalog.Domain.Errors;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AccessTiers.Commands.DeactivateAccessTier;

public class DeactivateAccessTierHandler(
    CatalogModuleContext context
) : IRequestHandler<DeactivateAccessTierCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeactivateAccessTierCommand request, CancellationToken cancellationToken)
    {
        var accessTier = await context.AccessTiers.FindAsync([request.Id], cancellationToken);
        if (accessTier is null) return Result<string>.Failure(AccessTiersErrors.NotFound);

        if (accessTier.IsActive) accessTier.Deactivate();
        else accessTier.Activate();

        context.AccessTiers.Update(accessTier);
        await context.SaveChangesAsync(cancellationToken);
        
        return "Nivel de Acceso desactivado correctamente.";
    }
}