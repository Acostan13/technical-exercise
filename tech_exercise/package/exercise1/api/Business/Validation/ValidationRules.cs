using FluentValidation;

namespace StargateAPI.Business.Validation
{
    /// <summary>
    /// Field rules shared by every request that accepts the same kind of value.
    /// </summary>
    public static class ValidationRules
    {
        public const int PersonNameMaxLength = 100;
        public const int RankMaxLength = 50;
        public const int DutyTitleMaxLength = 100;

        public static IRuleBuilderOptions<T, string> ValidPersonName<T>(this IRuleBuilder<T, string> rule) =>
            rule.NotEmpty().WithMessage("{PropertyName} is required.")
                .MaximumLength(PersonNameMaxLength).WithMessage("{PropertyName} must be {MaxLength} characters or fewer.");

        public static IRuleBuilderOptions<T, string> ValidRank<T>(this IRuleBuilder<T, string> rule) =>
            rule.NotEmpty().WithMessage("Rank is required.")
                .MaximumLength(RankMaxLength).WithMessage("Rank must be {MaxLength} characters or fewer.");

        public static IRuleBuilderOptions<T, string> ValidDutyTitle<T>(this IRuleBuilder<T, string> rule) =>
            rule.NotEmpty().WithMessage("Duty Title is required.")
                .MaximumLength(DutyTitleMaxLength).WithMessage("Duty Title must be {MaxLength} characters or fewer.");
    }

    public static class Normalize
    {
        /// <summary>
        /// Trims surrounding whitespace so " John Doe " and "John Doe" are the same Person, and treats a
        /// missing value as empty so validation (not a NullReferenceException) reports it.
        /// </summary>
        public static string Text(string? value) => value?.Trim() ?? string.Empty;
    }
}
