namespace TennisClub.Api.Features.Reservations.Rules;

public static class BookingRulesExtensions
{
    public static IServiceCollection AddBookingRules(this IServiceCollection services)
    {
        services.AddScoped<BookingRuleEngine>();

        services.AddScoped<IBookingRule, SlotBoundsAreValidRule>();
        services.AddScoped<IBookingRule, SlotIsNotInPastRule>();
        services.AddScoped<IBookingRule, SlotIsWithinSeasonRule>();
        services.AddScoped<IBookingRule, SlotIsWithinOpeningHoursRule>();
        services.AddScoped<IBookingRule, CourtIsActiveRule>();
        services.AddScoped<IBookingRule, CourtAllowsGuestRule>();
        services.AddScoped<IBookingRule, NoCourtBlockExistsRule>();
        services.AddScoped<IBookingRule, NoOverlappingReservationRule>();
        services.AddScoped<IBookingRule, MaxAdvanceBookingRule>();
        services.AddScoped<IBookingRule, MaxOpenReservationsRule>();

        return services;
    }
}
