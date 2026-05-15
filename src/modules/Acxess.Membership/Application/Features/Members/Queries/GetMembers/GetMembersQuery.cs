using Acxess.Shared.Constants;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMembers;

public sealed record GetMembersQuery(
    string? SearchTerm, 
    int PageNumber,
    int PageSize = PaginationValues.PageSize,
    string StatusFilter = "all") : IRequest<Result<MembersResponse>>;