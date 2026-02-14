using Acxess.Marketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Marketing.Infrastructure.Persistence.Configurations;

public class AppliedPromotionConfiguration : IEntityTypeConfiguration<AppliedPromotion>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AppliedPromotion> builder)
    {
        builder.HasKey(x => x.IdAppliedPromotion);

        builder.Property(x => x.IdAppliedPromotion).UseIdentityColumn();
        builder.Property(x => x.IdTenant).IsRequired();
        builder.Property(x => x.IdMemberTransactionDetail).IsRequired();

        builder.Property(x => x.IdPromotion);
        builder.Property(x => x.IdCoupon);

        builder.Property(x => x.AppliedAmount)
        .HasPrecision(10, 2)
        .IsRequired();

        builder.Property(x => x.Notes).HasMaxLength(200);
        
        builder.HasOne(ap => ap.Promotion)
            .WithMany()
            .HasForeignKey(ap => ap.IdPromotion);

        builder.HasOne(ap => ap.Coupon)
            .WithMany()
            .HasForeignKey(ap => ap.IdCoupon);
    }
}