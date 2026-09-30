namespace Common.Dto
{
    public class ResultDto
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public int Status { get; set; }
        public long Id { get; set; }
        public object? Result { set; get; }
    }
    public class ResultDto<T>
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public int Status { get; set; }
        public object? Result { set; get; }
        public T Data { get; set; }
    }
    public class ResultListDto
    {
        public int Count { get; set; }
        public int Current { get; set; }

    }
    public class RequestListDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Sort { get; set; }
        public string? Filter { get; set; }
    }
    public class RequestListFrontendDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Sort { get; set; }
        public string? Filter { get; set; }
        public string? Slug { get; set; }
    }
    public class ResultImageDto
    {
        public string Preview { get; set; }
        public long Id { get; set; }

    }
}
