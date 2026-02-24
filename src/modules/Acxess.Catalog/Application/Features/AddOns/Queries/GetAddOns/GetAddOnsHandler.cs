using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOns;

public class GetAddOnsHandler(
    CatalogModuleContext context
) : IRequestHandler<GetAddOnsQuery, Result<List<AddOnDto>>>
{
    public async Task<Result<List<AddOnDto>>> Handle(GetAddOnsQuery request, CancellationToken cancellationToken)
    {
        var query = context.AddOns.AsNoTracking();

        if (!request.IncludesInactives)
        {
            query = query.Where(p => p.ShowInCheckout);
        }

        var items = await query.Select(a => new AddOnDto(
            a.IdAddOn,
            a.AddOnKey,
            a.Name,
            a.Price,
            a.IsActive,
            a.ShowInCheckout,
            a.IsVisit
        )).ToListAsync(cancellationToken);

        return items;
    }
}
