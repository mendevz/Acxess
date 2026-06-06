namespace Acxess.Identity.Domain.Entities;

public class TenantsUsers
{
    public int IdTenant { get; private set; }
    public int UserNumber { get; private set; }
    public bool Default { get; private set; } = false;

    public virtual Tenant Tenant { get; private set; } = null!;
    public virtual ApplicationUser User { get; private set; } = null!;

    private TenantsUsers() { }

    private TenantsUsers(int idTenant, int userNumber)
    {
        IdTenant = idTenant;
        UserNumber = userNumber;
        Default = true;
    }

    public static TenantsUsers Create(int idTenant, int userNumber)
    {
        return new TenantsUsers(idTenant, userNumber);
    }
}