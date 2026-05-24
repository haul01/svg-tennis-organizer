namespace TennisClub.Api.Features.Membership.Apply;

/// <summary>
/// Configures who receives a copy of every membership application submitted
/// through the public form. When NotificationEmail is empty the handler
/// falls back to the SMTP FromAddress so the admin still gets the mail.
/// </summary>
public sealed class MembershipApplicationSettings
{
    public const string SectionName = "MembershipApplications";

    public string NotificationEmail { get; set; } = "";
}
