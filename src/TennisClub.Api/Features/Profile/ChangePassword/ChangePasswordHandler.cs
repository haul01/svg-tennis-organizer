using Microsoft.AspNetCore.Identity;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;

namespace TennisClub.Api.Features.Profile.ChangePassword;

public sealed class ChangePasswordHandler(UserManager<Member> users)
{
    public async Task<Result> HandleAsync(
        Guid memberId, ChangePasswordRequest req, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(memberId.ToString());
        if (user is null) return Result.NotFound("Mitglied nicht gefunden.");

        var result = await users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (result.Succeeded) return Result.Success();

        // Identity returns the same error whether the current password was
        // wrong or the new one failed the policy - collect them all so the
        // UI can show each reason.
        var failures = result.Errors
            .Select(e => new ValidationFailure(e.Code, e.Description))
            .ToList();
        return Result.Invalid(failures);
    }
}
