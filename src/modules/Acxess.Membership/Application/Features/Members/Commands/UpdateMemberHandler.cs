using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Features.Members.Commands;

public record UpdateMemberCommand(
    int Id,
    string FirstName,
    string LastName,
    string? Phone,
    string? Email
) : IRequest<Result<string>>;
public class UpdateMemberHandler(
    MembershipModuleContext context,
    ITimeService timeService,
    ILogger<UpdateMemberHandler> logger): IRequestHandler<UpdateMemberCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        var utcNow = timeService.GetUtcNow();

        var member = await context.Members
            .FirstOrDefaultAsync(m => m.IdMember == request.Id, cancellationToken);

        if (member is null)
        {
            logger.LogError("Member not found. MemberId: {MemberId}", request.Id);
            return Result<string>.Failure(Error.NotFound("Member.NotFound", "Socio no encontrado"));
        }
        
        member.UpdateInformation(
            request.FirstName, 
            request.LastName,
            utcNow,
            request.Phone, 
            request.Email);

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Member updated. MemberId: {MemberId}", request.Id);

        return "Información actualizada";
    }
}