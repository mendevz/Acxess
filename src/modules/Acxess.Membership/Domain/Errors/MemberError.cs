using Acxess.Shared.ResultManager;

namespace Acxess.Membership.Domain.Errors;

public static class MemberError
{
    public static readonly Error NotFound = Error.Conflict(
        "Membership.Member.NotFound", 
        "Member not found.");
}