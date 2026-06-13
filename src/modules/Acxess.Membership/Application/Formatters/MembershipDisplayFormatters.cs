using Acxess.Membership.Domain.ValueObjects;

namespace Acxess.Membership.Application.Formatters;
public record MemberDisplayInfo(string Label, string Color, string TextColor, string Msg);
public static class MembershipDisplayFormatters
{
    public static MemberDisplayInfo GetStatusDisplay(
        bool isDeleted,
        bool hasPlan,
        bool isSubscriptionCancelled,
        SubscriptionMetrics metrics)
    {
        if (isDeleted)
            return new MemberDisplayInfo("BAJA / ELIMINADO", "gray", "text-red-500", "Este miembro ha sido dado de baja.");
            
        if (!hasPlan)
            return new MemberDisplayInfo("NUEVO / SIN PLAN", "yellow", "text-orange-500", "Selecciona 'Renovar' para asignar el primer plan.");
            
        if (metrics.HasActiveSubscription)
            return new MemberDisplayInfo("ACTIVO", "green", "text-blue-600", "Tu membresía está al corriente.");

        if (isSubscriptionCancelled)
            return new MemberDisplayInfo("CANCELADA", "red", "text-red-500", "La membresía ha sido cancelada por el administrador.");

        if (metrics is { IsExpired: true, IsInGracePeriod: true })
            return new MemberDisplayInfo("VENCIDO", "yellow", "text-orange-500", $"Renueva antes del {metrics.GracePeriodEndDate:dd/MMM} para conservar su antigüedad.");
            
        return new MemberDisplayInfo("VENCIDO", "red", "text-red-500", "La membresía venció. Al renovar, la fecha de corte se reiniciará.");
    }

    public static string GetInitials(string firstName, string lastName)
    {
        var firstInitial = !string.IsNullOrEmpty(firstName) ? firstName[0].ToString().ToUpper() : "";
        var lastInitial = !string.IsNullOrEmpty(lastName) ? lastName[0].ToString().ToUpper() : "";
        return $"{firstInitial}{lastInitial}";
    }

    public static string GetSellingPlanName(string sellingPlanName, bool isCanceled)
    {
        return isCanceled
                    ? $"{sellingPlanName} (Canceled)"
                    : sellingPlanName;
    }

    public static string MemberElegibleLabel(bool isElegible, string? SellingPlanName)
       =>  isElegible ? "Disponible" : $"Tiene plan activo {SellingPlanName ?? ""}";

}