namespace TennisClub.Api.Domain.Entities;

public class GuestPlayer
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }

    public Guid InvitedByMemberId { get; set; }
    public Member InvitedBy { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
