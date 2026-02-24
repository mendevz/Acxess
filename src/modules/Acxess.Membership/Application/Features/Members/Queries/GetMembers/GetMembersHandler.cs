using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMembers;

public class GetMembersHandler(
    MembershipModuleContext context) : IRequestHandler<GetMembersQuery, Result<MembersResponse>>
{
    public async Task<Result<MembersResponse>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Members.IgnoreQueryFilters().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            if (int.TryParse(term, out var id))
            {
                query = query.Where(m => m.IdMember == id);
            }
            else
            {
                query = query.Where(m =>
                    m.FirstName.Contains(term) ||
                    m.LastName.Contains(term));
            }
        }

       
        var now = DateTime.Now;
        
        // 2. Aplicar Filtro de Estado (Las Reglas de Negocio)
        switch (request.StatusFilter?.ToLower())
        {
            case "active":
                query = query.Where(m => !m.IsDeleted && 
                                         m.SubscriptionMemberships.Any(sm => sm.Subscription.EndDate >= now && sm.Subscription.IsActive));
                break;
            case "expired":
                query = query.Where(m => !m.IsDeleted && 
                                         !m.SubscriptionMemberships.Any(sm => sm.Subscription.EndDate >= now && sm.Subscription.IsActive));
                break;
            case "deleted":
                query = query.Where(m => m.IsDeleted);
                break;
            case "all":
            default:
                query = query.Where(m => !m.IsDeleted); // <-- FALTABA ESTO: Ocultar bajas por defecto
                break;
        }
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var results = await query
            .OrderByDescending(m => m.UpdatedAt)
            .Select(m => new
            {
                m.IdMember,
                m.FirstName,
                m.LastName,
                m.Email,
                m.Phone,
                m.IsDeleted,
                HasActiveSubscription = m.SubscriptionMemberships
                    .Any(sm => sm.Subscription.EndDate >= now && sm.Subscription.IsActive)
            })
            .Take(15)
            .Select(m => new MemberItem(
                m.IdMember,
                $"{m.FirstName} {m.LastName}",
                GetInitials(m.FirstName, m.LastName),
                m.HasActiveSubscription,
                m.IsDeleted,
                m.Email ?? string.Empty,
                m.Phone ?? string.Empty
            ))
            .ToListAsync(cancellationToken);
        return new MembersResponse(totalCount, results.Count, results);
    }
    
    private static string GetInitials(string first, string last)
    {
        return $"{first.FirstOrDefault()}{last.FirstOrDefault()}".ToUpper();
    }
}