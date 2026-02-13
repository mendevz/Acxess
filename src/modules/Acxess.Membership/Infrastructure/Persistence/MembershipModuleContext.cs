using System.Reflection;
using Acxess.Infrastructure.Extensions;
using Acxess.Membership.Domain.Entities;
using Acxess.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Infrastructure.Persistence;

public class MembershipModuleContext(
    DbContextOptions<MembershipModuleContext> options,
    ICurrentTenant current) : DbContext(options)
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

        modelBuilder.ApplyTenantFilters(current);
    }
}
