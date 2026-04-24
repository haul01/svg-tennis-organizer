using FluentValidation;

namespace TennisClub.Api.Features.Auth.Refresh;

public sealed class RefreshValidator : AbstractValidator<RefreshRequest>
{
    public RefreshValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
