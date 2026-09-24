using Common;

namespace FrontendWebApi.Models.ViewModels.Common
{
    public class RequestReviewListViewModel
    {
        public long? ReferenceId { get; set; }
        public ReviewTypeEnum? Type { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
