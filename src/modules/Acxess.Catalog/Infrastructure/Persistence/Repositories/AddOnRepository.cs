using Acxess.Catalog.Domain.Abstractions;
using Acxess.Catalog.Domain.Entities;

namespace Acxess.Catalog.Infrastructure.Persistence.Repositories;

public class AddOnRepository(
    CatalogModuleContext context
) : IAddOnRepository
{
    public void Add(AddOn addOn)
    {
        context.AddOns.Add(addOn);
    }



    public void Update(AccessTier accessTier)
    {
        context.AccessTiers.Update(accessTier);
    }

    public async Task<AddOn?> GetById(int id, CancellationToken cancellationToken)
    {
        return await context.AddOns.FindAsync([id], cancellationToken);
    }

}
