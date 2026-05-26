using System.Globalization;
using Acxess.Membership.Application.Formatters;
using Acxess.Membership.Domain.Constants;
using Acxess.Membership.Domain.Errors;
using Acxess.Membership.Domain.ValueObjects;
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
            var planInfoResult = await catalogService.GetPlanInfoAsync(displaySub.IdSellingPlan, cancellationToken);
            if (planInfoResult.IsFailure) return Result<MemberDetailDto>.Failure(planInfoResult.Error);

            var planInfo = planInfoResult.Value;

            planName = planInfo is null ? $"Plan #{displaySub.IdSellingPlan}" :  planInfo.Name;

            if (displaySub.AddOns.Count != 0)
            {
                var addOnIds = displaySub.AddOns.Select(x => x.IdAddOn).Distinct().ToList();
                var names = await catalogService.GetAddOnNamesAsync(addOnIds, cancellationToken);
                addOnNames.AddRange(names);
            }
        }

        var isSubscriptionCancelled = displaySub != null && !displaySub.IsActive && displaySub.CancelledAt.HasValue;
        
        var metrics = SubscriptionMetrics.Calculate(
            currentDate: today,
            joinedDate: member.CreatedAt,
            chainStartDate: chainStartDate,
            absoluteEndDate: absoluteEndDate,
            isDisplaySubActive: displaySub?.IsActive ?? false,
            isSubscriptionCancelled: isSubscriptionCancelled,
            gracePeriodDaysConfig: Configurations.PRORROGA_DAYS 
        );

        var displayInfoToFront = MemberDetailDisplayFormatter.GetStatusDisplay(
            isDeleted: member.IsDeleted, 
            hasPlan: displaySub != null, 
            isSubscriptionCancelled: isSubscriptionCancelled,
            metrics: metrics
        );
        
        var billingInfo = await billingService.GetMemberStatsAsync(member.IdMember, cancellationToken);
        var cultureMx = new CultureInfo("es-MX");
        var datePart = cultureMx.TextInfo.ToTitleCase(member.CreatedAt.ToString("dd MMM yyyy", cultureMx));
        var activeSub = activeSubscriptions.OrderBy(s => s.StartDate).FirstOrDefault();

        logger.LogInformation("Query completed. Detail member obtained successfully. IdMember: {idMember}", member.IdMember);
        
        return Result<MemberDetailDto>.Success(new MemberDetailDto
        {
            IdMember = member.IdMember,
            FullName = $"{member.FirstName} {member.LastName}",
            Phone = member.Phone ?? "",
            Email = member.Email ?? "",
            PhotoUrl = member.PhotoUrl,
            Initials = GetInitials(member.FirstName, member.LastName),
            
            IsDeleted = member.IsDeleted,
            StatusLabel = displayInfoToFront.Label,
            StatusColor = displayInfoToFront.Color,
            CanRenew = !member.IsDeleted, 
            
            HasActiveMembership = !metrics.IsExpired,
            PlanName = planName,
            ActiveAddOns = addOnNames,
            
            JoinedDate = member.CreatedAt,
            StartDate = chainStartDate,
            AbsoluteExpirationDate = absoluteEndDate,
            
            RemainingDays = metrics.RemainingDays,
            TotalDays = metrics.TotalDays,
            ProgressPercentage = metrics.ProgressPercentage,
            
            IsInGracePeriod = metrics.IsInGracePeriod,
            GracePeriodEndDate = metrics.GracePeriodEndDate,
            RenewalMessage = displayInfoToFront.Msg,
            
            MemberSinceLabel = datePart,
            TotalSpentLabel = billingInfo.TotalSpent.ToString("C", cultureMx),
            PaymentBehaviorLabel = billingInfo.PaymentBehaviorLabel,
            PaymentBehaviorColor = billingInfo.PaymentBehaviorColor,
            LoyaltyLabel = metrics.LoyaltyLabel,
            
            CurrentSubscriptionId = activeSub?.IdSubscription,
            HasActiveSubscription = metrics.HasActiveSubscription,
            IsSubscriptionCancelled = isSubscriptionCancelled,
            ColorSubscription = displayInfoToFront.TextColor,
        });
    }
    
    private static string GetInitials(string first, string last)
    {
        return $"{first.FirstOrDefault()}{last.FirstOrDefault()}".ToUpper();
    }
}