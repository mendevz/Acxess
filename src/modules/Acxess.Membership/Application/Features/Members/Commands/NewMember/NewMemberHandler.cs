using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Membership.Domain.Entities;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.IntegrationEvents.Membership;
using Acxess.Shared.IntegrationServices.Catalog;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Membership.Application.Features.Members.Commands.NewMember;

public class NewMemberHandler(
    MembershipModuleContext context,
    ICatalogIntegrationService catalogService,
    IMediator mediator) : IRequestHandler<NewMemberCommand, Result<UpdatedSubMemberResponse>>
{
    public async Task<Result<UpdatedSubMemberResponse>> Handle(NewMemberCommand request, CancellationToken cancellationToken)
    {
        var planInfo = await catalogService.GetPlanInfoAsync(request.SellingPlanId, cancellationToken);
        if (planInfo == null)
            return Result<UpdatedSubMemberResponse>.Failure("Plan.NotFound", "El plan seleccionado no existe o no está activo.");
        
        // get addOns info
        var addOnsResult = await catalogService.GetAddOnPriceBatchAsync(request.AddOnIds, cancellationToken);
        var addOnsWithPrice = addOnsResult.Value;
        
        // create new beneficiares
        var newBeneficiaries = new List<Member>();
        foreach (var benDto in request.Beneficiaries.Where(b => b.IdMember == 0))
        {
            var newBeneficiary = Member.Create(
                request.IdTenant, benDto.FirstName, benDto.LastName, request.CreatedUserId, benDto.Phone, null);
            
            context.Members.Add(newBeneficiary);
            newBeneficiaries.Add(newBeneficiary);
        }
        
        if (newBeneficiaries.Count != 0)
        {
            await context.SaveChangesAsync(cancellationToken); 
        }
        
        // combine beneficiaries
        var finalBeneficiaryIds = new List<int>();
        finalBeneficiaryIds.AddRange(request.Beneficiaries.Where(b => b.IdMember != 0).Select(b => b.IdMember));
        finalBeneficiaryIds.AddRange(newBeneficiaries.Select(b => b.IdMember));
        
        // create member
        var mainMember = Member.Create(
            request.IdTenant,
            request.MemberDto.FirstName,
            request.MemberDto.LastName,
            request.CreatedUserId,
            request.MemberDto.Phone
        );
        
        mainMember.Subscribe(
            planInfo.Id,
            planInfo.Name,
            planInfo.Price,
            planInfo.Duration,
            request.CreatedUserId,
            planInfo.DurationUnit,
            finalBeneficiaryIds, 
            addOnsWithPrice);

        context.Members.Add(mainMember);
        
        await context.SaveChangesAsync(cancellationToken);

        var addOnItems = addOnsWithPrice.Select(a => 
                new PurchasedAddOnItem(a.Id, a.Name, a.Price)
        ).ToList();
        
        var integrationBilling = new SubscriptionPurchasedIntegrationEvent(
            request.IdTenant,
            request.CreatedUserId,
            mainMember.IdMember,
            mainMember.OwnedSubscriptions.Last().IdSubscription,
            planInfo.Name,
            planInfo.Price,
            request.PaymentMethodId,
            request.AmountPaid,
            $"{mainMember.FirstName}  {mainMember.LastName}",
            true,
            addOnItems
        );
        
        await mediator.Publish(integrationBilling, cancellationToken);

        return new UpdatedSubMemberResponse(
            "Subscripción registrada correctamente.", 
            mainMember.IdMember);
    }
}