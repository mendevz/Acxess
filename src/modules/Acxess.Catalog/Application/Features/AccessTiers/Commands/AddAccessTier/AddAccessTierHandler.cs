using Acxess.Catalog.Domain.Abstractions;
using Acxess.Catalog.Domain.Entities;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AccessTiers.Commands.AddAccessTier;

public class AddAccessTierHandler(
    IAccessTierRepository accessTierRepository,
    ICatalogUnitOfWork catalogUnitOfWork,
    ICurrentTenant currentTenant
) : IRequestHandler<AddAccessTierCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AddAccessTierCommand request, CancellationToken cancellationToken)
    {
        // if (!currentTenant.IsAvailable)
        //     return Result<string>.Failure("TenantId.NotAvailable","Tenant information is not available.");

        var accessTier = AccessTier.Create(currentTenant.Id ?? request.TenantId ?? 0, request.Name, request.Description);

        accessTierRepository.Add(accessTier);

        var result = await catalogUnitOfWork.SaveChangesAsync(cancellationToken);

        if (result.IsFailure)
        {
            return Result<string>.Failure(result.Error);
        }

        return Result<string>.Success($"Nivel de access '{accessTier.Name}' agregado.");
    }
}
