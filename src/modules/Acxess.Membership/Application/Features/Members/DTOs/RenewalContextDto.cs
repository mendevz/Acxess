namespace Acxess.Membership.Application.Features.Members.DTOs;

public record RenewalContextDto
{
    public int MemberId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public int? LastSubscriptionId { get; init; }
    public string? LastPlanName { get; init; }
    public List<SuggestedBeneficiaryDto> SuggestedBeneficiaries { get; init; } = [];
};

public record SuggestedBeneficiaryDto(
    int Id, 
    string FirstName, 
    string LastName, 
    string Phone, 
    string Email,
    bool IsEligible, 
    string IneligibilityReason
);