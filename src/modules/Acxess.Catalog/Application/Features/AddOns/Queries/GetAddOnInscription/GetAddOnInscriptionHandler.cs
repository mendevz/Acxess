using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOns;
using Acxess.Catalog.Domain.Constants;
using Acxess.Catalog.Domain.Errors;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOnInscription;

public class GetAddOnInscriptionHandler(
    CatalogModuleContext  context) : IRequestHandler<GetAddOnInscriptionQuery, Result<AddOnDto>>
{
    public async Task<Result<AddOnDto>> Handle(GetAddOnInscriptionQuery request, CancellationToken cancellationToken)
    {
        var query = context.AddOns.AsNoTracking();
        
        query = query.Where(p => p.AddOnKey == AddOnDefaults.Inscription.Key);
        
        var item = await query.Select(a => new AddOnDto(
            a.IdAddOn,
            a.AddOnKey,
            a.Name,
            a.Price,
            a.IsActive,
            a.ShowInCheckout,
            a.IsVisit
        )).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        
        return item ?? Result<AddOnDto>.Failure(AddOnsErrors.InscriptionNotFound);
    }
}