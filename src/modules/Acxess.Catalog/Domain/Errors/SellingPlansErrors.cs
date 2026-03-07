using Acxess.Shared.ResultManager;

namespace Acxess.Catalog.Domain.Errors;

public static class SellingPlansErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Catalog.SellingPlan.NotFound", 
        "El plan de venta no existe."); 
}