using Acxess.Membership.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acxess.Membership.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
         builder.ToTable("Subscriptions");

        builder.HasKey(t => t.IdSubscription);
        
        builder.Property(t => t.IdSubscription)
            .UseIdentityColumn(); 

        builder.Property(t => t.IdTenant)
            .IsRequired();

        builder.Property(t => t.IdMemberOwner)
            .IsRequired();

        builder.Property(t => t.IdSellingPlan)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.StartDate)
            .IsRequired();

        builder.Property(rt => rt.EndDate)
            .IsRequired();

        builder.Property(rt => rt.PriceSnapshot)
            .HasPrecision(10,2)
            .IsRequired();

        builder.Property(rt => rt.Notes)
            .HasMaxLength(250);

        builder.Property(rt => rt.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.CreatedByUser)
            .IsRequired();

        builder.HasIndex(t => t.IdTenant);
        
        builder.HasMany(s => s.SubscriptionMembers)
            .WithOne(sm => sm.Subscription)
            .HasForeignKey(sm => sm.IdSubscription)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(s => s.AddOns)
            .WithOne(sa => sa.Subscription)
            .HasForeignKey(sa => sa.IdSubscription)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(s => s.OwnerMember)
            .WithMany(m => m.OwnedSubscriptions)
            .HasForeignKey(s => s.IdMemberOwner);
    }
}
