namespace Acxess.Membership.Application.Features.Members.DTOs;

public record MemberHistoryDto
{
    public int MemberId { get; set; }
    public List<TimelineItemDto> Items { get; init; } = [];
}

public record TimelineItemDto
{
    public string Title { get; init; } = string.Empty; 
    public DateTime Date { get; init; }
    public decimal? Amount { get; init; } 
    public string Type { get; init; } = "Info"; 
    public string Icon { get; init; } = "activity"; 
    public string ColorClass { get; init; } = "gray";
    public List<string> Details { get; init; } = []; 
}