using Acxess.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acxess.Billing.Infrastructure.Persistence.Configurations;

public class MemberTransactionConfiguration : IEntityTypeConfiguration<MemberTransaction>
{
    public void Configure(EntityTypeBuilder<MemberTransaction> builder)
    {
        builder.ToTable("MemberTransactions");

        builder.HasKey(t => t.IdMemberTransaction);
        
        builder.Property(t => t.IdMemberTransaction)
            .UseIdentityColumn(); 

        builder.Property(t => t.IdTenant)
            .IsRequired();

        builder.Property(t => t.IdMember);

        builder.Property(t => t.IdPaymentMethod)
            .IsRequired();
      
        builder.Property(t => t.Total)
        .HasPrecision(18, 2)
        .IsRequired(); 
        
        builder.Property(t => t.Received)
        .HasPrecision(18, 2)
        .IsRequired();
        
        builder.Property(t => t.Difference)
        .HasPrecision(18, 2)
        .IsRequired();
        
        builder.Property(t => t.Notes)
        .HasMaxLength(500);
        
        builder.Property(t => t.Member)
        .HasMaxLength(120);

        builder.Property(rt => rt.TransactionDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(t => t.CreatedByUser)
            .IsRequired();

        builder.HasIndex(t => t.IdTenant);
        builder.HasIndex(t => t.IdMember);
        
        builder.HasMany(t => t.Details)
            .WithOne(d => d.Transaction)
            .HasForeignKey(d => d.IdMemberTransaction)
            .OnDelete(DeleteBehavior.Cascade); 
        
        builder.Navigation(t => t.Details)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
