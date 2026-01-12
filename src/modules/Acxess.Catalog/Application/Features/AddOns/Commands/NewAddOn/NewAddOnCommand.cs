using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AddOns.Commands.NewAddOn;

public record NewAddOnCommand
(int TenantId,
 string AddOnKey,
 string Name,
 decimal Price,
 bool ShowInCheckout = false) : IRequest<Result<string>>;
