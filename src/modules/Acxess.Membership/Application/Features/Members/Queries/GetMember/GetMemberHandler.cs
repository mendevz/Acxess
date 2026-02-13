using Acxess.Membership.Domain.Entities;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMember;

internal sealed class GetMemberHandler(MembershipModuleContext context)
    : IRequestHandler<GetMemberQuery, Result<List<MemberResponse>>>
{
    public async Task<Result<List<MemberResponse>>> Handle(GetMemberQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return new List<MemberResponse>();
        }
        var term = request.SearchTerm.Trim();
        var query = context.Members.AsNoTracking();
        
        var isNumeric = int.TryParse(term, out var id);

        query = query.Where(m => !m.IsDeleted);
        
        if (isNumeric)
        {
            query = query.Where(m => m.IdMember == id);
        }
        else 
        {
            query = query.Where(m =>
                m.FirstName.Contains(term) ||
                m.LastName.Contains(term));
        }
        
        
        
        var members = await query
            .Select(m => new
            {
                Member = m,
                LatestSubscription = m.SubscriptionMemberships
                    .Select(sm => sm.Subscription)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => new
                    {
                        s.EndDate,
                        s.IdSellingPlan
                    })
                    .FirstOrDefault()
            })
            .Select(x => new MemberResponse(
                x.Member.IdMember,
                x.Member.FirstName,
                x.Member.LastName,
                x.Member.Email ?? string.Empty,
                x.Member.Phone ?? string.Empty,
                x.Member.CreatedAt,
                x.LatestSubscription != null ? (DateTime?)x.LatestSubscription.EndDate : null,
                x.LatestSubscription != null ? $"Plan #{x.LatestSubscription.IdSellingPlan}" : null,
                x.LatestSubscription != null && x.LatestSubscription.EndDate > DateTime.Now
            ))
            .ToListAsync(cancellationToken);

        return members ?? Result<List<MemberResponse>>.Failure("Member.Query.NotFound", "Member not found");
    }
}
