using Microsoft.AspNetCore.Identity;

namespace TennisClub.Api.Domain.Entities;

public class Member : IdentityUser<Guid>
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = [];
}
