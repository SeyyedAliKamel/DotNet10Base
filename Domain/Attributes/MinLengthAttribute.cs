using Common;
using System.ComponentModel.DataAnnotations;


namespace Domain.Attributes
{
    public sealed class MinLengthAttribute : System.ComponentModel.DataAnnotations.MinLengthAttribute
    {
        private readonly int _minLength;
        public MinLengthAttribute(int minLength) : base(minLength)
        {
            _minLength = minLength;
        }

        protected override ValidationResult IsValid(object? inp, ValidationContext validationContext)
        {
            if (inp?.ToString() != null && inp.ToString()?.Length < _minLength)
            {
                return new ValidationResult($"{validationContext.DisplayName}:{nameof(ErrorInput.MinLength)}");
            }
            return ValidationResult.Success!;
        }
    }
}