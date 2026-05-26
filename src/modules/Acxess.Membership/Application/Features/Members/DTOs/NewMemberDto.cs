using Destructurama.Attributed;
using System.Text.Json.Serialization;

namespace Acxess.Membership.Application.Features.Members.DTOs;

public record NewMemberDto
(
    int IdMember,
    string FirstName,
    string LastName,
    string? Phone,
    [property: JsonIgnore, LogMasked] string? PhotoBase64 = null);