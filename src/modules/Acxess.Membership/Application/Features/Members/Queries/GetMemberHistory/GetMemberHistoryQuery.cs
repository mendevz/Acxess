using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMemberHistory;

public record GetMemberHistoryQuery(
    int IdMember, 
    bool ShowAll) : IRequest<Result<MemberHistoryDto>>;