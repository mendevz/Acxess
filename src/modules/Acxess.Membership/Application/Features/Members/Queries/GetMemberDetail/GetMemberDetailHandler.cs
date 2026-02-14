using System.Globalization;
using Acxess.Membership.Domain.Constants;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.IntegrationEvents.Billing;
using Acxess.Shared.IntegrationEvents.Catalog;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMemberDetail;

public class GetMemberDetailHandler(
    MembershipModuleContext context,
    ICatalogIntegrationService catalogService,
    IBillingIntegrationService billingService) : IRequestHandler<GetMemberDetailQuery, Result<MemberDetailDto>>
{
    public async Task<Result<MemberDetailDto>> Handle(GetMemberDetailQuery request, CancellationToken cancellationToken)
    {
        
        
        var today = DateTime.UtcNow.Date;
        
        
        var member = await context.Members
            .AsNoTracking()
            .Where(m => m.IdMember == request.IdMember)
            .Select(m => new
            {
                m.IdMember,
                m.FirstName,
                m.LastName,
                m.Phone,
                m.Email,
                m.CreatedAt,
                m.IsDeleted,
                Subscriptions = m.SubscriptionMemberships
                    .Select(sm => sm.Subscription)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => new 
                    { 
                        s.IdSubscription,
                        s.StartDate, 
                        s.EndDate, 
                        s.IdSellingPlan,
                        AddOns = s.AddOns.Select(ao => new { ao.IdAddOn, ao.PriceSnapshot }).ToList() 
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        if (member is null)
            return Result<MemberDetailDto>.Failure(Error.NotFound("Member.NotFound", "Socio no encontrado"));
        
        var activeOrFutureSubs = member.Subscriptions
            .Where(s => s.EndDate >= today)
            .OrderBy(s => s.StartDate)
            .ToList();
        
        var lastSubscription = member.Subscriptions.FirstOrDefault(); 
        
        var absoluteEndDate = activeOrFutureSubs.Count != 0
            ? activeOrFutureSubs.Max(s => s.EndDate) 
            : lastSubscription?.EndDate;

        var chainStartDate = activeOrFutureSubs.Count != 0
            ? activeOrFutureSubs.Min(s => s.StartDate) 
            : lastSubscription?.StartDate;
        
        var displaySub = activeOrFutureSubs.FirstOrDefault(s => s.StartDate <= today) // La que corre hoy
                         ?? activeOrFutureSubs.FirstOrDefault() // O la futura próxima
                         ?? lastSubscription; // O la vencida
        
        var planName = "Sin Historial";
        List<string> addOnNames = [];
        
        if (displaySub != null)
        {
            var planInfo = await catalogService.GetPlanInfoAsync(displaySub.IdSellingPlan, cancellationToken); 
            
            planName = planInfo is null ? $"Plan #{displaySub.IdSellingPlan}" :  planInfo.Name;

            if (displaySub.AddOns.Count != 0)
            {
                var addOnIds = displaySub.AddOns.Select(x => x.IdAddOn).Distinct().ToList();
                
                var namesResult = await catalogService.GetAddOnNamesAsync(addOnIds, false,  cancellationToken);

                if (namesResult.IsFailure)
                    return Result<MemberDetailDto>.Failure(namesResult.Error);
                
                addOnNames.AddRange(namesResult.Value);
            }
        }
        
        
        var isExpired = absoluteEndDate < today;
        var inGracePeriod = false;
        DateTime? gracePeriodEnd = null;
        var statusLabel = "ACTIVO";
        var statusColor = "green";
        var renewalMsg = "Tu membresía está al corriente.";
        
        if (member.IsDeleted)
        {
            statusLabel = "BAJA / ELIMINADO";
            statusColor = "gray"; // U opacity-50
            renewalMsg = "Este miembro ha sido dado de baja.";
        }
        else if (displaySub == null)
        {
            statusLabel = "NUEVO / SIN PLAN";
            statusColor = "yellow";
            renewalMsg = "Selecciona 'Renovar' para asignar el primer plan.";
        }
        else if (isExpired)
        {
            gracePeriodEnd = absoluteEndDate!.Value.AddDays(Configurations.PRORROGA_DAYS);
            inGracePeriod = today <= gracePeriodEnd;

            if (inGracePeriod)
            {
                statusLabel = "EN PRÓRROGA";
                statusColor = "yellow"; 
                renewalMsg = $"¡Cuidado! Renueva antes del {gracePeriodEnd:dd/MMM} para conservar su antigüedad.";
            }
            else
            {
                statusLabel = "VENCIDO";
                statusColor = "red";
                renewalMsg = "La membresía venció. Al renovar, la fecha de corte se reiniciará.";
            }
        }
        
        
        var remainingDays = 0;
        var totalDays = 1;
        var progress = 0;
        
        if (absoluteEndDate.HasValue && !isExpired)
        {
            remainingDays = (absoluteEndDate.Value - today).Days;
            
            // Calculamos el total de días desde el inicio de esta "cadena" hasta el final acumulado
            // Si chainStartDate es futuro (ej. pagó por adelantado y empieza mañana), usamos hoy para no romper la barra
            var effectiveStart = chainStartDate < today ? chainStartDate.Value : today;
            
            totalDays = (absoluteEndDate.Value - effectiveStart).Days;
            if (totalDays == 0) totalDays = 1; // Evitar div por cero

            var daysPassed = (today - effectiveStart).Days;
            progress = (int)((double)daysPassed / totalDays * 100);
            
            // Capar porcentaje por seguridad visual
            if (progress > 100) progress = 100;
            if (progress < 0) progress = 0;
        }
        else if (isExpired)
        {
            progress = 100; 
            remainingDays = 0;
        }

        
        var monthsSinceJoin = ((today.Year - member.CreatedAt.Year) * 12) + today.Month - member.CreatedAt.Month;
        // var loyaltyLabel = "Nuevo Ingreso";
        // var loyaltyIcon = "🌱"; // Semilla
        //
        // if (monthsSinceJoin >= 24) { loyaltyLabel = "Socio VIP"; loyaltyIcon = "👑"; }
        // else if (monthsSinceJoin >= 6) { loyaltyLabel = "Recurrente"; loyaltyIcon = "⭐"; }
        
        var billingInfo = await billingService.GetMemberStatsAsync(member.IdMember, cancellationToken);
        var cultureMx = new CultureInfo("es-MX");
        
        var datePart = cultureMx.TextInfo.ToTitleCase(member.CreatedAt.ToString("dd MMM yyyy", cultureMx));
        var memberSinceLabel = $"{datePart}";
        return Result<MemberDetailDto>.Success(new MemberDetailDto
        {
            IdMember = member.IdMember,
            FullName = $"{member.FirstName} {member.LastName}",
            Phone = member.Phone ?? "",
            Email = member.Email ?? "",
            Initials = GetInitials(member.FirstName, member.LastName),
            
            IsDeleted = member.IsDeleted,
            StatusLabel = statusLabel,
            StatusColor = statusColor,
            CanRenew = !member.IsDeleted, // Solo bloqueamos renovar si está eliminado
            
            HasActiveMembership = !isExpired,
            PlanName = planName,
            ActiveAddOns = addOnNames,
            
            JoinedDate = member.CreatedAt,
            StartDate = chainStartDate,
            AbsoluteExpirationDate = absoluteEndDate,
            
            RemainingDays = remainingDays,
            TotalDays = totalDays,
            ProgressPercentage = progress,
            
            IsInGracePeriod = inGracePeriod,
            GracePeriodEndDate = gracePeriodEnd,
            RenewalMessage = renewalMsg,
            
            MemberSinceLabel = memberSinceLabel,
            // MemberSinceLabel = member.CreatedAt.ToString("dd MMM yyyy"),
            TotalSpentLabel = billingInfo.TotalSpent.ToString("C", cultureMx),
            PaymentBehaviorLabel = billingInfo.PaymentBehaviorLabel,
            PaymentBehaviorColor = billingInfo.PaymentBehaviorColor
        });
    }
    
    private static string GetInitials(string first, string last)
    {
        return $"{first.FirstOrDefault()}{last.FirstOrDefault()}".ToUpper();
    }
}