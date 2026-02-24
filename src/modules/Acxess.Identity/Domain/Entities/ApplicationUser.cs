using System;
using System.ComponentModel.DataAnnotations.Schema;
using Acxess.Shared.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Acxess.Identity.Domain.Entities;

public class ApplicationUser : IdentityUser, IMayHaveTenant
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserNumber { get; set; }
    public int? IdTenant { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    
    public virtual Tenant? Tenant { get; set; }

    public static ApplicationUser Create(string userName, string email, string fullName, int? idTenant = null)
    {
        return new ApplicationUser
        {
            UserName = userName,
            Email = email,
            FullName = fullName,
            IsActive = true,
            IdTenant = idTenant
        };
    }
    
    
}
