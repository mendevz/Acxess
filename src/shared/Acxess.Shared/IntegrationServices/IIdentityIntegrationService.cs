
namespace Acxess.Shared.IntegrationServices;

public record AdminContactIntegrationDto(string FullName, string PhoneNumber);
public record TenantAdminContactIntegrationDto(int TenantId, string TenantName, List<AdminContactIntegrationDto> Admins);
public interface IIdentityIntegrationService
{
    Task<List<TenantAdminContactIntegrationDto>> GetTenantAdminsContactsAsync(List<int> tenantIds, CancellationToken cancellationToken = default);
}
