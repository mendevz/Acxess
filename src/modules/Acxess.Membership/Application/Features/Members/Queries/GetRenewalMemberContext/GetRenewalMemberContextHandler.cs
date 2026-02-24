using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Queries.GetRenewalMemberContext;

public class GetRenewalMemberContextHandler(
    MembershipModuleContext context): IRequestHandler<GetRenewalMemberContextQuery, Result<RenewalContextDto>>
{
    public async Task<Result<RenewalContextDto>> Handle(GetRenewalMemberContextQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;

        var requestingMember = await context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdMember == request.MemberId, cancellationToken);
            
        if (requestingMember is null) 
            return Result<RenewalContextDto>.Failure(Error.NotFound("Member.NotFound", "Socio no encontrado"));
            
        // 1. OBTENEMOS TODAS LAS SUSCRIPCIONES DEL SOCIO PRINCIPAL (El que está renovando)
        var sharedSubscriptionIds = await context.SubscriptionMembers
            .AsNoTracking()
            .Where(sm => sm.IdMember == request.MemberId)
            .Select(sm => sm.IdSubscription)
            .ToListAsync(cancellationToken);

        var lastGroupSub = await context.SubscriptionMembers
            .AsNoTracking()
            .Where(sm => sm.IdMember == request.MemberId)
            .OrderByDescending(sm => sm.Subscription.EndDate)
            .Select(sm => new { sm.IdSubscription, sm.Subscription.IdSellingPlan }) 
            .FirstOrDefaultAsync(cancellationToken);
            
        List<SuggestedBeneficiaryDto> suggestions = [];
            
        if (lastGroupSub != null)
        {
            var previousMembersRaw = await context.SubscriptionMembers
                .AsNoTracking()
                .Where(sm => sm.IdSubscription == lastGroupSub.IdSubscription && sm.IdMember != request.MemberId)
                .Select(sm => new 
                {
                    sm.IdMember,
                    sm.Member.FirstName,
                    sm.Member.LastName,
                    Phone = sm.Member.Phone ?? "",
                    Email = sm.Member.Email ?? "",
                    
                    // 2. LA MAGIA: Buscamos si tiene una suscripción activa que NO esté en la lista compartida
                    ConflictingSub = sm.Member.SubscriptionMemberships
                        .Where(subLink => 
                            subLink.Subscription.IsActive &&
                            subLink.Subscription.EndDate >= today && 
                            !sharedSubscriptionIds.Contains(subLink.IdSubscription) // <- REGLA CORREGIDA
                        )
                        .Select(subLink => subLink.Subscription.EndDate)
                        .OrderByDescending(d => d)
                        .FirstOrDefault() 
                })
                .ToListAsync(cancellationToken);

            suggestions.AddRange(from item in previousMembersRaw
                let hasConflict = item.ConflictingSub != default
                let isEligible = !hasConflict
                let reason = isEligible
                    ? "Disponible"
                    : $"Tiene otro plan activo "
                select new SuggestedBeneficiaryDto(item.IdMember, item.FirstName, item.LastName, item.Phone, item.Email, isEligible, reason));
        }
        
        return Result<RenewalContextDto>.Success(new RenewalContextDto
        {
            MemberId = requestingMember.IdMember,
            FullName = $"{requestingMember.FirstName} {requestingMember.LastName}",
            LastSubscriptionId = lastGroupSub?.IdSubscription,
            LastPlanName = lastGroupSub != null ? $"Plan anterior ({lastGroupSub.IdSellingPlan})" : null,
            SuggestedBeneficiaries = suggestions
        });
                
    }
}