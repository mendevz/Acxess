
using Acxess.Shared.Abstractions;

namespace Acxess.IntegrationTests.Setup;

public class TestCurrentTenant : ICurrentTenant
{
    private readonly AsyncLocal<int?> _currentTenantId = new();
    public int? Id
    {
        get => _currentTenantId.Value ?? 1;
        set => _currentTenantId.Value = value;
    }
    public bool IsAvailable => Id.HasValue;
}
