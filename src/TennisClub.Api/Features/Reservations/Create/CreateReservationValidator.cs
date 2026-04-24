using FluentValidation;

namespace TennisClub.Api.Features.Reservations.Create;

public sealed class CreateReservationValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationValidator()
    {
        RuleFor(x => x.CourtId).GreaterThan(0);
        RuleFor(x => x.StartsAt).LessThan(x => x.EndsAt);
    }
}
