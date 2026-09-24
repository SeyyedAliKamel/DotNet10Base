using Common;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;


namespace Domain.Attributes
{
    public sealed class MaxLengthAttribute : System.ComponentModel.DataAnnotations.MaxLengthAttribute
    {
        private readonly int _maxLength;
        public MaxLengthAttribute(int maxLength) : base(maxLength)
        {
            _maxLength = maxLength;
        }

        protected override ValidationResult IsValid(object? inp, ValidationContext validationContext)
        {
            var isFile = inp?.GetType() == typeof(List<IFormFile>);
            if (!isFile && inp?.ToString() != null && inp.ToString()?.Length > _maxLength)
            {
                return new ValidationResult($"{validationContext.DisplayName}:{nameof(ErrorInput.MaxLength)}");
            }
            if (inp != null && isFile && ((List<IFormFile>)inp).Any(x => x.Length > _maxLength * 1024 * 1024))
            {
                return new ValidationResult($"{validationContext.DisplayName}:{nameof(ErrorInput.MaxLength)}");
            }
            return ValidationResult.Success!;
        }
    }
}