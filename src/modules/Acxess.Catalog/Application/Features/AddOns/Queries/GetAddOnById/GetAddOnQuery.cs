using System;
using Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOns;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOnById;

public record  GetAddOnQuery(int IdAddOn) : IRequest<Result<AddOnDto>>;
