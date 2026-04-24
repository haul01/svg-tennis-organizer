using FluentValidation;

namespace TennisClub.Api.Features.Courts.Update;

public sealed class UpdateCourtValidator : AbstractValidator<UpdateCourtRequest>
{
    public UpdateCourtValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
