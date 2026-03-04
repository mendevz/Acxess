using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Commands.DeleteMember;

public class DeleteMemberHandler(
    MembershipModuleContext context) : IRequestHandler<DeleteMemberCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await context.Members
            .Include(m => m.SubscriptionMemberships)
            .ThenInclude(sm => sm.Subscription)
            .FirstOrDefaultAsync(m => m.IdMember == request.MemberId, cancellationToken);
        
        if (member is null)
            return Result<string>.Failure(Error.NotFound("Member.NotFound", "Socio no encontrado."));
        
        var hasActiveSub = member.HasActiveSubscription();
        
        if (hasActiveSub)
        {
            return Result<string>.Failure(Error.Conflict("Member.HasActiveSubscription", 
                "El socio tiene una suscripción vigente. Cancélela antes de eliminar al socio."));
        }
        
        member.Delete(request.UserId);

        await context.SaveChangesAsync(cancellationToken);

        return "Socio eliminado";

    }
}