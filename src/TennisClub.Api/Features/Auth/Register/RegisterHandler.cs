using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Auth.Shared;
using TennisClub.Api.Infrastructure.Email;
using TennisClub.Api.Infrastructure.Persistence.Seed;

namespace TennisClub.Api.Features.Auth.Register;

/// <summary>
/// Self-registers a Guest. The flow mirrors the admin-create-member
/// happy path (no password set, welcome mail with set-password link) so
/// existing identity infrastructure is reused 1:1. Response is always
/// generic 200 OK - prevents email enumeration just like ForgotPassword.
/// </summary>
public sealed class RegisterHandler(
    UserManager<Member> users,
    EmailQueue email,
    EmailTemplateRenderer templates,
    IOptions<FrontendSettings> frontend,
    TimeProvider time,
    ILogger<RegisterHandler> log)
{
    public async Task Handle(RegisterRequest req, CancellationToken ct)
    {
        var emailTrimmed = req.Email.Trim();

        // If the address already exists we silently exit (caller gets the
        // same generic 200 as for a brand-new sign-up). Re-sending a
        // welcome mail to existing accounts would let an attacker probe
        // membership status, so we just stop here.
        var existing = await users.FindByEmailAsync(emailTrimmed);
        if (existing is not null)
        {
            log.LogInformation(
                "Register: address {Email} already registered, returning generic OK", emailTrimmed);
            return;
        }

        var member = new Member
        {
            Id = Guid.NewGuid(),
            UserName = emailTrimmed,
            Email = emailTrimmed,
            EmailConfirmed = true,
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            IsActive = true,
            CreatedAt = time.GetUtcNow()
        };

        var create = await users.CreateAsync(member);
        if (!create.Succeeded)
        {
            // Identity rejected the user (e.g. malformed email even though
            // FluentValidation passed). Log + bail; still a generic 200.
            log.LogWarning(
                "Register: identity rejected {Email}: {Errors}",
                emailTrimmed,
                string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}")));
            return;
        }

        await users.AddToRoleAsync(member, SeedData.GuestRole);

        var token = await users.GeneratePasswordResetTokenAsync(member);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var encodedEmail = Uri.EscapeDataString(member.Email!);
        var setPasswordUrl = $"{frontend.Value.BaseUrl.TrimEnd('/')}" +
                             $"/set-password?email={encodedEmail}&token={encodedToken}";

        // Best-effort: failed mail must not block the (already-created)
        // account. The dispatcher logs its own errors.
        try
        {
            var rendered = await templates.RenderEmailAsync("welcome-guest", new
            {
                FirstName = member.FirstName,
                SetPasswordUrl = setPasswordUrl
            }, ct);

            await email.EnqueueAsync(
                new EmailMessage(member.Email!, "Willkommen im TennisClub",
                    rendered.Html, rendered.Plain),
                ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Register: failed to enqueue welcome mail for {Email}", member.Email);
        }
    }
}
