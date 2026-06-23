using Acxess.Catalog.Domain.Entities;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AccessTiers.Commands;

public record AddAccessTierCommand( string Name, string? Description) : IRequest<Result<string>>, ITenantRequest
{
    public int IdTenant { get; set; }
}

public class AddAccessTierHandler(CatalogModuleContext context ) : IRequestHandler<AddAccessTierCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AddAccessTierCommand request, CancellationToken cancellationToken)
    {
        var accessTier = AccessTier.Create(request.IdTenant, request.Name, request.Description);

        context.AccessTiers.Add(accessTier);

        await context.SaveChangesAsync(cancellationToken);

        return $"Nivel de acceso '{accessTier.Name}' agregado.";
    }
}
