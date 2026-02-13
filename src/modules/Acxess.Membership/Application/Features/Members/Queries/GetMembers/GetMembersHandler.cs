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
        var query = context.Members.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.searchTerm))
        {
            var term = request.searchTerm.Trim();
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

        var totalCount = await context.Members.CountAsync(cancellationToken);
        var now = DateTime.Now;
        var results = await query
            .OrderByDescending(m => m.IdMember)
            .Select(m => new
            {
                m.IdMember,
                m.FirstName,
                m.LastName,
                m.Email,
                m.Phone,
                m.IsDeleted,
                LatestEndDate = m.SubscriptionMemberships
                    .Select(sm => sm.Subscription.EndDate)
                    .OrderByDescending(ed => ed)
                    .FirstOrDefault()
            })
            .Take(15)
            .Select(m => new MemberItem(
                m.IdMember,
                m.FirstName,
                m.LastName,
                // Lógica traducible a SQL (CASE WHEN)
                m.IsDeleted 
                    ? "Eliminado" 
                    : (m.LatestEndDate > now ? "Activo" : "Vencido"),
                m.Email ?? string.Empty,
                m.Phone ?? string.Empty
            ))
            .ToListAsync(cancellationToken);
 
        return new MembersResponse(totalCount, results.Count, results);
    }
}