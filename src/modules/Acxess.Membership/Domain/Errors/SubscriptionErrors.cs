

using Acxess.Shared.ResultManager;

namespace Acxess.Membership.Domain.Errors;

public static class SubscriptionErrors
{
    public static readonly Error InscriptionRequired = Error.Validation(
        "Membership.Subscription.InscriptionRequired",
        "The inscription is required");

    public static readonly Error ExceededBeneficiaries = Error.Conflict(
        "Membership.Subscription.ExceededBeneficiaries",
        "Exceeds the number of members of the plan");
}
