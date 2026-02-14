namespace Acxess.Membership.Application.Features.Members.Queries.GetMemberHistory;

public record MemberHistoryDto
{
    public int MemberId { get; set; }
    public List<TimelineItemDto> Items { get; init; } = [];
}

public record TimelineItemDto
{
    public string Title { get; init; } = string.Empty; // Ej: "Renovación Mensual" o "Membresía Vencida"
    public DateTime Date { get; init; }
    public decimal? Amount { get; init; } // Null si es evento de sistema (vencimiento)
    public string Type { get; init; } = "Info"; // "Payment", "Expiration", "System"
    public string Icon { get; init; } = "activity"; // Para elegir el SVG en el front
    public string ColorClass { get; init; } = "gray"; // blue, red, green
    public List<string> Details { get; init; } = []; // Ej: ["Plan Gym $500", "Locker $50"]
}