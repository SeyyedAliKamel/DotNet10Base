using Domain.Attributes;

namespace Common
{
    public enum ResultStatus
    {
        Failed,
        Success,
        Unauthorized,
        NotAccess,
    }
    public enum ErrorInput
    {
        Required,
        Invalid,
        Length,
        MaxLength,
        MinLength,
        NotAllowed
    }
    public enum LoginLogStatus
    {
        Login,
        Logout,
    }
    public enum TypeSaveFile
    {
        Product,
        ProductCategory,
        BannerSlider,
        CatalogFile,
        Content,
        Branch,
        FrequentQuestionCategory,
        FrequentQuestion,
        ContentCategory,
        Slider,
        Banner,
        Public,
        ProductSlider,
        PaymentType,
        ProductDetail,
        People,
        ProductDetailImage,
        Editor,
        BasketItem,
        Cheque,
    }
    public enum BasketStatus
    {
        Created = 0,
        Cancelled = 1,
        Finalized = 2,
        Rejected = 3,
        Sent = 4,
    }
    public enum PaymentType
    {
        Online,
        Cheque
    }
    public enum WeightType
    {
        Gram,
        Kilogram,
        Pound,
    }
    public enum UserTypeEnum
    {
        Shop = -1,
        Individual = 0,
        Company = 1,
        CashSell = -2,
    }
    public enum UserStatusEnum
    {
        Active = 1,
        Inactive = 0,
        Suspend = 2
    }
    public enum ScoreTypeEnum
    {
        Deposit = 1, // واریز
        Withdraw = 0, // برداشت
        Lottery = 2
    }
    public enum SlugTypeEnum
    {
        Product = 0,
        Content = 1,
        ProductCategory = 2,
        ContentCategory = 3,
    }
    public enum OfferStatus
    {
        Active = 1,
        Inactive = 0,
        Repetitive = 2,
        Upcoming = 3,
    }
    public enum ImageRoleEnum
    {
        Single = 0,
        Primary = 1,
        Secondary = 2
    }
    public enum ImageTypeEnum
    {
        Product = 0,
        Content = 1,
        ProductCategory = 2,
        ContentCategory = 3,
        Branch = 4,
        Editor = 5,
        BasketItem = 6,
        Cheque = 7,
    }
    public enum ReviewTypeEnum
    {
        Product = 0,
        Content = 1,
    }
    public enum BannerPositionEnum
    {
        Slider,
        MainMobile ,
        MainDesktop ,
        BannerType1 ,
        BannerType2 ,
        BannerType3 ,
        BannerType4 ,
        BannerType5 ,
        BannerType6 ,
        BannerType7 ,
    }
    public enum MenuTypeEnum
    {
        Internal = 0,
        External = 1
    }
    public enum CampaignTypeEnum
    {
        Once = 0,
        Daily = 1,
        Hourly = 2,
    }
    public enum Gender
    {
        None = 0,
        Male = 1,
        Female = 2,
    }
    public enum PaymentTransactionStatus
    {
        Created = 0,
        RedirectedToGateway = 1,
        GatewayRejected = 2,
        ReturnedFromGateway = 3,
        Verified = 4,
        Failed = 5
    }
    public enum OrderTypeEnum
    {
        Product = 0,
        ProductCategory = 1,
        Content = 2,
        ContentCategory = 3,
        Brand = 4
    }
}