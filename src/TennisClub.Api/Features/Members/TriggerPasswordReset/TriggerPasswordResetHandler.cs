using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Auth.Shared;
using TennisClub.Api.Infrastructure.Email;

namespace TennisClub.Api.Features.Members.TriggerPasswordReset;

public sealed class TriggerPasswordResetHandler(
    UserManager<Member> users,
    IEmailSender email,
    IOptions<FrontendSettings> frontend)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return Result.NotFound("Mitglied nicht gefunden.");
        if (!user.IsActive) return Result.Invalid("Für inaktive Mitglieder kann kein Passwort-Reset ausgelöst werden.");

        var token = await users.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var encodedEmail = Uri.EscapeDataString(user.Email!);
        var url = $"{frontend.Value.BaseUrl.TrimEnd('/')}" +
                  $"/set-password?email={encodedEmail}&token={encodedToken}";

        var html = $"""
            <p>Hallo {user.FirstName},</p>
            <p>ein Administrator hat einen Passwort-Reset für dein Konto ausgelöst.</p>
            <p><a href="{url}">Passwort neu setzen</a></p>
            <p>Der Link ist 24 Stunden gültig. Falls du das nicht erwartet hast,
            wende dich an den Vereinsvorstand.</p>
            """;

        await email.SendAsync(
            new EmailMessage(user.Email!, "Passwort zurücksetzen", html), ct);

        return Result.Success();
    }
}
