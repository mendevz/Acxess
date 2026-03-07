using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Catalog.Application.Features.AccessTiers.Queries.GetAccessTiers;

public class GetAccessTiersHandler(
    CatalogModuleContext context
) : IRequestHandler<GetAccessTiersQuery, Result<List<AccessTierDto>>>
{
    public async Task<Result<List<AccessTierDto>>> Handle(GetAccessTiersQuery request, CancellationToken cancellationToken)
    {
         var query = context.AccessTiers.AsNoTracking();

        if (!request.IncludesInactives)
        {
            query = query.Where(p => p.IsActive);
        }

        var items = await query.Select(a => new AccessTierDto(
            a.IdAccessTier,
            a.Name,
            a.Description!,
            a.IsActive
        )).ToListAsync(cancellationToken);

        return items;
    }
}
