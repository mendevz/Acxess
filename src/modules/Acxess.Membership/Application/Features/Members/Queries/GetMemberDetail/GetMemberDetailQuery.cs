using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMemberDetail;

public record GetMemberDetailQuery(int IdMember) : IRequest<Result<MemberDetailDto>>;