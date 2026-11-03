using FluentValidation;

namespace Gavel.Identity.Api.Features.Auth;

internal static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .MinimumLength(MinLength)
        .MaximumLength(MaxLength)
        .Matches("[A-Za-z]").WithMessage("Password must contain a letter.")
        .Matches("[0-9]").WithMessage("Password must contain a digit.");
}
