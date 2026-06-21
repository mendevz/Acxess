using Acxess.Identity.Domain.Errors;
using Acxess.Shared.ResultManager;

namespace Acxess.Identity.Domain.Entities;

public class Tenant
{
    public int IdTenant { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Logo { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string TimeZoneId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public virtual ICollection<TenantsUsers> TenantsUsers { get; private set; } = [];

    private Tenant() { }
    private Tenant(string name, string timeZoneId, DateTime createdAtUtc)
    {
        Name = name;
        TimeZoneId = timeZoneId;
        CreatedAt = createdAtUtc;
    }

    public static Tenant Create(string name, string timeZoneId, DateTime createdAtUtc)
            => new(name, timeZoneId, createdAtUtc);
    public void SetLogo(string logo)
        => Logo = logo;

    public Result Desactive()
    {
        if (!IsActive)
            return Result.Failure(TenantErros.AlreadyDesactivated);

        IsActive = false;
        return Result.Success();
    }
}
