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
    EmailQueue email,
    EmailTemplateRenderer templates,
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

        try
        {
            var html = await templates.RenderAsync("password-reset", new
            {
                FirstName = user.FirstName,
                ResetUrl = url,
                TriggeredByAdmin = true
            }, ct);

            await email.EnqueueAsync(
                new EmailMessage(user.Email!, "Passwort zurücksetzen", html), ct);
        }
        catch { /* dispatcher logs the failure */ }

        return Result.Success();
    }
}
