namespace Acxess.Membership.Application.Features.Members.Queries.GetMember;

public sealed record MemberResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateTime JoinedDate,
    DateTime? LastExpirationDate,
    string? LastPlanName, 
    bool IsSubscriptionActive,
    bool IsProrrogation,
    string? PhotoUrl); 
