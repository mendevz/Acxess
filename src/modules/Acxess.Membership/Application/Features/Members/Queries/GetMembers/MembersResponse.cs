namespace Acxess.Membership.Application.Features.Members.Queries.GetMembers;

public record MembersResponse (
    int TotalMembers,
    int CountMembers,
    IEnumerable<MemberItem> Members);