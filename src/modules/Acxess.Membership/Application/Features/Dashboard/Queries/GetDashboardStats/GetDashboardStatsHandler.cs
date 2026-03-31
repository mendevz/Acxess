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
        
        // Ventanas de tiempo
        var daysFromNow = today.AddDays(2); // Para "Por Vencer"
        var fifteenDaysAgo = today.AddDays(-60); // Para "Vencidos recientes"

        // 1. Nuevos Hoy
        var newToday = await context.Members
            .CountAsync(m => m.CreatedAt >= startOfToday && m.CreatedAt <= endOfToday, cancellationToken);

        // 2. Base de Socios No Eliminados
        var activeBaseQuery = context.Members.AsNoTracking().Where(m => !m.IsDeleted);

        var totalMembers = await activeBaseQuery.CountAsync(cancellationToken);

        // 3. Activos: Tienen al menos una suscripción vigente y ACTIVA que vence hoy o en el futuro
        var activeMembers = await activeBaseQuery
            .Where(m => m.SubscriptionMemberships.Any(sm => sm.Subscription.IsActive && sm.Subscription.EndDate >= today))
            .CountAsync(cancellationToken);

        // Vencidos Históricos = Total - Activos
        var expiredMembers = totalMembers - activeMembers;

        // 4. "Por Vencer" (Próximos 2 días)
        // Son activos hoy, su suscripción termina <= daysFromNow, y NO tienen renovaciones pagadas a futuro
        var expiringSoon = await activeBaseQuery
            .Where(m => m.SubscriptionMemberships.Any(sm => sm.Subscription.IsActive && sm.Subscription.EndDate >= today && sm.Subscription.EndDate <= daysFromNow))
            .Where(m => !m.SubscriptionMemberships.Any(sm => sm.Subscription.IsActive && sm.Subscription.EndDate > daysFromNow))
            .CountAsync(cancellationToken);

        // 5. La Lista Combinada (Por Vencer + Vencidos Recientes + Cancelados Recientes)
        // Queremos buscar la ÚLTIMA suscripción de cada socio dentro del rango (Hace 15 días hasta dentro de 2 días)
        // y asegurarnos de que NO tienen una suscripción posterior a ese periodo.
        var topExpiringQuery = await activeBaseQuery
            .Select(m => new 
            {
                Member = m,
                // Tomamos la última suscripción relevante dentro de nuestra ventana de tiempo
                RelevantSub = m.SubscriptionMemberships
                    .Select(sm => sm.Subscription)
                    .Where(s => 
                        (!s.CancelledAt.HasValue && s.EndDate >= fifteenDaysAgo && s.EndDate <= daysFromNow) ||
                        (s.CancelledAt.HasValue && s.CancelledAt >= fifteenDaysAgo && s.CancelledAt <= daysFromNow)
                    )
                    .OrderByDescending(s => s.CancelledAt ?? s.EndDate)
                    .FirstOrDefault(),
                // Verificamos si tiene alguna suscripción en el futuro que nos diga que ya renovó
                HasFutureSub = m.SubscriptionMemberships
                    .Any(sm => sm.Subscription.IsActive && sm.Subscription.EndDate > daysFromNow)
            })
            // Solo aquellos que tienen una suscripción en la ventana Y que NO han renovado a futuro
            .Where(x => x.RelevantSub != null && !x.HasFutureSub)
            .OrderBy(x => x.RelevantSub!.CancelledAt ?? x.RelevantSub!.EndDate)
            .Take(20)
            .ToListAsync(cancellationToken);

        var topExpiring = topExpiringQuery
            .Select(x => 
            {
                // Determinamos si es una cancelación para ajustar la UI
                var isCancelled = x.RelevantSub!.CancelledAt.HasValue;
                var referenceDate = isCancelled ? x.RelevantSub!.CancelledAt!.Value : x.RelevantSub!.EndDate;
                var daysDiff = (referenceDate.Date - today).Days;
                
                // Si fue cancelada, le agregamos la etiqueta al nombre del plan para que sea obvio en la UI
                var planDisplay = isCancelled 
                    ? $"{x.RelevantSub.SellingPlanName} (Cancelado)" 
                    : x.RelevantSub.SellingPlanName;

                return new ExpiringMemberItem(
                    x.Member.IdMember,
                    $"{x.Member.FirstName} {x.Member.LastName}",
                    planDisplay, 
                    referenceDate,
                    daysDiff, 
                    GetInitials(x.Member.FirstName, x.Member.LastName),
                    x.Member.PhotoUrl
                );
            })
            .ToList();

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