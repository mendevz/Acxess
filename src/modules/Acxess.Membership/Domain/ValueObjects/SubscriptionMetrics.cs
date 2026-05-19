namespace Acxess.Membership.Domain.ValueObjects;

public record SubscriptionMetrics
{
    public int RemainingDays { get; init; }
    public int TotalDays { get; init; }
    public int ProgressPercentage { get; init; }
    public bool IsExpired { get; init; }
    public bool IsInGracePeriod { get; init; }
    public DateTime? GracePeriodEndDate { get; init; }
    public bool HasActiveSubscription { get; init; }
    public string LoyaltyLabel { get; init; } = string.Empty;

    public static SubscriptionMetrics Calculate(
        DateTime currentDate,
        DateTime joinedDate,
        DateTime? chainStartDate,
        DateTime? absoluteEndDate,
        bool isDisplaySubActive,
        bool isSubscriptionCancelled,
        int gracePeriodDaysConfig)
    {
        var isExpired = (absoluteEndDate < currentDate) || isSubscriptionCancelled;
        var hasActiveSub = !isExpired && isDisplaySubActive;
        
        var remainingDays = 0;
        var totalDaysDuration = 1;
        double progress = 0;

        if (hasActiveSub && chainStartDate.HasValue && absoluteEndDate.HasValue)
        {
            var currentStart = chainStartDate.Value.Date;
            var currentEnd = absoluteEndDate.Value.Date;
            
            remainingDays = Math.Max(0, (currentEnd - currentDate).Days);
            totalDaysDuration = Math.Max(1, (currentEnd - currentStart).Days);
            var daysPassed = (currentDate - currentStart).TotalDays;
            
            progress = Math.Clamp((daysPassed / totalDaysDuration) * 100, 0, 100);
        }
        else if (isExpired)
        {
            progress = 100;
        }
        
        var gracePeriodEnd = absoluteEndDate?.AddDays(gracePeriodDaysConfig);
        var inGracePeriod = !hasActiveSub && currentDate <= gracePeriodEnd;

        string loyaltyLabel;
        
        if (hasActiveSub)
        {
            var monthsSinceJoin = ((currentDate.Year - joinedDate.Year) * 12) + currentDate.Month - joinedDate.Month;
            loyaltyLabel = monthsSinceJoin switch
            {
                >= 7 => "Excelente Socio",
                >= 3 => "Recurrente",
                _ => "Nuevo Ingreso"
            };
        }
        else
        {
            loyaltyLabel = "Sin renovación";
        }
        
        return new SubscriptionMetrics
        {
            RemainingDays = remainingDays,
            TotalDays = totalDaysDuration,
            ProgressPercentage = (int)progress,
            IsExpired = isExpired,
            IsInGracePeriod = inGracePeriod,
            GracePeriodEndDate = gracePeriodEnd,
            HasActiveSubscription = hasActiveSub,
            LoyaltyLabel = loyaltyLabel
        };
    }
};