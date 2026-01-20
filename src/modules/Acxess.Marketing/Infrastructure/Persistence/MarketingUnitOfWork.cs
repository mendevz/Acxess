using Acxess.Infrastructure.Persistence;
using Acxess.Marketing.Domain.Abstractions;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Marketing.Infrastructure.Persistence;

public class MarketingUnitOfWork(
    MarketingModuleContext context) : IMarketingUnitOfWork
{
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            var error = SqlExceptionParser.Parse(ex);

            return Result.Failure(error ?? Error.Failure("Database.Error", "Error inesperado de base de datos, Marketing Module."));
        }
    }
}