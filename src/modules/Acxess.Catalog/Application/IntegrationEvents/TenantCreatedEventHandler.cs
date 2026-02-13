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
            AddOnDefaults.Inscription.Price,
            false
        );  
        var resultAdd = await mediator.Send(command, cancellationToken);

        if (resultAdd.IsFailure)
        {
            throw new IntegrationEventException(resultAdd.Error);
        }
        
    }
}
