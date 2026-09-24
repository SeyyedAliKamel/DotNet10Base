using Common;
using System.ComponentModel.DataAnnotations;


namespace Domain.Attributes
{
    public sealed class LengthAttribute : System.ComponentModel.DataAnnotations.MaxLengthAttribute
    {
        private readonly int _length;
        public LengthAttribute(int length) : base(length)
        {
            _length = length;
        }
        protected override ValidationResult IsValid(object? inp, ValidationContext validationContext)
        {
            if (inp?.ToString() != null && inp.ToString()?.Length != _length)
            {
                return new ValidationResult($"{validationContext.DisplayName}:{nameof(ErrorInput.Length)}");
            }
            return ValidationResult.Success!;
        }
    }
}