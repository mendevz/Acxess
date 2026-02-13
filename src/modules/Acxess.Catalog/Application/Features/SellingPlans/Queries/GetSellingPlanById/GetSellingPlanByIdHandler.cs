using System;
using Acxess.Catalog.Domain.Enums;
using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Catalog.Application.Features.SellingPlans.Queries.GetSellingPlanById;

public class GetSellingPlanByIdHandler(
    CatalogModuleContext context
) : IRequestHandler<GetSellingPlanByIdQuery, Result<SellingPlanDto>>
{
    public async Task<Result<SellingPlanDto>> Handle(GetSellingPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var query = context.SellingPlans.AsNoTracking()
            .Where(p => p.IdSellingPlan == request.IdSellingPlan);


        var item = await query
            .Select(p => new SellingPlanDto(
                p.IdSellingPlan,
                p.Name,
                p.TotalMembers,
                p.DurationInValue,
                p.DurationUnit,
                p.Price,
                p.IsActive,
                p.AccessTiers.Select(link => link.IdAccessTier).ToList(),
                string.Join(", ", p.AccessTiers.Select(link => link.AccessTier.Name)),
                $"{p.DurationInValue} {GetUnitName(p.DurationUnit, p.DurationInValue)}"
            ))
            .FirstOrDefaultAsync(cancellationToken);

        return item ?? Result<SellingPlanDto>.Failure("NOT FOUND", "No se encontro el plan de venta");
    }
    
    private static string GetUnitName(DurationUnit unit, int value)
    {
        return unit switch
        {
            DurationUnit.Days => value == 1 ? "Día" : "Días",
            DurationUnit.Months => value == 1 ? "Mes" : "Meses",
            DurationUnit.Years => value == 1 ? "Año" : "Años",
            _ => unit.ToString()
        };
    }
}
