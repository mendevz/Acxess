
namespace Acxess.Catalog.Application.Features.AddOns.DTOs;

public record AddOnDto(
 int IdAddOn,
 string AddOnKey,
 string Name,
 decimal Price,
 bool IsActive,
 bool ShowInCheckout,
 bool IsVisit
);
