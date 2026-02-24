using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMembers;

public sealed record GetMembersQuery(
    string? SearchTerm, string StatusFilter = "all") : IRequest<Result<MembersResponse>>;