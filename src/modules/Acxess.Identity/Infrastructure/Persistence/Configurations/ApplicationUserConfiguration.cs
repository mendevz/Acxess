using System;
using Acxess.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acxess.Identity.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        
        builder.Property(u => u.UserNumber)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn() 
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore); 

       builder
            .HasIndex(u => u.UserNumber)
            .IsUnique();
       
       builder.HasOne(u => u.Tenant)
           .WithMany()
           .HasForeignKey(u => u.IdTenant)
           .OnDelete(DeleteBehavior.Restrict);
    }
}