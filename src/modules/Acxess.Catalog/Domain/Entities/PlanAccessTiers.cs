using Acxess.Shared.Abstractions;

namespace Acxess.Catalog.Domain.Entities;

public class PlanAccessTiers : IHasTenant
{
    public int IdPlanAccessTier { get; private set; }
    public int IdAccessTier { get; private set; }
    public virtual AccessTier AccessTier { get; private set; } = null!;
    public int IdSellingPlan { get; private set; }
    public virtual SellingPlan SellingPlan { get; private set; } = null!;
    public int IdTenant { get; private set; }
    private PlanAccessTiers()
    {
    }
    private PlanAccessTiers(int idTenant, int accessTierId, int sellingPlanId)
    {
        IdTenant = idTenant;
        IdAccessTier = accessTierId;
        IdSellingPlan = sellingPlanId;
    }
    public static PlanAccessTiers Create(int idTenant, int accessTierId, int sellingPlanId)
    {
        return new PlanAccessTiers(idTenant, accessTierId, sellingPlanId);
    }
}
