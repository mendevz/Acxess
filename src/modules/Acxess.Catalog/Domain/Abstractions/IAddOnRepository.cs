using Acxess.Catalog.Domain.Entities;

namespace Acxess.Catalog.Domain.Abstractions;

public interface IAddOnRepository
{
    void Add(AddOn addOn);

    Task<AddOn?> GetById(int id, CancellationToken cancellationToken);
}
