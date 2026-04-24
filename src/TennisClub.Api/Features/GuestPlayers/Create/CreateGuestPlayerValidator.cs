using FluentValidation;

namespace TennisClub.Api.Features.GuestPlayers.Create;

public sealed class CreateGuestPlayerValidator : AbstractValidator<CreateGuestPlayerRequest>
{
    public CreateGuestPlayerValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Email).MaximumLength(256);
    }
}
