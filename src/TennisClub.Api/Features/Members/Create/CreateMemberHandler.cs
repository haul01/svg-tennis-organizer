using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Auth.Shared;
using TennisClub.Api.Features.Members.Shared;
using TennisClub.Api.Infrastructure.Email;

namespace TennisClub.Api.Features.Members.Create;

public sealed class CreateMemberHandler(
    UserManager<Member> users,
    IEmailSender email,
    IOptions<FrontendSettings> frontend,
    TimeProvider time)
{
    public async Task<Result<MemberDetailDto>> HandleAsync(
        CreateMemberRequest req, CancellationToken ct)
    {
        var member = new Member
        {
            Id = Guid.NewGuid(),
            UserName = req.Email.Trim(),
            Email = req.Email.Trim(),
            EmailConfirmed = true,
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            IsActive = true,
            CreatedAt = time.GetUtcNow()
        };

        // Create without a password - the welcome mail routes the user to
        // the set-password flow, same token shape as forgot-password.
        var create = await users.CreateAsync(member);
        if (!create.Succeeded)
        {
            var failures = create.Errors
                .Select(e => new ValidationFailure(e.Code, e.Description))
                .ToList();
            return Result.Invalid(failures);
        }

        await users.AddToRoleAsync(member, req.Role);

        var token = await users.GeneratePasswordResetTokenAsync(member);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var encodedEmail = Uri.EscapeDataString(member.Email!);
        var setPasswordUrl = $"{frontend.Value.BaseUrl.TrimEnd('/')}" +
                             $"/set-password?email={encodedEmail}&token={encodedToken}";

        var html = $"""
            <p>Hallo {member.FirstName},</p>
            <p>ein Administrator hat ein Konto für dich im TennisClub angelegt.</p>
            <p>Klicke den folgenden Link, um dein Passwort zu setzen und dich zum ersten Mal anzumelden:</p>
            <p><a href="{setPasswordUrl}">Passwort setzen und einloggen</a></p>
            <p>Der Link ist 24 Stunden gültig. Falls du kein Konto bei uns erwartet hast,
            ignoriere diese Nachricht.</p>
            """;

        await email.SendAsync(
            new EmailMessage(member.Email!, "Willkommen im TennisClub", html),
            ct);

        return Result.Success(new MemberDetailDto(
            member.Id, member.Email!, member.FirstName, member.LastName,
            req.Role, member.IsActive, member.CreatedAt));
    }
}
