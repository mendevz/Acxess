namespace Acxess.Membership.Application.Features.Members.DTOs;

public record MembersResponse (
    int TotalMembers,
    int CountMembers,
    IEnumerable<MemberItem> Members,
    int ActiveCount,
    int ExpiredCount,
    int DeletedCount);