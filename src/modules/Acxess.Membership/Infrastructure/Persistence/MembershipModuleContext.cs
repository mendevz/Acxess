using System.Reflection;
using Acxess.Membership.Domain.Entities;
using Acxess.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Infrastructure.Persistence;

public class MembershipModuleContext(
    DbContextOptions<MembershipModuleContext> options,
    ICurrentTenant currentTenant) : DbContext(options)
{
    public DbSet<Member> Members { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionMembers> SubscriptionMembers { get; set; }
    public DbSet<SubscriptionAddOns> SubscriptionAddOns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("Membership");

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplyTenantFilters(modelBuilder);
    }
    
    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // IHasTenant (int IdTenant - Obligatorio)
            if (typeof(IHasTenant).IsAssignableFrom(clrType))
            {
                var method = this.GetType()
                    .GetMethod(nameof(ConfigureHasTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(clrType);

                method?.Invoke(this, [modelBuilder]);
            }
            // IMayHaveTenant (int? IdTenant - Opcional)
            else if (typeof(IMayHaveTenant).IsAssignableFrom(clrType))
            {
                var method = this.GetType()
                    .GetMethod(nameof(ConfigureMayHaveTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(clrType);

                method?.Invoke(this, [modelBuilder]);
            }
        }
    }
    
    private void ConfigureHasTenantFilter<T>(ModelBuilder builder) where T : class, IHasTenant
    {
        builder.Entity<T>().HasQueryFilter(e => e.IdTenant == currentTenant.Id);
    }
    
    private void ConfigureMayHaveTenantFilter<T>(ModelBuilder builder) where T : class, IMayHaveTenant
    {
        builder.Entity<T>().HasQueryFilter(e => e.IdTenant == currentTenant.Id || e.IdTenant == null);
    }
}
