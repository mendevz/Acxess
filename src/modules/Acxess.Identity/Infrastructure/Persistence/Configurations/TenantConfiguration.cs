using System;
using Acxess.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acxess.Identity.Infrastructure.Persistence.Configurations;

public class TenantConfiguration: IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.IdTenant);

        builder.Property(t => t.IdTenant)
            .ValueGeneratedOnAdd();


        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.TimeZoneId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.Logo)
            .HasMaxLength(600);

        builder.Property(rt => rt.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(rt => rt.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(rt => rt.IdTenant).IsUnique();
    }
}
