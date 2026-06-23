namespace Acxess.Membership.Application.Features.Dashboard.DTOs;

public record
    DashboardStatsDto
{
    public int NewMembersToday { get; init; }
    public int NewMembersLastMonth { get; init; } 
    public double GrowthPercentage { get; init; }
    public int TotalMembers { get; init; }
    public int ActiveMembers { get; init; }
    public int ExpiredMembers { get; init; }
    public int ExpiringSoon { get; init; }
    public List<ExpiringMemberItem> TopExpiringMembers { get; init; } = [];
}

public record ExpiringMemberItem(
    int Id, 
    string FullName, 
    string PlanName, 
    DateTime EndDate, 
    int DaysLeft, 
    string Initials, 
    string? PhotoUrl);