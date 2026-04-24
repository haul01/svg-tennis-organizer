using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;

namespace TennisClub.Api.Features.Auth.ResetPassword;

public sealed class ResetPasswordHandler(UserManager<Member> users)
{
    public async Task<Result> HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(req.Email);
        if (user is null || !user.IsActive)
        {
            // Generic failure - no hint whether the email exists.
            return Result.Invalid("Der Link ist ungültig oder abgelaufen.");
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(req.Token));
        }
        catch (FormatException)
        {
            return Result.Invalid("Der Link ist ungültig oder abgelaufen.");
        }

        var result = await users.ResetPasswordAsync(user, decodedToken, req.NewPassword);
        if (result.Succeeded) return Result.Success();

        var failures = result.Errors
            .Select(e => new ValidationFailure(e.Code, e.Description))
            .ToList();
        return Result.Invalid(failures);
    }
}
