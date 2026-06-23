using Acxess.Catalog.Application.Features.AccessTiers.DTOs;
using Acxess.Catalog.Domain.Errors;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AccessTiers.Queries;

public record GetAccesTierByIdQuery(int Id) : IRequest<Result<AccessTierDto>>;

public class GetAccesTierByIdHandler(
    CatalogModuleContext context
) : IRequestHandler<GetAccesTierByIdQuery, Result<AccessTierDto>>
{
    public async Task<Result<AccessTierDto>> Handle(GetAccesTierByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.AccessTiers.FindAsync([request.Id], cancellationToken);

        if (entity == null) return Result<AccessTierDto>.Failure(AccessTiersErrors.NotFound);

        var dto = new AccessTierDto
        (
            entity.IdAccessTier,
            entity.Name,
            entity.Description ?? string.Empty,
            entity.IsActive
        );

        return Result<AccessTierDto>.Success(dto);
    }
}
