namespace Acxess.Membership.Application.Features.Members.Queries.GetMembers;

public record MemberItem(
    int IdMember,
    string FullName,
    string Initials,
    bool Active,
    string? Email,
    string? Phone);