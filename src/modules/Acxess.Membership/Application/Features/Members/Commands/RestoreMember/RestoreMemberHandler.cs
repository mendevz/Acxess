using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Features.Members.Commands.RestoreMember;

public class RestoreMemberHandler(
    MembershipModuleContext context,
    ILogger<RestoreMemberHandler> logger) : IRequestHandler<RestoreMemberCommand,  Result<string>>  
{
    public async Task<Result<string>> Handle(RestoreMemberCommand request, CancellationToken ct)
    {
        var member = await context.Members
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.IdMember == request.MemberId, ct);

        if (member is null)
        {
            logger.LogError("Member not found. MemberId: {MemberId}", request.MemberId);
            return Result<string>.Failure(Error.NotFound("Member.NotFound", "Socio no encontrado."));
        }

        member.Restore();
        
        await context.SaveChangesAsync(ct);
        
        logger.LogInformation("Member Restored successful. MemberId: {MemberId}", request.MemberId);

        return "Socio restaurado exitosamente";
    }
}