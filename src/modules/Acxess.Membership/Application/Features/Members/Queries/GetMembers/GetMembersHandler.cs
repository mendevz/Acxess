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
        var searchTerm = request.SearchTerm?.Trim() ?? string.Empty;
        var now = DateTime.Now;
        var baseGetMembersQuery = context.Members.AsNoTracking();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            if (int.TryParse(searchTerm, out var id))
                baseGetMembersQuery = baseGetMembersQuery.Where(m => m.IdMember == id);
            else
                baseGetMembersQuery = baseGetMembersQuery.Where(m =>
                    m.FirstName.Contains(searchTerm) ||
                    m.LastName.Contains(searchTerm));
        }
        
        var statsMembers = await  baseGetMembersQuery
            .GroupBy(_ => 1)
            .Select(m => new
            {
                TotalMembers = m.Count(),
                DeletedMembers = m.Count(member => member.IsDeleted),
                ActiveMembers = m.Count(member => 
                    !member.IsDeleted && 
                    member.SubscriptionMemberships.Any(sm => 
                        sm.Subscription.IsActive && sm.Subscription.EndDate >= now))
            }).FirstOrDefaultAsync(cancellationToken)?? new { TotalMembers = 0, DeletedMembers = 0, ActiveMembers = 0 };

        var expiredMembers = (statsMembers.TotalMembers - statsMembers.DeletedMembers) - statsMembers.ActiveMembers;

        var query = request.StatusFilter?.ToLower() switch
        {
            "active" => baseGetMembersQuery.Where(m => !m.IsDeleted && m.SubscriptionMemberships.Any(sm => sm.Subscription.EndDate >= now && sm.Subscription.IsActive)),
            "expired" => baseGetMembersQuery.Where(m => !m.IsDeleted && !m.SubscriptionMemberships.Any(sm => sm.Subscription.EndDate >= now && sm.Subscription.IsActive)),
            "deleted" => baseGetMembersQuery.Where(m => m.IsDeleted),
            _ => baseGetMembersQuery.Where(m => !m.IsDeleted)
        };
        
        const int pageSize = 15; 
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
                    .Any(sm => sm.Subscription.EndDate >= now && sm.Subscription.IsActive)
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