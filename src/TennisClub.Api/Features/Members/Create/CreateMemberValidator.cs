using FluentValidation;
using TennisClub.Api.Features.Members.Shared;

namespace TennisClub.Api.Features.Members.Create;

public sealed class CreateMemberValidator : AbstractValidator<CreateMemberRequest>
{
    public CreateMemberValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(MembershipRoles.IsKnown)
            .WithMessage($"Rolle muss eine von {string.Join(", ", MembershipRoles.All)} sein.");
    }
}
