using Domain.Attributes;

namespace EndPoint.WebService.Models.ViewModels.Base
{
    public class ListViewModel
    {
        public int Page { set; get; } = 1;
        public int PerPage { set; get; } = 10;
        private string Sort { set; get; } = "Id";
        private string SortOrder { set; get; } = "DESC";
        public string SortBy
        {
            set { }
            get => $"{Sort} {SortOrder}";
        }
    }

    public class ResultListViewModel
    {
        public int Count { set; get; }
        public IEnumerable<object> List { set; get; } = new List<object>();
        public object? Search { set; get; }
    }

    public class IdViewModel
    {
        public int Id { set; get; }
    }
    public class PIdViewModel
    {
        public int PId { set; get; }
        public int Id { set; get; }
    }

    public class DropdownViewModel
    {
        public int Id { set; get; }
        public string Title { set; get; }
    }
}