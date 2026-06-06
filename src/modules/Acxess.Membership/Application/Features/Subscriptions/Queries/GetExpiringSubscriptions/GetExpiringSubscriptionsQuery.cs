using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Membership.Application.Features.Subscriptions.Queries.GetExpiringSubscriptions;
public record ExpiringMemberDto(int IdMember, string FullName, string Phone);
public record TenantExpiringDataDto(int IdTenant, List<ExpiringMemberDto> ExpiringMembers);

public record GetExpiringSubscriptionsQuery() : IRequest<Result<List<TenantExpiringDataDto>>>;