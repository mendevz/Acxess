using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOns;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOnById;

public class GetAddOnHandler(
    CatalogModuleContext context
) : IRequestHandler<GetAddOnQuery, Result<AddOnDto>>
{
    public async Task<Result<AddOnDto>> Handle(GetAddOnQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.AddOns.FindAsync([request.IdAddOn], cancellationToken);

        if (entity == null)
        {
            return Result<AddOnDto>.Failure("NotFound", "AddOn not found.");
        }

        var dto = new AddOnDto
        (
            entity.IdAddOn,
            entity.AddOnKey,
            entity.Name,
            entity.Price,
            entity.IsActive,
            entity.ShowInCheckout,
            entity.IsVisit
        );

        return dto;
    }
}
