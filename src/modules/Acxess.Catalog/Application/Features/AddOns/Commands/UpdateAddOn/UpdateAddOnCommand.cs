using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AddOns.Commands.UpdateAddOn;

public record UpdateAddOnCommand(
 int IdAddOn,
 string AddOnKey,
 string Name,
 decimal Price,
 bool ShowInCheckout,
 bool IsVisit, 
 bool IsActive)  : IRequest<Result<string>>;


