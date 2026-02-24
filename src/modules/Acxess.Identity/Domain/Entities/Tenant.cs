using Acxess.Identity.Domain.Errors;
using Acxess.Shared.ResultManager;

namespace Acxess.Identity.Domain.Entities;

public class Tenant
{
    public int IdTenant { get; private set; }
    public string Name { get; private set; }
    public string? Logo { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.Now;

    private Tenant(string name)
    {
        Name = name;
    }

    public static Tenant Create(string name)
        => new(name);
    
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
