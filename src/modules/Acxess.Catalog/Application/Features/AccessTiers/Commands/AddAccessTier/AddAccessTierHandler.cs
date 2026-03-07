using Acxess.Catalog.Domain.Entities;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AccessTiers.Commands.AddAccessTier;

public class AddAccessTierHandler(CatalogModuleContext context ) : IRequestHandler<AddAccessTierCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AddAccessTierCommand request, CancellationToken cancellationToken)
    {
        var accessTier = AccessTier.Create(request.TenantId, request.Name, request.Description);

        context.AccessTiers.Add(accessTier);

        await context.SaveChangesAsync(cancellationToken);

        return $"Nivel de acceso '{accessTier.Name}' agregado.";
    }
}
