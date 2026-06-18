using Acxess.Membership.Infrastructure.Extensions;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Constants;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Features.Members.Queries.GetMembers;

public class GetMembersHandler(
    MembershipModuleContext context,
    ILogger<GetMembersHandler> logger) : IRequestHandler<GetMembersQuery, Result<MembersResponse>>
{
    public async Task<Result<MembersResponse>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var searchTerm = request.SearchTerm?.Trim() ?? string.Empty;
        var now = DateTime.Now;
        var pageSize = request.PageSize > 0 ? request.PageSize : PaginationValues.PageSize;
        var baseGetMembersQuery = context.Members.AsNoTracking();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            if (int.TryParse(searchTerm, out var id))
                baseGetMembersQuery = baseGetMembersQuery.Where(m => m.IdMember == id);
            else
            {
                var searchTerms = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                baseGetMembersQuery = searchTerms.Aggregate(baseGetMembersQuery, (current, term) => current.Where(m => m.FirstName.Contains(term) || m.LastName.Contains(term)));
            }
        }
        
        var statsMembers = await  baseGetMembersQuery
            .GroupBy(_ => 1)
            .Select(m => new
            {
                TotalMembers = m.Count(),
                DeletedMembers = m.Count(member => member.IsDeleted),
                ActiveMembers = m.Count(member => 
                    !member.IsDeleted 
                    && member.SubscriptionMemberships.Any(sm =>
                        sm.Subscription.EndDate >= now
                        && !sm.Subscription.CancelledAt.HasValue
                    )
                )
            }).SingleOrDefaultAsync(cancellationToken) ?? new { TotalMembers = 0, DeletedMembers = 0, ActiveMembers = 0 };

        var expiredMembers = (statsMembers.TotalMembers - statsMembers.DeletedMembers) - statsMembers.ActiveMembers;

        var query = request.StatusFilter?.ToLower() switch
        {
            "active" => baseGetMembersQuery
                .Where(m => !m.IsDeleted)
                .WhereHasSubscriptionActive(now),

            "expired" => baseGetMembersQuery
                .Where(m => !m.IsDeleted)
                .WhereHasNotSubscriptionActive(now),

            "deleted" => baseGetMembersQuery.Where(m => m.IsDeleted),

            _ => baseGetMembersQuery.Where(m => !m.IsDeleted)
        };
        

        var skip = (request.PageNumber - 1) * pageSize;

        var results = await query
            .OrderByDescending(m => m.UpdatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(m => new
            {
                m.IdMember,
                m.FirstName,
                m.LastName,
                m.Email,
                m.Phone,
                m.IsDeleted,
                m.PhotoUrl,
                HasActiveSubscription = m.SubscriptionMemberships
                    .Any(
                        sm => sm.Subscription.EndDate >= now
                        && !sm.Subscription.CancelledAt.HasValue
                    )
            })
            .ToListAsync(cancellationToken);

        var memberItems = results.Select(m => new MemberItem(
            m.IdMember,
            $"{m.FirstName} {m.LastName}",
            GetInitials(m.FirstName, m.LastName),
            m.HasActiveSubscription,
            m.IsDeleted,
            m.Email ?? string.Empty,
            m.Phone ?? string.Empty,
            m.PhotoUrl
        )).ToList();
        
        logger.LogInformation(
            "Query completed. Returns {Count}  out of a total {Total}", 
            memberItems.Count, statsMembers.TotalMembers);

        return new MembersResponse(
            statsMembers.TotalMembers, 
            memberItems.Count, 
            memberItems,
            statsMembers.ActiveMembers,
            expiredMembers,
            statsMembers.DeletedMembers
        );
    }
    
    private static string GetInitials(string first, string last)
    {
        return $"{first.FirstOrDefault()}{last.FirstOrDefault()}".ToUpper();
    }
}