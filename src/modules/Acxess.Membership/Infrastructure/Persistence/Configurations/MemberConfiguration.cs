using System;
using Acxess.Membership.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acxess.Membership.Infrastructure.Persistence.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");

        builder.HasKey(t => t.IdMember);
        
        builder.Property(t => t.IdMember)
            .UseIdentityColumn(); 

        builder.Property(t => t.IdTenant)
            .IsRequired();

        builder.Property(t => t.FirstName)
        .HasMaxLength(80)
        .IsRequired();
      
        builder.Property(t => t.LastName)
        .HasMaxLength(150)
        .IsRequired();
        
        builder.Property(t => t.Email)
        .HasMaxLength(80);
        
        builder.Property(t => t.Phone)
        .HasMaxLength(13);


        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");        
        
        builder.Property(rt => rt.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.CreatedByUser)
            .IsRequired();

        builder.HasIndex(t => t.IdTenant);
        builder.HasIndex(t => t.IdMember);
        
        builder.HasMany(m => m.OwnedSubscriptions)
            .WithOne(s => s.OwnerMember)
            .HasForeignKey(s => s.IdMemberOwner)
            .OnDelete(DeleteBehavior.Restrict); // Evita borrado en cascada peligroso
        
        
        builder.HasMany(m => m.SubscriptionMemberships)
            .WithOne(sm => sm.Member)
            .HasForeignKey(sm => sm.IdMember)
            .OnDelete(DeleteBehavior.Restrict);
        
            
    }
}
