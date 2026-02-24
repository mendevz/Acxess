using Acxess.Catalog.Application.Features.AccessTiers.Commands.AddAccessTier;
using Acxess.Catalog.Application.Features.AddOns.Commands.NewAddOn;
using Acxess.Catalog.Domain.Constants;
using Acxess.Shared.Exceptions;
using Acxess.Shared.IntegrationEvents.Identity;
using MediatR;

namespace Acxess.Catalog.Application.IntegrationEvents;

public class TenantCreatedEventHandler(
    IMediator mediator
) : INotificationHandler<TenantCreatedIntegrationEvent>
{
    public async Task Handle(TenantCreatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var command = new NewAddOnCommand(
            notification.TenantId,
            AddOnDefaults.Inscription.Key,
            AddOnDefaults.Inscription.Name,
            AddOnDefaults.Inscription.Price
        );  
        var resultAdd = await mediator.Send(command, cancellationToken);

        if (resultAdd.IsFailure)
        {
            throw new IntegrationEventException(resultAdd.Error);
        }
        
        var commandVisit = new NewAddOnCommand(
            notification.TenantId,
            AddOnDefaults.Visit.Key,
            AddOnDefaults.Visit.Name,
            AddOnDefaults.Visit.Price,
            true,
            true
        );  
        var resultVisit = await mediator.Send(commandVisit, cancellationToken);

        if (resultVisit.IsFailure)
        {
            throw new IntegrationEventException(resultVisit.Error);
        }

        var commandAccessTIer = new AddAccessTierCommand(
            AccessTiersDefaults.GeneralAccessTiers.Name,
            AccessTiersDefaults.GeneralAccessTiers.Description,
            notification.TenantId);
        
        var resultAccessTIer = await mediator.Send(commandAccessTIer, cancellationToken);
        
        if (resultAccessTIer.IsFailure)
        {
            throw new IntegrationEventException(resultAccessTIer.Error);
        }

    }
}
