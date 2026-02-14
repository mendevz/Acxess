using Acxess.Marketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acxess.Marketing.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.HasKey(x => x.IdPromotion);

        builder.Property(x => x.IdPromotion).UseIdentityColumn();

        builder.Property(x => x.IdTenant).IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DiscountType)
            .IsRequired()
            .HasColumnType("tinyint");

        builder.Property(x => x.Discount)
            .HasPrecision(5, 2);

        builder.Property(x => x.RequiresCoupon)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(x => x.AutoApply)
            .IsRequired()
            .HasDefaultValue(false);


        builder.Property(x => x.AvailableFrom);

        builder.Property(x => x.AvailableTo);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.HasMany(p => p.Coupons)
            .WithOne(c => c.Promotion)
            .HasForeignKey(c => c.IdPromotion)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CreatedByUser).IsRequired();

        
    }
}
