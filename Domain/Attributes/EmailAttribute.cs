using Common;
using System.ComponentModel.DataAnnotations;


namespace Domain.Attributes
{
    public sealed class EmailAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? inp, ValidationContext validationContext)
        {
            if (inp?.ToString() != null && !inp.ToString()!.IsValidEmail())
            {
                return new ValidationResult($"{validationContext.DisplayName}:{nameof(ErrorInput.Invalid)}");
            }
            return ValidationResult.Success!;
        }
    }
}