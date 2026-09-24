using Common;

namespace AdminWebApi.Models.ViewModels.Additional
{
    public class AddBannerViewModel
    {
        public string Title { get; set; }
        public string AdminTitle { get; set; }
        public string Url { get; set; }
        /// <summary>
        /// BannerPosition:
        /// اسلایدر = 0
        /// بنر اصلی موبایل = 1
        /// بنر اصلی دسکتاپ = 2
        /// </summary>
        public BannerPositionEnum Position { get; set; } = BannerPositionEnum.Slider;
        public IFormFile File { get; set; }
        public bool IsActive { get; set; }
    }
}
