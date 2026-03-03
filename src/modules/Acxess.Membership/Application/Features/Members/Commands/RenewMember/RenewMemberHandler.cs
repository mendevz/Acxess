using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Membership.Domain.Abstractions;
using Acxess.Membership.Domain.Entities;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.IntegrationEvents.Catalog;
using Acxess.Shared.IntegrationEvents.Membership;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Commands.RenewMember;

public class RenewMemberHandler(
    MembershipModuleContext context,
    ICatalogIntegrationService catalogService,
    IMembershipUnitOfWork unitOfWork,
    IMediator mediator) : IRequestHandler<RenewMemberCommand, Result<UpdatedSubMemberResponse>>
{
    public async Task<Result<UpdatedSubMemberResponse>> Handle(RenewMemberCommand request, CancellationToken cancellationToken)
    {
        var planInfo = await catalogService.GetPlanInfoAsync(request.SellingPlanId, cancellationToken);
        
        if (planInfo == null)
            return Result<UpdatedSubMemberResponse>.Failure("Plan.NotFound", "El plan seleccionado no existe o no está activo.");
        
        var mainMember = await context.Members
            .Include(m => m.SubscriptionMemberships)
            .ThenInclude(sm => sm.Subscription)
            .Include(m => m.OwnedSubscriptions) 
            .FirstOrDefaultAsync(m => m.IdMember == request.IdMember, cancellationToken);
        
        if (mainMember is null)
        {
            return Result<UpdatedSubMemberResponse>.Failure("Member.NotFound", $"No se encontró el miembro con Id {request.IdMember}");
        }
        
        var finalBeneficiaryIds = new List<int>();
        
        if (request.Beneficiaries.Count != 0)
        {
            foreach (var benDto in request.Beneficiaries)
            {
                if (benDto.IdMember == 0) 
                {
                    var newBeneficiary = Member.Create(
                        request.IdTenant,
                        benDto.FirstName,
                        benDto.LastName,
                        request.CreatedUserId,
                        benDto.Phone,
                        null 
                    );

                    context.Members.Add(newBeneficiary);
                    await context.SaveChangesAsync(cancellationToken); 
                    
                    finalBeneficiaryIds.Add(newBeneficiary.IdMember);
                }
                else 
                {
                    finalBeneficiaryIds.Add(benDto.IdMember);
                }
            }
        }
        
        var addOnsWithPrice = new List<(int Id, string Name, decimal Price)>();

        foreach (var addOnId in request.AddOnIds)
        {
            var resultAddOn = await catalogService.GetAddOnPriceAsync(addOnId, cancellationToken);

            if (resultAddOn.IsFailure) 
                return Result<UpdatedSubMemberResponse>.Failure(resultAddOn.Error);
           
            addOnsWithPrice.Add((addOnId, resultAddOn.Value.Name, resultAddOn.Value.Price));
        }
        
        mainMember.Subscribe(
            planInfo.Id,
            planInfo.Name,
            planInfo.Price,
            planInfo.Duration,
            planInfo.DurationUnit,
            request.CreatedUserId,
            finalBeneficiaryIds,
            addOnsWithPrice
        );
        
        
        var resultSave = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (resultSave.IsFailure)
        {
            return Result<UpdatedSubMemberResponse>.Failure(resultSave.Error);
        }
        
        var addOnItems = addOnsWithPrice.Select(a => 
            new PurchasedAddOnItem(a.Id, a.Name, a.Price)
        ).ToList();
        
        var integrationBilling = new SubcriptionPurchasedDomainEvent(
            request.IdTenant,
            request.CreatedUserId,
            mainMember.IdMember,
            mainMember.OwnedSubscriptions.Last().IdSubscription, 
            planInfo.Name,
            planInfo.Price,
            request.PaymentMethodId,
            request.AmountPaid,
            $"{mainMember.FirstName } {mainMember.LastName}",
            false,
            addOnItems
        );
        
        await mediator.Publish(integrationBilling, cancellationToken);

        return new UpdatedSubMemberResponse("Renovación registrada correctamente.", mainMember.IdMember);
    }
}