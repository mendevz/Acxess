using System;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMemberDetail;

public record MemberDetailDto
{
    public int IdMember { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string Initials { get; init; } = string.Empty;
    
    public bool IsDeleted { get; init; }
    public string StatusLabel { get; init; } = "Inactivo"; // Texto: ACTIVO, VENCIDO, BAJA, PRÓRROGA
    public string StatusColor { get; init; } = "gray"; // gray, green, red, yellow
    public bool CanRenew { get; init; }
    
    public bool HasActiveMembership { get; init; }
    public string PlanName { get; init; } = "Sin Plan";
    public string PlanDescription { get; init; } = string.Empty;

    
    // --- Fechas y Tiempos ---
    public DateTime JoinedDate { get; init; }
    public DateTime? StartDate { get; init; } 
    public DateTime? AbsoluteExpirationDate { get; init; } 
    public int RemainingDays { get; init; }
    public int TotalDays { get; init; }
    public int ProgressPercentage { get; init; }
    
    public bool IsInGracePeriod { get; init; }
    public DateTime? GracePeriodEndDate { get; init; }
    public string RenewalMessage { get; init; } = string.Empty;
    
    public List<string> ActiveAddOns { get; init; } = [];
    public string? InternalNotes { get; init; }
    
    

    public string MemberSinceLabel { get; init; } = string.Empty;
    public string TotalSpentLabel { get; init; } = string.Empty;
    public string PaymentBehaviorColor { get; set; } = string.Empty;
    public string PaymentBehaviorLabel  { get; set; }= string.Empty;
    



};