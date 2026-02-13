using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOns;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOnInscription;

public record GetAddOnInscriptionQuery () : IRequest<Result<AddOnDto>>;