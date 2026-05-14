using FluentValidation;
using TennisClub.Api.Infrastructure.Persistence.Seed;

namespace TennisClub.Api.Features.Members.ChangeRole;

public sealed class ChangeRoleValidator : AbstractValidator<ChangeRoleRequest>
{
    // Single source of truth for "what counts as a valid role to assign".
    // Kept in sync with SeedData.EnsureRolesAsync.
    public static readonly string[] AllowedRoles =
    [
        SeedData.MemberRole,
        SeedData.GuestRole,
        SeedData.TrainerRole,
        SeedData.AdminRole
    ];

    public ChangeRoleValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Rolle fehlt.")
            .Must(r => AllowedRoles.Contains(r, StringComparer.Ordinal))
            .WithMessage($"Rolle muss eine der folgenden sein: {string.Join(", ", AllowedRoles)}.");
    }
}
