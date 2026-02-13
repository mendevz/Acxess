using System;
using Acxess.Membership.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acxess.Membership.Infrastructure.Persistence.Configurations;

public class SubscriptionMembersConfiguration : IEntityTypeConfiguration<SubscriptionMembers>
{
    public void Configure(EntityTypeBuilder<SubscriptionMembers> builder)
    {
        builder.ToTable("SubscriptionMembers");
        
        builder.Property(t => t.IdTenant)
            .IsRequired();

        builder.HasKey(t => t.IdSubscriptionMember);
        
        builder.Property(t => t.IdSubscriptionMember)
            .UseIdentityColumn();

        builder.Property(t => t.IdMember)
        .IsRequired(); 
        
        builder.Property(t => t.IdSubscription)
        .IsRequired(); 

        builder.Property(t => t.Owner)
        .IsRequired(); 

    }
}
