using Microsoft.Extensions.Options;
using TennisClub.Api.Infrastructure.Email;

namespace TennisClub.Api.Features.Membership.Apply;

public sealed class ApplyMembershipHandler(
    EmailQueue email,
    EmailTemplateRenderer templates,
    IOptions<MembershipApplicationSettings> settings,
    IOptions<SmtpSettings> smtp,
    TimeProvider time,
    ILogger<ApplyMembershipHandler> log)
{
    public async Task HandleAsync(ApplyMembershipRequest req, CancellationToken ct)
    {
        var submittedAt = time.GetUtcNow();
        var feeLabel = MembershipFeeTiers.Label(req.FeeTier);

        // Admin recipient: dedicated config wins, otherwise the SMTP From
        // address (the verein's own inbox) so the mail always lands
        // somewhere a human reads.
        var adminRecipient = settings.Value.NotificationEmail;
        if (string.IsNullOrWhiteSpace(adminRecipient))
        {
            adminRecipient = smtp.Value.FromAddress;
        }

        var adminModel = new
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Street = req.Street,
            PostalCode = req.PostalCode,
            City = req.City,
            BirthDate = req.BirthDate.ToString("dd.MM.yyyy"),
            Phone = req.Phone,
            Email = req.Email,
            FeeTier = feeLabel,
            Comment = req.Comment ?? "",
            HasComment = !string.IsNullOrWhiteSpace(req.Comment),
            SubmittedAt = submittedAt.ToString("dd.MM.yyyy HH:mm")
        };

        try
        {
            if (!string.IsNullOrWhiteSpace(adminRecipient))
            {
                var adminMail = await templates.RenderEmailAsync(
                    "membership-application-admin", adminModel, ct);
                await email.EnqueueAsync(
                    new EmailMessage(adminRecipient,
                        $"Neue Beitrittserklärung: {req.FirstName} {req.LastName}",
                        adminMail.Html, adminMail.Plain),
                    ct);
            }
            else
            {
                log.LogWarning(
                    "Membership application from {Email} received but no admin "
                    + "notification address is configured (MembershipApplications:NotificationEmail "
                    + "and Smtp:FromAddress are both empty).", req.Email);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to enqueue admin notification for "
                + "membership application from {Email}.", req.Email);
        }

        try
        {
            var applicantMail = await templates.RenderEmailAsync(
                "membership-application-confirmation", adminModel, ct);
            await email.EnqueueAsync(
                new EmailMessage(req.Email,
                    "Beitrittserklärung eingegangen",
                    applicantMail.Html, applicantMail.Plain),
                ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to enqueue applicant confirmation for "
                + "membership application from {Email}.", req.Email);
        }
    }
}
