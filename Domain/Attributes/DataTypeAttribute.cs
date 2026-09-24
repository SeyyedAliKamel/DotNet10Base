using Common;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;


namespace Domain.Attributes
{
    public sealed class DataTypeAttribute : System.ComponentModel.DataAnnotations.DataTypeAttribute
    {
        private readonly string _dataType;
        public DataTypeAttribute(string dataType) : base(dataType)
        {
            _dataType = dataType;
        }
        protected override ValidationResult? IsValid(object? inp, ValidationContext validationContext)
        {
            var isFile = inp?.GetType() == typeof(List<IFormFile>);
            if (inp != null && isFile && ((List<IFormFile>)inp).Any(x => !_dataType.Split(',').Contains(x.FileName.Split('.').Last())))
            {
                return new ValidationResult($"{validationContext.DisplayName}:{nameof(ErrorInput.NotAllowed)}");
            }
            return base.IsValid(inp, validationContext);
        }
    }
}