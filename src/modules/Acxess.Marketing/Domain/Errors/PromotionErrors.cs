using System.Runtime.InteropServices.JavaScript;
using Acxess.Shared.ResultManager;

namespace Acxess.Marketing.Domain.Errors;

public static class PromotionErrors
{
    public static readonly Error NotFound = Error.Conflict(
        "Marketing.Promotions.NotFound", 
        "No se encontó la promoción consultada.");
}