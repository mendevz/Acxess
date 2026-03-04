using Acxess.Membership.Application.Features.Members.Queries.GetMember;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMemberById;

public class GetMemberByIdHandler(MembershipModuleContext context) : IRequestHandler<GetMemberByIdQuery, Result<MemberResponse>>
{
    public async Task<Result<MemberResponse>> Handle(GetMemberByIdQuery request, CancellationToken cancellationToken)
    {
        var member = await context
            .Members.Where(m => m.IdMember == request.IdMember)
            .FirstOrDefaultAsync(cancellationToken);
        
        return member is null
            ? Result<MemberResponse>.Failure("Member.NotFound", "Member not found")
            : new MemberResponse(
                member.IdMember,
                member.FirstName,
                member.LastName,
                member.Email ?? string.Empty,
                member.Phone ?? string.Empty,
                member.CreatedAt,
                null,
                null,
                true,
                false,
                member.PhotoUrl);
    }
}