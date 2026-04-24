using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Auth.Shared;
using TennisClub.Api.Infrastructure.Email;

namespace TennisClub.Api.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordHandler(
    UserManager<Member> users,
    IEmailSender email,
    IOptions<FrontendSettings> frontend)
{
    public async Task HandleAsync(ForgotPasswordRequest req, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(req.Email);
        if (user is null || !user.IsActive)
        {
            // Enumeration protection: silently succeed for unknown / inactive.
            return;
        }

        var token = await users.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var encodedEmail = Uri.EscapeDataString(user.Email!);

        var resetUrl = $"{frontend.Value.BaseUrl.TrimEnd('/')}" +
                       $"/set-password?email={encodedEmail}&token={encodedToken}";

        var html = $"""
            <p>Hallo {user.FirstName},</p>
            <p>klicke den folgenden Link, um dein Passwort zurückzusetzen:</p>
            <p><a href="{resetUrl}">Passwort setzen</a></p>
            <p>Der Link ist 24 Stunden gültig. Falls du keinen Reset angefordert hast,
            ignoriere diese Nachricht.</p>
            """;

        await email.SendAsync(
            new EmailMessage(user.Email!, "Passwort zurücksetzen", html),
            ct);
    }
}
