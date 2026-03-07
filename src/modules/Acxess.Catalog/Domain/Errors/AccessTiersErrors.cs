using Acxess.Shared.ResultManager;

namespace Acxess.Catalog.Domain.Errors;

public static class AccessTiersErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Catalog.AccessTier.NotFound", 
        "El nivel de acceso no existe."); 
}