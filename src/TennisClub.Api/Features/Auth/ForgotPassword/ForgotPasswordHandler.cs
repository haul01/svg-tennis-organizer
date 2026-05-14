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
    EmailQueue email,
    EmailTemplateRenderer templates,
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

        try
        {
            var rendered = await templates.RenderEmailAsync("password-reset", new
            {
                FirstName = user.FirstName,
                ResetUrl = resetUrl,
                TriggeredByAdmin = false
            }, ct);

            await email.EnqueueAsync(
                new EmailMessage(user.Email!, "Passwort zurücksetzen",
                    rendered.Html, rendered.Plain),
                ct);
        }
        catch
        {
            // Mail pipeline failures are logged by the dispatcher; the
            // user-facing endpoint must stay generic for enumeration safety.
        }
    }
}
