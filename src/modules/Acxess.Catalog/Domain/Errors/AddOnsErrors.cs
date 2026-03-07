using Acxess.Shared.ResultManager;

namespace Acxess.Catalog.Domain.Errors;

public static class AddOnsErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Catalog.AddOn.NotFound", 
        "El complemento no existe."); 
    
    public static readonly Error InscriptionNotFound = Error.NotFound(
        "Catalog.AddOn.Inscription.NotFound", 
        "El complemento de inscripción no existe."); 
}