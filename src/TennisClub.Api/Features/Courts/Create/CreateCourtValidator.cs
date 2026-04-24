using FluentValidation;

namespace TennisClub.Api.Features.Courts.Create;

public sealed class CreateCourtValidator : AbstractValidator<CreateCourtRequest>
{
    public CreateCourtValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue);
    }
}
