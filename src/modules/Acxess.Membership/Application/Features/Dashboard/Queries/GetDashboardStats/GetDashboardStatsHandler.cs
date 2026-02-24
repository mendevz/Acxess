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
        var threeDaysFromNow = today.AddDays(3);
        
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
        
        // 3. Vencidos (Total - Activos es una aprox, pero mejor consultamos los que vencieron recientemente y no renovaron)
        // Definamos "Vencidos" como aquellos cuya última suscripción terminó antes de hoy.
        // Por simplicidad y rendimiento en dashboard, a veces se muestra "Vencidos este mes".
        // Usaremos: Socios con suscripción que venció y no tienen una nueva futura.
        var expiredMembers = totalMembers - activeMembers; // Aritmética simple para dashboard rápido
        // 1. Creamos una consulta base reutilizable para el conteo y la tabla
        var baseExpiringQuery = context.Members
            .AsNoTracking()
            .Where(m => !m.IsDeleted) // Descartar a los eliminados
            .Where(m => m.SubscriptionMemberships.Any(sm => 
                sm.Subscription.IsActive && // Solo planes activos/vigentes
                sm.Subscription.EndDate >= today && 
                sm.Subscription.EndDate <= threeDaysFromNow))
            // 2. LA MAGIA CONTRA DUPLICADOS/PRONTO PAGO:
            // Excluir a los socios que ya tengan otra suscripción que venza DESPUÉS de estos 3 días
            .Where(m => !m.SubscriptionMemberships.Any(sm => 
                sm.Subscription.IsActive && 
                sm.Subscription.EndDate > threeDaysFromNow));

       // 4. Por Vencer (Conteo real de PERSONAS en riesgo de no renovar)
        var expiringSoon = await baseExpiringQuery.CountAsync(cancellationToken);

            // 5. Tabla Top 5 Por Vencer
        var topExpiring = await baseExpiringQuery
            .Select(m => new 
            {
                m.IdMember,
                m.FirstName,
                m.LastName,
                // Buscamos cuál es exactamente la suscripción que está a punto de vencer
                ExpiringSub = m.SubscriptionMemberships
                    .Where(sm => sm.Subscription.IsActive && 
                                 sm.Subscription.EndDate >= today && 
                                 sm.Subscription.EndDate <= threeDaysFromNow)
                    .Select(sm => sm.Subscription)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefault()
            })
            // Ordenamos por los que vencen primero (los más urgentes arriba)
            .OrderBy(x => x.ExpiringSub!.EndDate)
            .Take(5)
            .Select(x => new ExpiringMemberItem(
                x.IdMember,
                $"{x.FirstName} {x.LastName}",
                x.ExpiringSub!.SellingPlanName, 
                x.ExpiringSub.EndDate,
                (x.ExpiringSub.EndDate.Date - today).Days
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
            // GrowthPercentage se puede calcular si traes datos del mes pasado (tarea opcional para v2)
            GrowthPercentage = 12.5 // Hardcodeado por ahora para no complicar el query inicial
        });
    }
}