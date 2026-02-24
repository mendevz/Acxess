using System.ComponentModel.DataAnnotations;

namespace Acxess.Web.Pages.Membership.AddRenewMember;

public class AddRenewMemberInput
{
    public int Id { get; set; }
    public string? FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }  = string.Empty;
    public string? Email { get; set; }  = string.Empty;
    public string? Phone { get; set; }   = string.Empty;
}