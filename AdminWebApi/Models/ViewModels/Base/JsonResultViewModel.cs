using Common;
using Domain.Attributes;

namespace EndPoint.WebService.Models.ViewModels.Base
{
    public class JsonResultViewModel
    {
        public object? Result { set; get; }
        public ResultStatus Status { set; get; }
    }
}