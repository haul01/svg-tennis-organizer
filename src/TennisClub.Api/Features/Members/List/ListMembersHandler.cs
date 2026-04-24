using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Members.Shared;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Members.List;

public sealed class ListMembersHandler(AppDbContext db)
{
    public async Task<List<MemberListItemDto>> HandleAsync(
        string? search,
        string? status,
        string? role,
        CancellationToken ct)
    {
        // Joined query against Identity role tables - keeps a single roundtrip.
        var query =
            from m in db.Users.AsNoTracking()
            join ur in db.UserRoles on m.Id equals ur.UserId into mRoles
            from ur in mRoles.DefaultIfEmpty()
            join r in db.Roles on ur.RoleId equals r.Id into rs
            from r in rs.DefaultIfEmpty()
            select new { Member = m, RoleName = r != null ? r.Name : null };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Member.FirstName, pattern) ||
                EF.Functions.Like(x.Member.LastName, pattern) ||
                EF.Functions.Like(x.Member.Email!, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.Member.IsActive);
            else if (status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => !x.Member.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(x => x.RoleName == role);
        }

        var rows = await query
            .OrderBy(x => x.Member.LastName).ThenBy(x => x.Member.FirstName)
            .ToListAsync(ct);

        // A member with no role assignment still needs a default displayed;
        // Identity normally keeps every user in at least one role via seed.
        return [.. rows.Select(x => new MemberListItemDto(
            x.Member.Id,
            x.Member.Email ?? "",
            x.Member.FirstName,
            x.Member.LastName,
            x.RoleName ?? "Member",
            x.Member.IsActive,
            x.Member.CreatedAt))];
    }
}
