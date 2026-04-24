using FluentValidation;
using TennisClub.Api.Features.Members.Shared;

namespace TennisClub.Api.Features.Members.Update;

public sealed class UpdateMemberValidator : AbstractValidator<UpdateMemberRequest>
{
    public UpdateMemberValidator()
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
