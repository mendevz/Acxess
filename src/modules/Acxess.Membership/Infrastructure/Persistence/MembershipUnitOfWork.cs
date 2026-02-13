using Acxess.Infrastructure.Persistence;
using Acxess.Membership.Domain.Abstractions;
using Acxess.Shared.ResultManager;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Infrastructure.Persistence;

public class MembershipUnitOfWork(MembershipModuleContext context) : IMembershipUnitOfWork
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

            return Result.Failure(error ?? Error.Failure("Database.Error", "Error inesperado de base de datos."));
        }
    }
}