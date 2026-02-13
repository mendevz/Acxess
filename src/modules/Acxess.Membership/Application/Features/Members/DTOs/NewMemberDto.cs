namespace Acxess.Membership.Application.Features.Members.DTOs;

public record NewMemberDto
(
    int IdMember,
    string FirstName,
    string LastName,
    string? Phone);