using System;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Catalog.Application.Features.AddOns.Queries.GetAddOns;

public record GetAddOnsQuery(bool IncludesInactives) : IRequest<Result<List<AddOnDto>>>;

public record AddOnDto(
    int IdAddOn,
    string AddOnKey,
    string Name,
    decimal Price,
    bool IsActive,
    bool ShowInCheckout,
    bool IsVisit
);