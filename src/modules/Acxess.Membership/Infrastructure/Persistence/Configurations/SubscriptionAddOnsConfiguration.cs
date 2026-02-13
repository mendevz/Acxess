using Acxess.Membership.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acxess.Membership.Infrastructure.Persistence.Configurations;

public class SubscriptionAddOnsConfiguration : IEntityTypeConfiguration<SubscriptionAddOns>
{
    public void Configure(EntityTypeBuilder<SubscriptionAddOns> builder)
    {
        builder.ToTable("SubscriptionAddOns");
        
        builder.Property(t => t.IdTenant)
            .IsRequired();

        builder.HasKey(t => t.IdSubscriptionAddOn);
        
        builder.Property(t => t.IdSubscriptionAddOn)
            .UseIdentityColumn();

        builder.Property(t => t.IdAddOn)
        .IsRequired(); 
        
        builder.Property(t => t.IdSubscription)
        .IsRequired(); 
        
        builder.Property(t => t.PriceSnapshot)
        .HasPrecision(10,2) 
        .IsRequired(); 
    }
}
