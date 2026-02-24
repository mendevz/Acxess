namespace Acxess.Membership.Application.Features.Members.Queries.GetMembers;

public record MemberItem(
    int IdMember,
    string FullName,
    string Initials,
    bool Active,
    bool IsDeleted,
    string? Email,
    string? Phone);