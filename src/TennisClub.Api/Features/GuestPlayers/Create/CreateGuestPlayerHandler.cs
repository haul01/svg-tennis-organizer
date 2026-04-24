using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.GuestPlayers.Shared;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.GuestPlayers.Create;

public sealed class CreateGuestPlayerHandler(AppDbContext db, TimeProvider time)
{
    public async Task<Result<GuestPlayerDto>> HandleAsync(
        CreateGuestPlayerRequest req, Guid invitedByMemberId, CancellationToken ct)
    {
        var guest = new GuestPlayer
        {
            Id = Guid.NewGuid(),
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
            InvitedByMemberId = invitedByMemberId,
            CreatedAt = time.GetUtcNow(),
            IsActive = true
        };

        db.GuestPlayers.Add(guest);
        await db.SaveChangesAsync(ct);

        return Result.Success(new GuestPlayerDto(
            guest.Id, guest.FirstName, guest.LastName, guest.Email, guest.IsActive, guest.CreatedAt));
    }
}
