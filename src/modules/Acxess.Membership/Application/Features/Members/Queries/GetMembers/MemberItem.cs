namespace Acxess.Membership.Application.Features.Members.Queries.GetMembers;

public record MemberItem(
    int IdMember,
    string Name,
    string LastName,
    string Status,
    string Email,
    string Phone);