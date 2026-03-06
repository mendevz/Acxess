using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Dashboard.Queries.GetDashboardStats;

public class GetDashboardStatsHandler(MembershipModuleContext context) : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    public async Task<Result<DashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;
        var startOfToday = today;
        var endOfToday = today.AddDays(1).AddTicks(-1);
        var daysFromNow = today.AddDays(2);
        
        var newToday = await context.Members.CountAsync(m => m.CreatedAt >= startOfToday && m.CreatedAt <= endOfToday, cancellationToken);
        
        // 2. Total y Activos (Aproximación rápida: Activos = Tienen suscripción vigente)
        // Nota: Para "Activos" exacto se requiere query complejo de fechas. 
        // Haremos una aproximación eficiente: Members con IsDeleted=false
        var totalMembers = await context.Members.CountAsync(m => !m.IsDeleted, cancellationToken);
        
        // Para saber activos reales (con membresía vigente), consultamos SubscriptionMembers
        var activeMembers = await context.SubscriptionMembers
            .Where(sm => sm.Subscription.EndDate >= today)
            .Select(sm => sm.IdMember)
            .Distinct()
            .CountAsync(cancellationToken);
        var fifteen = today.AddDays(-15);
        // 3. Vencidos (Total - Activos es una aprox, pero mejor consultamos los que vencieron recientemente y no renovaron)
        // Definamos "Vencidos" como aquellos cuya última suscripción terminó antes de hoy.
        // Por simplicidad y rendimiento en dashboard, a veces se muestra "Vencidos este mes".
        // Usaremos: Socios con suscripción que venció y no tienen una nueva futura.
        var expiredMembers = totalMembers - activeMembers; 
        var baseExpiringQuery = context.Members
            .AsNoTracking()
            .Where(m => !m.IsDeleted)
            .Where(m => m.SubscriptionMemberships.Any(sm => 
                sm.Subscription.EndDate >= fifteen && 
                sm.Subscription.EndDate <= daysFromNow))
            .Where(m => !m.SubscriptionMemberships.Any(sm => 
                sm.Subscription.EndDate > daysFromNow));
        
        var expiringQuery = context.Members
            .AsNoTracking()
            .Where(m => !m.IsDeleted) // Descartar a los eliminados
            .Where(m => m.SubscriptionMemberships.Any(sm => 
                sm.Subscription.IsActive && // Solo planes activos/vigentes
                sm.Subscription.EndDate >= today && 
                sm.Subscription.EndDate <= daysFromNow))
            // 2. LA MAGIA CONTRA DUPLICADOS/PRONTO PAGO:
            // Excluir a los socios que ya tengan otra suscripción que venza DESPUÉS de estos 3 días
            .Where(m => !m.SubscriptionMemberships.Any(sm => 
                sm.Subscription.IsActive && 
                sm.Subscription.EndDate > daysFromNow));

        var expiringSoon = await expiringQuery.CountAsync(cancellationToken);

        var topExpiring = await baseExpiringQuery
            .Select(m => new 
            {
                m.IdMember,
                m.FirstName,
                m.LastName,
                m.PhotoUrl,
                ExpiringSub = m.SubscriptionMemberships
                    .Where(sm =>  sm.Subscription.EndDate >= fifteen && 
                                 sm.Subscription.EndDate <= daysFromNow)
                    .Select(sm => sm.Subscription)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefault()
            })
            .Where(x => x.ExpiringSub != null)
            .OrderBy(x => x.ExpiringSub!.EndDate)
            .Select(x => new ExpiringMemberItem(
                x.IdMember,
                $"{x.FirstName} {x.LastName}",
                x.ExpiringSub!.SellingPlanName, 
                x.ExpiringSub.EndDate,
                (x.ExpiringSub.EndDate.Date - today).Days,
                GetInitials(x.FirstName, x.LastName),
                x.PhotoUrl
            ))
            .ToListAsync(cancellationToken);
        
        return Result<DashboardStatsDto>.Success(new DashboardStatsDto
        {
            NewMembersToday = newToday,
            TotalMembers = totalMembers,
            ActiveMembers = activeMembers,
            ExpiredMembers = expiredMembers,
            ExpiringSoon = expiringSoon,
            TopExpiringMembers = topExpiring,
            GrowthPercentage = 0
        });
    }
    
    private static string GetInitials(string first, string last)
    {
        return $"{first.FirstOrDefault()}{last.FirstOrDefault()}".ToUpper();
    }
}