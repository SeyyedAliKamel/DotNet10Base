namespace AdminWebApi.Models.ViewModels.Basket
{
    public class UpdateBasketViewModel
    {
        public long ProductId { get; set; }
        public string Type { get; set; }
        public bool? IsGrounded { get; set; }
    }
}
