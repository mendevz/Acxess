using System.Globalization;
using Acxess.Membership.Domain.Constants;
using Acxess.Membership.Domain.Errors;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.IntegrationServices.Billing;
using Acxess.Shared.IntegrationServices.Catalog;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMemberDetail;

public class GetMemberDetailHandler(
    MembershipModuleContext context,
    ICatalogIntegrationService catalogService,
    IBillingIntegrationService billingService,
    ILogger<GetMemberDetailHandler> logger) : IRequestHandler<GetMemberDetailQuery, Result<MemberDetailDto>>
{
    public async Task<Result<MemberDetailDto>> Handle(GetMemberDetailQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;
        
        var member = await context.Members
            .AsNoTracking()
            .AsSplitQuery()
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
                m.PhotoUrl,
                Subscriptions = m.SubscriptionMemberships
                    .Select(sm => sm.Subscription)
                    .Select(s => new 
                    { 
                        s.IdSubscription,
                        s.StartDate, 
                        s.EndDate, 
                        s.IdSellingPlan,
                        s.IsActive,
                        s.CancelledAt,
                        AddOns = s.AddOns.Select(ao => new { ao.IdAddOn, ao.PriceSnapshot }).ToList() 
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        if (member is null)
            return Result<MemberDetailDto>.Failure(MemberError.NotFound);
        
        var activeSubscriptions = member.Subscriptions
            .Where(s => s.EndDate >= today && s.IsActive)
            .ToList();
        
        var lastSubscription = member.Subscriptions
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefault(); 
        
        var chainStartDate = activeSubscriptions.Count != 0
            ? activeSubscriptions.Min(s => s.StartDate) 
            : lastSubscription?.StartDate;

        var absoluteEndDate = activeSubscriptions.Count != 0
            ? activeSubscriptions.Max(s => s.EndDate) 
            : lastSubscription?.EndDate;
        
        var displaySub = 
            activeSubscriptions.FirstOrDefault(s => s.StartDate <= today) // La que corre hoy
                         ?? activeSubscriptions.FirstOrDefault() // O la futura próxima
                         ?? lastSubscription; // O la vencida
        
        var planName = string.Empty;
        List<string> addOnNames = [];
        
        if (displaySub != null)
        {
            var planInfo = await catalogService.GetPlanInfoAsync(displaySub.IdSellingPlan, cancellationToken); 
            planName = planInfo is null ? $"Plan #{displaySub.IdSellingPlan}" :  planInfo.Name;

            if (displaySub.AddOns.Count != 0)
            {
                var addOnIds = displaySub.AddOns.Select(x => x.IdAddOn).Distinct().ToList();
                var names = await catalogService.GetAddOnNamesAsync(addOnIds, cancellationToken);
                addOnNames.AddRange(names);
            }
        }
        
        double progress = 0;
        var remainingDays = 0;
        var totalDaysDuration = 1; 
        
        var isSubscriptionCancelled = displaySub != null 
                                      && !displaySub.IsActive 
                                      && displaySub.CancelledAt.HasValue;
        
        var isExpired = absoluteEndDate < today || isSubscriptionCancelled;

        var hasSubscriptionActive = displaySub != null && !isExpired && displaySub.IsActive;
        
        if (hasSubscriptionActive)
        {
            var currentStart = chainStartDate!.Value.Date;
            var currentEnd = absoluteEndDate!.Value.Date;
            
            remainingDays = (currentEnd - today).Days;
            if (remainingDays < 0) remainingDays = 0;

            totalDaysDuration = (currentEnd - currentStart).Days;
            if (totalDaysDuration <= 0) totalDaysDuration = 1;

            var daysPassed = (today - currentStart).TotalDays;
    
            progress = (daysPassed / totalDaysDuration) * 100;

            if (progress < 0) progress = 0;     // Subscription starts in the future
            if (progress > 100) progress = 100; //  Subscription expired
        }
        else if (isExpired)
        {
            progress = 100;
            remainingDays = 0;
        }
       
        var inGracePeriod = false;
        DateTime? gracePeriodEnd = null;
        
        var statusLabel = "ACTIVO";
        var statusColor = "green";
        var colorSubscription = "text-blue-600";
        var renewalMsg = "Tu membresía está al corriente.";
        
        if (member.IsDeleted)
        {
            statusLabel = "BAJA / ELIMINADO";
            statusColor = "gray"; // U opacity-50
            colorSubscription = "text-red-500";
            renewalMsg = "Este miembro ha sido dado de baja.";
        }
        else if (displaySub == null)
        {
            statusLabel = "NUEVO / SIN PLAN";
            statusColor = "yellow";
            colorSubscription = "text-orange-500";
            renewalMsg = "Selecciona 'Renovar' para asignar el primer plan.";
        }
        else if (!hasSubscriptionActive)
        {
            gracePeriodEnd = absoluteEndDate!.Value.AddDays(Configurations.PRORROGA_DAYS);
            inGracePeriod = today <= gracePeriodEnd;

            if (isSubscriptionCancelled)
            {
                statusLabel = "CANCELADA";
                statusColor = "red";
                renewalMsg = "La membresía ha sido cancelada por el administrador.";
                colorSubscription = "text-red-500";
            }
            else if (inGracePeriod)
            {
                statusLabel = "VENCIDO";
                statusColor = "yellow"; 
                colorSubscription = "text-orange-500";
                renewalMsg = $"Renueva antes del {gracePeriodEnd:dd/MMM} para conservar su antigüedad.";
            }
            else
            {
                statusLabel = "VENCIDO";
                statusColor = "red";
                renewalMsg = "La membresía venció. Al renovar, la fecha de corte se reiniciará.";
                colorSubscription = "text-red-500";
            }
        }
        
        var monthsSinceJoin = ((today.Year - member.CreatedAt.Year) * 12) + today.Month - member.CreatedAt.Month;

        var loyaltyLabel = monthsSinceJoin switch
        {
            >= 7 => "Socio Fiel",
            >= 3 => "Recurrente",
            _ => "Nuevo Ingreso"
        };

        var billingInfo = await billingService.GetMemberStatsAsync(member.IdMember, cancellationToken);
        var cultureMx = new CultureInfo("es-MX");
        
        var datePart = cultureMx.TextInfo.ToTitleCase(member.CreatedAt.ToString("dd MMM yyyy", cultureMx));
        var memberSinceLabel = $"{datePart}";
        
        var activeSub = activeSubscriptions
            .OrderBy(s => s.StartDate)
            .SingleOrDefault();

        var nameMember = $"{member.FirstName} {member.LastName}";
        logger.LogInformation("Query completed. Detail member obtained successfully. IdMember: {idMember} Name: {nameMember}", 
            member.IdMember, 
            nameMember);
        
        return Result<MemberDetailDto>.Success(new MemberDetailDto
        {
            IdMember = member.IdMember,
            FullName = $"{member.FirstName} {member.LastName}",
            Phone = member.Phone ?? "",
            Email = member.Email ?? "",
            PhotoUrl = member.PhotoUrl,
            Initials = GetInitials(member.FirstName, member.LastName),
            
            IsDeleted = member.IsDeleted,
            StatusLabel = statusLabel,
            StatusColor = statusColor,
            CanRenew = !member.IsDeleted, 
            
            HasActiveMembership = !isExpired,
            PlanName = planName,
            ActiveAddOns = addOnNames,
            
            JoinedDate = member.CreatedAt,
            StartDate = chainStartDate,
            AbsoluteExpirationDate = absoluteEndDate,
            
            RemainingDays = remainingDays,
            TotalDays = totalDaysDuration,
            ProgressPercentage =(int)progress,
            
            IsInGracePeriod = inGracePeriod,
            GracePeriodEndDate = gracePeriodEnd,
            RenewalMessage = renewalMsg,
            
            MemberSinceLabel = memberSinceLabel,
            TotalSpentLabel = billingInfo.TotalSpent.ToString("C", cultureMx),
            PaymentBehaviorLabel = billingInfo.PaymentBehaviorLabel,
            PaymentBehaviorColor = billingInfo.PaymentBehaviorColor,
            LoyaltyLabel = loyaltyLabel,
            
            CurrentSubscriptionId = activeSub?.IdSubscription,
            HasActiveSubscription = hasSubscriptionActive,
            IsSubscriptionCancelled = isSubscriptionCancelled,
            ColorSubscription = colorSubscription
        });
    }
    
    private static string GetInitials(string first, string last)
    {
        return $"{first.FirstOrDefault()}{last.FirstOrDefault()}".ToUpper();
    }
}