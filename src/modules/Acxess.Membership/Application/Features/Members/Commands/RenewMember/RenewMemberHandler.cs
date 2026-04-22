using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Membership.Domain.Entities;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.IntegrationEvents.Membership;
using Acxess.Shared.IntegrationServices.Catalog;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Commands.RenewMember;

public class RenewMemberHandler(
    MembershipModuleContext context,
    ICatalogIntegrationService catalogService,
    IMediator mediator,
    IImageStorageService imageStorage) : IRequestHandler<RenewMemberCommand, Result<UpdatedSubMemberResponse>>
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
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.IdMember == request.IdMember, cancellationToken);
        
        if (mainMember is null)
        {
            return Result<UpdatedSubMemberResponse>.Failure("Member.NotFound", $"No se encontró el miembro con Id {request.IdMember}");
        }
        
        if (!string.IsNullOrWhiteSpace(request.PhotoBase64))
        {
            if (!string.IsNullOrEmpty(mainMember.PhotoUrl))
                await imageStorage.DeleteImageAsync(mainMember.PhotoUrl, cancellationToken);

            var cleanName = $"{mainMember.FirstName}-{mainMember.LastName}".ToLower().Replace(" ", "-");
            var newPhotoResult = await imageStorage.SaveImageAsync(request.PhotoBase64, cleanName, cancellationToken);
            mainMember.UpdatePhoto(newPhotoResult.Value);
        }
        
        var addOnsResult = await catalogService.GetAddOnPriceBatchAsync(request.AddOnIds, cancellationToken);
        var addOnsWithPrice = addOnsResult.Value;
        
        // create new beneficiares
        var newBeneficiaries = new List<Member>();
        foreach (var benDto in request.Beneficiaries.Where(b => b.IdMember == 0))
        {
            
            string? benPhotoUrl = null;
            if (!string.IsNullOrWhiteSpace(benDto.PhotoBase64))
            {
                var cleanName = $"{benDto.FirstName}-{benDto.LastName}".ToLower().Replace(" ", "-");
                var resultSaved = await imageStorage.SaveImageAsync(benDto.PhotoBase64, cleanName, cancellationToken);
                benPhotoUrl = resultSaved.Value;
            }
            
            var newBeneficiary = Member.Create(
                request.IdTenant, benDto.FirstName, benDto.LastName, request.CreatedUserId, benDto.Phone, null, benPhotoUrl);
            
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
        
        mainMember.Subscribe(
            planInfo.Id,
            planInfo.Name,
            planInfo.Price,
            planInfo.Duration,
            request.CreatedUserId,
            planInfo.DurationUnit,
            finalBeneficiaryIds, addOnsWithPrice);
        
        
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
            $"{mainMember.FirstName } {mainMember.LastName}",
            false,
            addOnItems
        );
        
        await mediator.Publish(integrationBilling, cancellationToken);

        return new UpdatedSubMemberResponse("Renovación registrada correctamente.", mainMember.IdMember);
    }
}