using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Identity.Application.Features.Tenants.Queries.GetTenantAdminsContact;

public record TenantAdminContactDto(string FullName, string PhoneNumber);
public record TenantContactDataDto(int TenantId, string TenantName, List<TenantAdminContactDto> Admins);
public record GetTenantAdminsContactQuery(List<int> TenantIds) : IRequest<Result<List<TenantContactDataDto>>>;