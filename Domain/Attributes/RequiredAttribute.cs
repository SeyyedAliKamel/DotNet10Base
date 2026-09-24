using Common;
using System.ComponentModel.DataAnnotations;


namespace Domain.Attributes
{
    public sealed class RequiredAttribute : System.ComponentModel.DataAnnotations.RequiredAttribute
    {
        protected override ValidationResult IsValid(object? inp, ValidationContext validationContext)
        {
            if (inp == null)
            {
                return new ValidationResult($"{validationContext.DisplayName}:{nameof(ErrorInput.Required)}");
            }
            return ValidationResult.Success!;
        }
    }
}