namespace TennisClub.Api.Domain.Entities;

public class SystemSettings
{
    public int Id { get; set; }
    public int MaxAdvanceBookingDays { get; set; } = 7;
    public int MinCancellationHours { get; set; } = 0;
    public int MaxOpenReservationsPerMember { get; set; } = 2;
    /// <summary>
    /// Hard cap on how many consecutive slot units a single reservation can
    /// span. With 30-min slots this defaults to 4 (= 2 h max per booking).
    /// </summary>
    public int MaxSlotsPerBooking { get; set; } = 4;

    /// <summary>
    /// Friendly nudge shown to Guest-role users above the booking dialog's
    /// submit button. Admin-editable so each club can phrase its
    /// membership pitch the way it likes.
    /// </summary>
    public string GuestMembershipPromptText { get; set; } = DefaultGuestMembershipPromptText;

    /// <summary>
    /// Initial value for <see cref="GuestMembershipPromptText"/>. Also wired
    /// as the column default in the EF configuration so the migration
    /// backfills existing single-row SystemSettings rows.
    /// </summary>
    public const string DefaultGuestMembershipPromptText =
        "Schön, dass du bei uns spielst! Hast du schon überlegt, "
        + "Vereinsmitglied zu werden? Als Mitglied kannst du alle "
        + "Plätze buchen, hast bessere Buchungsbedingungen und "
        + "unterstützt damit unseren Verein. Wir freuen uns über "
        + "jede neue Mitgliedschaft!";
}
