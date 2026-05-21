using Acxess.Shared.Enums;
namespace Acxess.Membership.Domain.Services;

public static class SubscriptionDateCalculator
{
    public static DateTime CalculateEndDate(DateTime baseDate, int duration, DurationSubscriptionUnit unit)
    {
        return unit switch
        {
            DurationSubscriptionUnit.Days => baseDate.AddDays(duration),
            DurationSubscriptionUnit.Months => baseDate.AddMonths(duration),
            DurationSubscriptionUnit.Years => baseDate.AddYears(duration),
            _ => baseDate
        };
    }
}
