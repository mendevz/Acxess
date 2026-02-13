using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMember;

public sealed record GetMemberQuery(string? SearchTerm) : IRequest<Result<List<MemberResponse>>>;
