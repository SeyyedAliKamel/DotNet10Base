using Application.Interfaces.Contexts;
using Common;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.Common;
using Domain.Entities.Users;
using Microsoft.CodeAnalysis.Host;
using Microsoft.EntityFrameworkCore;
using PaypalServerSdk.Standard.Models;
using System.Linq.Dynamic.Core;
using System.Text.Json;

namespace Application.Services.SeedData;

public interface ISeedDataService
{
    void Ensure();
}

public class SeedDataService : ISeedDataService
{
    private readonly IDataBaseContext _context;

    public SeedDataService(IDataBaseContext context)
    {
        _context = context;
    }

    public void Ensure()
    {
        InsureRole();
        InsureUser();
        //InsureStatesAndCities();
    }

    private void InsureUser()
    {
        if (!_context.Users.Any())
        {
            var role = _context.Roles.First(x => x.Name == "Admin");
            _context.Users.AddRange(new List<User>
            {
                new()
                {
                    FirstName = "Super",
                    LastName = "Admin",
                    Password = Hash.Get("123456"),
                    IsActive = true,
                    UserGuid = Guid.NewGuid(),
                    Sessions = new List<UserSession>
                    {
                        new()
                        {
                            Platform = "swagger",
                            Ip = "1.1.1.1",
                            Browser = "test",
                            Token = "ssb"
                        }
                    },
                    Roles = new List<Role> {role}
                }
            });
            _context.SaveChanges();
        }
    }

    private void InsureStatesAndCities()
    {
        if (_context.States.Any())
            return;

        var provincesFile = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "Application",
            "Services",
            "SeedData",
            "Data",
            "geo.json"));

        if (!File.Exists(provincesFile))
            throw new FileNotFoundException(provincesFile);

        using var doc = JsonDocument.Parse(File.ReadAllText(provincesFile));

        foreach (var provinceElement in doc.RootElement.EnumerateArray())
        {
            var provinceName = provinceElement.GetProperty("province").GetString();

            if (string.IsNullOrWhiteSpace(provinceName))
                continue;

            var state = _context.States.FirstOrDefault(x => x.Name == provinceName);

            if (state == null)
            {
                state = new State { Name = provinceName };
                _context.States.Add(state);
                _context.SaveChanges();
            }

            if (!provinceElement.TryGetProperty("cities", out var citiesElement) ||
                citiesElement.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var cityElement in citiesElement.EnumerateArray())
            {
                var cityName = cityElement.GetString();

                if (string.IsNullOrWhiteSpace(cityName))
                    continue;

                var cityExists = _context.Cities.Any(x => x.Name == cityName && x.StateId == state.Id);

                if (cityExists)
                    continue;

                _context.Cities.Add(new City
                {
                    Name = cityName,
                    StateId = state.Id
                });
            }
        }

        _context.SaveChanges();
    }

    private void InsureRole()
        {
            var permissionCategories = new List<PermissionCategory>
            {
                new()
                {
                    Title = "کاربران",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت کاربر",
                            Controller = "User",
                            Action = "AddUser",
                            FrontEndName = "UserAddUser"
                        },
                        new ()
                        {
                            Name = "ویرایش کاربر",
                            Controller = "User",
                            Action = "EditUser",
                            FrontEndName = "UserEditUser"
                        },
                        new ()
                        {
                            Name = "حذف کاربر",
                            Controller = "User",
                            Action = "RemoveUser",
                            FrontEndName = "UserRemoveUser"
                        },
                        new ()
                        {
                            Name = "لیست کاربران",
                            Controller = "User",
                            Action = "UserList",
                            FrontEndName = "UserUserList"
                        },
                        new ()
                        {
                            Name = "اطلاعات کاربر",
                            Controller = "User",
                            Action = "UserItem",
                            FrontEndName = "UserUserItem"
                        },
                        new ()
                        {
                            Name = "سطح دسترسی کاربر",
                            Controller = "User",
                            Action = "UserInfo",
                            FrontEndName = "UserUserInfo"
                        },
                        new ()
                        {
                            Name = "تغییر رمز کاربر خود",
                            Controller = "User",
                            Action = "ChangePasswordByCurrentUser",
                            FrontEndName = "UserChangePasswordByCurrentUser"
                        },
                        new ()
                        {
                            Name = "تغییر رمز کاربر",
                            Controller = "User",
                            Action = "ChangePasswordByUser",
                            FrontEndName = "UserChangePasswordByUser"
                        },
                    }
                },
                new()
                {
                    Title = "محصولات",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت محصول",
                            Controller = "Product",
                            Action = "AddProduct",
                            FrontEndName = "ProductAddProduct"
                        },
                        new ()
                        {
                            Name = "ویرایش محصول",
                            Controller = "Product",
                            Action = "EditProduct",
                            FrontEndName = "ProductEditProduct"
                        },
                        new ()
                        {
                            Name = "حذف محصول",
                            Controller = "Product",
                            Action = "RemoveProduct",
                            FrontEndName = "ProductRemoveProduct"
                        },
                        new ()
                        {
                            Name = "ویرایش عکس اصلی محصول",
                            Controller = "Product",
                            Action = "EditProductPrimaryImage",
                            FrontEndName = "ProductEditProductPrimaryImage"
                        },
                        new ()
                        {
                            Name = "حذف عکس محصول",
                            Controller = "Product",
                            Action = "RemoveProductImage",
                            FrontEndName = "ProductRemoveProductImage"
                        },
                        new ()
                        {
                            Name = "اطلاعات محصول",
                            Controller = "Product",
                            Action = "ProductItem",
                            FrontEndName = "ProductProductItem"
                        },
                        new ()
                        {
                            Name = "لیست محصولات",
                            Controller = "Product",
                            Action = "ProductList",
                            FrontEndName = "ProductProductList"
                        }
                    }
                },
                new()
                {
                    Title = "تنظیمات",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت تنظیمات",
                            Controller = "Setting",
                            Action = "AddSetting",
                            FrontEndName = "SettingAddSetting"
                        },
                        new ()
                        {
                            Name = "ویرایش تنظیمات",
                            Controller = "Setting",
                            Action = "EditSetting",
                            FrontEndName = "SettingEditSetting"
                        },
                        new ()
                        {
                            Name = "حذف تنظیمات",
                            Controller = "Setting",
                            Action = "RemoveSetting",
                            FrontEndName = "SettingRemoveSetting"
                        },
                        new ()
                        {
                            Name = "اطلاعات تنظیمات",
                            Controller = "Setting",
                            Action = "SettingItem",
                            FrontEndName = "SettingSettingItem"
                        },
                        new ()
                        {
                            Name = "لیست تنظیمات",
                            Controller = "Setting",
                            Action = "SettingList",
                            FrontEndName = "SettingSettingList"
                        }
                    }
                },
                new()
                {
                    Title = "دسته بندی محصولات",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت دسته بندی محصول",
                            Controller = "ProductCategory",
                            Action = "AddProductCategory",
                            FrontEndName = "ProductCategoryAddProductCategory"
                        },
                        new ()
                        {
                            Name = "ویرایش دسته بندی محصول",
                            Controller = "ProductCategory",
                            Action = "EditProductCategory",
                            FrontEndName = "ProductCategoryEditProductCategory"
                        },
                        new ()
                        {
                            Name = "حذف دسته بندی محصول",
                            Controller = "ProductCategory",
                            Action = "RemoveProductCategory",
                            FrontEndName = "ProductCategoryRemoveProductCategory"
                        },
                        new ()
                        {
                            Name = "اطلاعات دسته بندی محصول",
                            Controller = "ProductCategory",
                            Action = "ProductCategoryItem",
                            FrontEndName = "ProductCategoryProductCategoryItem"
                        },
                        new ()
                        {
                            Name = "لیست دسته بندی محصول",
                            Controller = "ProductCategory",
                            Action = "ProductCategoryList",
                            FrontEndName = "ProductCategoryProductCategoryList"
                        }
                    }
                },
                new()
                {
                    Title = "تخفیف محصولات",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت تخفیف محصول",
                            Controller = "ProductOffer",
                            Action = "AddProductOffer",
                            FrontEndName = "ProductOfferAddProductOffer"
                        },
                        new ()
                        {
                            Name = "ویرایش نخفیف محصول",
                            Controller = "ProductOffer",
                            Action = "EditProductOffer",
                            FrontEndName = "ProductOfferEditProductOffer"
                        },
                        new ()
                        {
                            Name = "حذف تخفیف محصول",
                            Controller = "ProductOffer",
                            Action = "RemoveProductOffer",
                            FrontEndName = "ProductOfferRemoveProductOffer"
                        },
                        new ()
                        {
                            Name = "اطلاعات تخفیف محصول",
                            Controller = "ProductOffer",
                            Action = "ProductOfferItem",
                            FrontEndName = "ProductOfferProductOfferItem"
                        },
                        new ()
                        {
                            Name = "لیست تخفیف محصول",
                            Controller = "ProductOffer",
                            Action = "ProductOfferList",
                            FrontEndName = "ProductOfferProductOfferList"
                        }
                    }
                },
                new()
                {
                    Title = "برند ها",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت برند",
                            Controller = "Brand",
                            Action = "AddBrand",
                            FrontEndName = "BrandAddBrand"
                        },
                        new ()
                        {
                            Name = "ویرایش برند",
                            Controller = "Brand",
                            Action = "EditBrand",
                            FrontEndName = "BrandEditBrand"
                        },
                        new ()
                        {
                            Name = "حذف برند",
                            Controller = "Brand",
                            Action = "RemoveBrand",
                            FrontEndName = "BrandRemoveBrand"
                        },
                        new ()
                        {
                            Name = "اطلاعات برند",
                            Controller = "Brand",
                            Action = "BrandItem",
                            FrontEndName = "BrandBrandItem"
                        },
                        new ()
                        {
                            Name = "لیست برند",
                            Controller = "Brand",
                            Action = "BrandList",
                            FrontEndName = "BrandBrandList"
                        },
                    }
                },
                new()
                {
                    Title = "عکس ها",
                    Permissions = new List<Permission>
                    {
                        new()
                        {
                            Name = "ثبت عکس ادیتور",
                            Controller = "Image",
                            Action = "AddImageEditor",
                            FrontEndName = "ImageAddImage"
                        },
                        new()
                        {
                            Name = "ویرایش عکس ادیتور",
                            Controller = "Image",
                            Action = "EditImageEditor",
                            FrontEndName = "ImageEditImage"
                        },
                        new()
                        {
                            Name = "حذف عکس",
                            Controller = "Image",
                            Action = "RemoveImage",
                            FrontEndName = "ImageRemoveImage"
                        },
                        new()
                        {
                            Name = "اطلاعات عکس",
                            Controller = "Image",
                            Action = "ImageItem",
                            FrontEndName = "ImageImageItem"
                        },
                        new()
                        {
                            Name = "لیست عکس ادیتور",
                            Controller = "Image",
                            Action = "ImageListEditor",
                            FrontEndName = "ImageImageList"
                        },
                    }
                },
                new()
                {
                    Title = "امتیاز ها",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت امتیاز",
                            Controller = "Score",
                            Action = "AddScore",
                            FrontEndName = "ScoreAddScore"
                        },
                        new ()
                        {
                            Name = "ویرایش امتیاز",
                            Controller = "Score",
                            Action = "EditScore",
                            FrontEndName = "ScoreEditScore"
                        },
                        new ()
                        {
                            Name = "حذف امتیاز",
                            Controller = "Score",
                            Action = "RemoveScore",
                            FrontEndName = "ScoreRemoveScore"
                        },
                        new ()
                        {
                            Name = "اطلاعات امتیاز",
                            Controller = "Score",
                            Action = "ScoreItem",
                            FrontEndName = "ScoreScoreItem"
                        },
                        new ()
                        {
                            Name = "لیست امتیاز",
                            Controller = "Score",
                            Action = "ScoreList",
                            FrontEndName = "ScoreScoreList"
                        },
                        new ()
                        {
                            Name = "ثبت امتیاز کمپین",
                            Controller = "Score",
                            Action = "AddScoreCampaign",
                            FrontEndName = "ScoreAddScoreCampaign"
                        },
                        new ()
                        {
                            Name = "جمع امتیاز کاربر",
                            Controller = "Score",
                            Action = "ScoreTotal",
                            FrontEndName = "ScoreScoreTotal"
                        }
                    }
                },
                new()
                {
                    Title = "کیف پول ها",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت کیف پول",
                            Controller = "Wallet",
                            Action = "AddWallet",
                            FrontEndName = "WalletAddWallet"
                        },
                        new ()
                        {
                            Name = "ویرایش کیف پول",
                            Controller = "Wallet",
                            Action = "EditWallet",
                            FrontEndName = "WalletEditWallet"
                        },
                        new ()
                        {
                            Name = "حذف کیف پول",
                            Controller = "Wallet",
                            Action = "RemoveWallet",
                            FrontEndName = "WalletRemoveWallet"
                        },
                        new ()
                        {
                            Name = "اطلاعات کیف پول",
                            Controller = "Wallet",
                            Action = "WalletItem",
                            FrontEndName = "WalletWalletItem"
                        },
                        new ()
                        {
                            Name = "لیست کیف پول",
                            Controller = "Wallet",
                            Action = "WalletList",
                            FrontEndName = "WalletWalletList"
                        },
                    }
                },
                new()
                {
                    Title = "هشتگ ها",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت هشتگ",
                            Controller = "Hashtag",
                            Action = "AddHashtag",
                            FrontEndName = "HashtagAddHashtag"
                        },
                        new ()
                        {
                            Name = "ویرایش هشتگ",
                            Controller = "Hashtag",
                            Action = "EditHashtag",
                            FrontEndName = "HashtagEditHashtag"
                        },
                        new ()
                        {
                            Name = "حذف هشتگ",
                            Controller = "Hashtag",
                            Action = "RemoveHashtag",
                            FrontEndName = "HashtagRemoveHashtag"
                        },
                        new ()
                        {
                            Name = "اطلاعات هشتگ",
                            Controller = "Hashtag",
                            Action = "HashtagItem",
                            FrontEndName = "HashtagHashtagItem"
                        },
                        new ()
                        {
                            Name = "لیست هشتگ",
                            Controller = "Hashtag",
                            Action = "HashtagList",
                            FrontEndName = "HashtagHashtagList"
                        },
                    }
                },
                new()
                {
                    Title = "محتوا",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت محتوا",
                            Controller = "Content",
                            Action = "AddContent",
                            FrontEndName = "ContentAddContent"
                        },
                        new ()
                        {
                            Name = "ویرایش محتوا",
                            Controller = "Content",
                            Action = "EditContent",
                            FrontEndName = "ContentEditContent"
                        },
                        new ()
                        {
                            Name = "حذف محتوا",
                            Controller = "Content",
                            Action = "RemoveContent",
                            FrontEndName = "ContentRemoveContent"
                        },
                        new ()
                        {
                            Name = "اطلاعات محتوا",
                            Controller = "Content",
                            Action = "ContentItem",
                            FrontEndName = "ContentContentItem"
                        },
                        new ()
                        {
                            Name = "لیست محتوا",
                            Controller = "Content",
                            Action = "ContentList",
                            FrontEndName = "ContentContentList"
                        },
                        new ()
                        {
                            Name = "اشتراک گذاری محتوا",
                            Controller = "Content",
                            Action = "ContentShare",
                            FrontEndName = "ContentContentShare"
                        },
                    },
                },
                new()
                {
                    Title = "دسته بندی محنوا",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت دسته بندی محتوا",
                            Controller = "ContentCategory",
                            Action = "AddContentCategory",
                            FrontEndName = "ContentCategoryAddContentCategory"
                        },
                        new ()
                        {
                            Name = "ویرایش دسته بندی محتوا",
                            Controller = "ContentCategory",
                            Action = "EditContentCategory",
                            FrontEndName = "ContentCategoryEditContentCategory"
                        },
                        new ()
                        {
                            Name = "حذف دسته بندی محتوا",
                            Controller = "ContentCategory",
                            Action = "RemoveContentCategory",
                            FrontEndName = "ContentCategoryRemoveContentCategory"
                        },
                        new ()
                        {
                            Name = "اطلاعات دسته بندی محتوا",
                            Controller = "ContentCategory",
                            Action = "ContentCategoryItem",
                            FrontEndName = "ContentCategoryContentCategoryItem"
                        },
                        new ()
                        {
                            Name = "لیستدسته بندی  محتوا",
                            Controller = "ContentCategory",
                            Action = "ContentCategoryList",
                            FrontEndName = "ContentCategoryContentCategoryList"
                        },
                    },
                },
                new()
                {
                    Title = "بنر ها",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت بنر",
                            Controller = "Banner",
                            Action = "AddBanner",
                            FrontEndName = "BannerAddBanner"
                        },
                        new ()
                        {
                            Name = "ویرایش بنر",
                            Controller = "Banner",
                            Action = "EditBanner",
                            FrontEndName = "BannerEditBanner"
                        },
                        new ()
                        {
                            Name = "حذف بنر",
                            Controller = "Banner",
                            Action = "RemoveBanner",
                            FrontEndName = "BannerRemoveBanner"
                        },
                        new ()
                        {
                            Name = "اطلاعات بنر",
                            Controller = "Banner",
                            Action = "BannerItem",
                            FrontEndName = "BannerBannerItem"
                        },
                        new ()
                        {
                            Name = "لیست بنر",
                            Controller = "Banner",
                            Action = "BannerList",
                            FrontEndName = "BannerBannerList"
                        },
                    }
                },
                new()
                {
                    Title = "منو ها",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "افزودن منو",
                            Controller = "Menu",
                            Action = "AddMenu",
                            FrontEndName = "MenuAddMenu"
                        },
                        new ()
                        {
                            Name = "ویرایش منو",
                            Controller = "Menu",
                            Action = "EditMenu",
                            FrontEndName = "MenuEditMenu"
                        },
                        new ()
                        {
                            Name = "حذف منو",
                            Controller = "Menu",
                            Action = "RemoveMenu",
                            FrontEndName = "MenuRemoveMenu"
                        },
                        new ()
                        {
                            Name = "لیست منو",
                            Controller = "Menu",
                            Action = "MenuList",
                            FrontEndName = "MenuMenuList"
                        },
                        new ()
                        {
                            Name = "اطلاعات منو",
                            Controller = "Menu",
                            Action = "MenuItem",
                            FrontEndName = "MenuMenuItem"
                        },
                    }
                },
                new()
                {
                    Title = "مدیریت امتیاز",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "افزودن مدیریت امتیاز",
                            Controller = "ManageScore",
                            Action = "AddManageScore",
                            FrontEndName = "ManageScoreAddManageScore"
                        },
                        new ()
                        {
                            Name = "ویرایش مدیریت امتیاز",
                            Controller = "ManageScore",
                            Action = "EditManageScore",
                            FrontEndName = "ManageScoreEditManageScore"
                        },
                        new ()
                        {
                            Name = "حذف مدیریت امتیاز",
                            Controller = "ManageScore",
                            Action = "RemoveManageScore",
                            FrontEndName = "ManageScoreRemoveManageScore"
                        },
                        new ()
                        {
                            Name = "لیست مدیریت امتیاز",
                            Controller = "ManageScore",
                            Action = "ManageScoreList",
                            FrontEndName = "ManageScoreManageScoreList"
                        },
                        new ()
                        {
                            Name = "اطلاعات مدیریت امتیاز",
                            Controller = "ManageScore",
                            Action = "ManageScoreItem",
                            FrontEndName = "ManageScoreManageScoreItem"
                        },
                    }
                },
                new()
                {
                    Title = "شعبه ها",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت شعبه",
                            Controller = "Branch",
                            Action = "AddBranch",
                            FrontEndName = "BranchAddBranch"
                        },
                        new ()
                        {
                            Name = "ویرایش شعبه",
                            Controller = "Branch",
                            Action = "EditBranch",
                            FrontEndName = "BranchEditBranch"
                        },
                        new ()
                        {
                            Name = "حذف شعبه",
                            Controller = "Branch",
                            Action = "RemoveBranch",
                            FrontEndName = "BranchRemoveBranch"
                        },
                        new ()
                        {
                            Name = "اطلاعات شعبه",
                            Controller = "Branch",
                            Action = "BranchItem",
                            FrontEndName = "BranchBranchItem"
                        },
                        new ()
                        {
                            Name = "لیست شعبه",
                            Controller = "Branch",
                            Action = "BranchList",
                            FrontEndName = "BranchBranchList"
                        },
                    }
                },
                new()
                {
                    Title = "کمپین ها",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت کمپین",
                            Controller = "Campaign",
                            Action = "AddCampaign",
                            FrontEndName = "CampaignAddCampaign"
                        },
                        new ()
                        {
                            Name = "ویرایش کمپین",
                            Controller = "Campaign",
                            Action = "EditCampaign",
                            FrontEndName = "CampaignEditCampaign"
                        },
                        new ()
                        {
                            Name = "حذف کمپین",
                            Controller = "Campaign",
                            Action = "RemoveCampaign",
                            FrontEndName = "CampaignRemoveCampaign"
                        },
                        new ()
                        {
                            Name = "اطلاعات کمپین",
                            Controller = "Campaign",
                            Action = "CampaignItem",
                            FrontEndName = "CampaignCampaignItem"
                        },
                        new ()
                        {
                            Name = "لیست کمپین",
                            Controller = "Campaign",
                            Action = "CampaignList",
                            FrontEndName = "CampaignCampaignList"
                        },
                    }
                },
                new()
                {
                    Title = "اسلاگ ها",
                    Permissions = new List<Permission>
                    {
                        new()
                        {
                            Name = "ثبت اسلاگ",
                            Controller = "Slug",
                            Action = "AddSlug",
                            FrontEndName = "SlugAddSlug"
                        },
                        new()
                        {
                            Name = "ویرایش اسلاگ",
                            Controller = "Slug",
                            Action = "EditSlug",
                            FrontEndName = "SlugEditSlug"
                        },
                        new()
                        {
                            Name = "حذف اسلاگ",
                            Controller = "Slug",
                            Action = "RemoveSlug",
                            FrontEndName = "SlugRemoveSlug"
                        },
                        new()
                        {
                            Name = "اطلاعات اسلاگ",
                            Controller = "Slug",
                            Action = "SlugItem",
                            FrontEndName = "SlugSlugItem"
                        },
                        new()
                        {
                            Name = "لیست اسلاگ",
                            Controller = "Slug",
                            Action = "SlugList",
                            FrontEndName = "SlugSlugList"
                        },
                    }
                },
                new()
                {
                    Title = "قرعه کشی",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "ثبت قرعه کشی",
                            Controller = "Lottery",
                            Action = "AddLottery",
                            FrontEndName = "LotteryAddLottery"
                        },
                        new ()
                        {
                            Name = "ویرایش قرعه کشی",
                            Controller = "Lottery",
                            Action = "EditLottery",
                            FrontEndName = "LotteryEditLottery"
                        },
                        new ()
                        {
                            Name = "حذف قرعه کشی",
                            Controller = "Lottery",
                            Action = "RemoveLottery",
                            FrontEndName = "LotteryRemoveLottery"
                        },
                        new ()
                        {
                            Name = "اطلاعات قرعه کشی",
                            Controller = "Lottery",
                            Action = "LotteryItem",
                            FrontEndName = "LotteryLotteryItem"
                        },
                        new ()
                        {
                            Name = "لیست قرعه کشی",
                            Controller = "Lottery",
                            Action = "LotteryList",
                            FrontEndName = "LotteryLotteryList"
                        },
                        new ()
                        {
                            Name = "لیست افراد داخل قرعه کشی",
                            Controller = "Lottery",
                            Action = "LotteryResultList",
                            FrontEndName = "LotteryLotteryResultList"
                        },
                        new ()
                        {
                            Name = "اضافه کردن برنده قرعه کشی",
                            Controller = "Lottery",
                            Action = "AddLotteryWinner",
                            FrontEndName = "LotteryAddLotteryWinner"
                        },
                        new ()
                        {
                            Name = "برندگاه قرعه کشی",
                            Controller = "Lottery",
                            Action = "LotteryWinnerList",
                            FrontEndName = "LotteryLotteryWinnerList"
                        }
                    }
                },
                new()
                {
                    Title = "نظرها",
                    Permissions = new List<Permission>
                    {
                        new()
                        {
                            Name = "ثبت نظر",
                            Controller = "Review",
                            Action = "AddReview",
                            FrontEndName = "ReviewAddReview"
                        },
                        new()
                        {
                            Name = "ویرایش نظر",
                            Controller = "Review",
                            Action = "EditReview",
                            FrontEndName = "ReviewEditReview"
                        },
                        new()
                        {
                            Name = "حذف نظر",
                            Controller = "Review",
                            Action = "RemoveReview",
                            FrontEndName = "ReviewRemoveReview"
                        },
                        new()
                        {
                            Name = "اطلاعات نظر",
                            Controller = "Review",
                            Action = "ReviewItem",
                            FrontEndName = "ReviewReviewItem"
                        },
                        new()
                        {
                            Name = "لیست نظرها",
                            Controller = "Review",
                            Action = "ReviewList",
                            FrontEndName = "ReviewReviewList"
                        }
                    }
                },
                new()
                {
                    Title = "کد تخفیف",
                    Permissions = new List<Permission>
                    {
                        new()
                        {
                            Name = "ثبت کد تخفیف",
                            Controller = "DiscountCode",
                            Action = "AddDiscountCode",
                            FrontEndName = "DiscountCodeAddDiscountCode"
                        },
                        new()
                        {
                            Name = "ویرایش کد تخفیف",
                            Controller = "DiscountCode",
                            Action = "EditDiscountCode",
                            FrontEndName = "DiscountCodeEditDiscountCode"
                        },
                        new()
                        {
                            Name = "حذف کد تخفیف",
                            Controller = "DiscountCode",
                            Action = "RemoveDiscountCode",
                            FrontEndName = "DiscountCodeRemoveDiscountCode"
                        },
                        new()
                        {
                            Name = "اطلاعات کد تخفیف",
                            Controller = "DiscountCode",
                            Action = "DiscountCodeItem",
                            FrontEndName = "DiscountCodeDiscountCodeItem"
                        },
                        new()
                        {
                            Name = "لیست کد تخفیف‌ها",
                            Controller = "DiscountCode",
                            Action = "DiscountCodeList",
                            FrontEndName = "DiscountCodeDiscountCodeList"
                        },
                        new()
                        {
                            Name = "لیست استفاده از کد تخفیف‌",
                            Controller = "DiscountCode",
                            Action = "DiscountCodeUsageList",
                            FrontEndName = "DiscountCodeDiscountCodeUsageList"
                        },
                    }
                },
                new()
                {
                    Title = "روش ارسال",
                    Permissions = new List<Permission>
                    {
                        new()
                        {
                            Name = "ثبت روش ارسال",
                            Controller = "Shipping",
                            Action = "AddShipping",
                            FrontEndName = "ShippingAddShipping"
                        },
                        new()
                        {
                            Name = "ویرایش روش ارسال",
                            Controller = "Shipping",
                            Action = "EditShipping",
                            FrontEndName = "ShippingEditShipping"
                        },
                        new()
                        {
                            Name = "حذف روش ارسال",
                            Controller = "Shipping",
                            Action = "RemoveShipping",
                            FrontEndName = "ShippingRemoveShipping"
                        },
                        new()
                        {
                            Name = "اطلاعات روش ارسال",
                            Controller = "Shipping",
                            Action = "ShippingItem",
                            FrontEndName = "ShippingShippingItem"
                        },
                        new()
                        {
                            Name = "لیست روش های ارسال",
                            Controller = "Shipping",
                            Action = "ShippingList",
                            FrontEndName = "ShippingShippingList"
                        },
                    }
                },
                new()
                {
                    Title = "ارتباط با ما ها",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "حذف ارتباط با ما",
                            Controller = "ContactUs",
                            Action = "RemoveContactUs",
                            FrontEndName = "ContactUsRemoveContactUs"
                        },
                        new ()
                        {
                            Name = "اطلاعات ارتباط با ما",
                            Controller = "ContactUs",
                            Action = "ContactUsItem",
                            FrontEndName = "ContactUsContactUsItem"
                        },
                        new ()
                        {
                            Name = "لیست ارتباط با ما",
                            Controller = "ContactUs",
                            Action = "ContactUsList",
                            FrontEndName = "ContactUsContactUsList"
                        },
                    }
                },
                new()
                {
                    Title = "سبد ها",
                    Permissions = new List<Permission>
                    {
                        new()
                        {
                            Name = "تغییر وضعیت سبد",
                            Controller = "Basket",
                            Action = "ChangeBasketStatus",
                            FrontEndName = "BasketChangeBasketStatus"
                        },
                        new()
                        {
                            Name = "اطلاعات سبد",
                            Controller = "Basket",
                            Action = "BasketDetail",
                            FrontEndName = "BasketBasketItem"
                        },
                        new()
                        {
                            Name = "لیست سبد",
                            Controller = "Basket",
                            Action = "BasketList",
                            FrontEndName = "BasketBasketList"
                        },
                        new()
                        {
                            Name = "لیست سبد های کافی شاپ ها",
                            Controller = "Basket",
                            Action = "BasketListBranch",
                            FrontEndName = "BasketBasketListBranch"
                        }
                    }
                },
                new()
                {
                    Title = "موقعیت جغرافیایی",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "لیست شهر",
                            Controller = "Geo",
                            Action = "CityList",
                            FrontEndName = "GeoCityList"
                        },
                        new ()
                        {
                            Name = "اطلاعات شهر",
                            Controller = "Geo",
                            Action = "CityItem",
                            FrontEndName = "GeoCityItem"
                        },
                        new ()
                        {
                            Name = "لیست استان",
                            Controller = "Geo",
                            Action = "StateList",
                            FrontEndName = "GeoStateList"
                        },
                        new ()
                        {
                            Name = "اطلاعات استان",
                            Controller = "Geo",
                            Action = "StateItem",
                            FrontEndName = "GeoStateItem"
                        },
                        new ()
                        {
                            Name = "تغییر وضعیت موقعیت جغرافیایی",
                            Controller = "Geo",
                            Action = "UpdateGeoStatus",
                            FrontEndName = "GeoUpdateGeoStatus"
                        },
                        new ()
                        {
                            Name = "افزودن استان",
                            Controller = "Geo",
                            Action = "AddState",
                            FrontEndName = "GeoAddState"
                        },
                        new ()
                        {
                            Name = "ویرایش استان",
                            Controller = "Geo",
                            Action = "EditState",
                            FrontEndName = "GeoEditState"
                        },
                        new ()
                        {
                            Name = "حذف استان",
                            Controller = "Geo",
                            Action = "RemoveState",
                            FrontEndName = "GeoRemoveState"
                        },
                        new ()
                        {
                            Name = "افزودن شهر",
                            Controller = "Geo",
                            Action = "AddCity",
                            FrontEndName = "GeoAddCity"
                        },
                        new ()
                        {
                            Name = "ویرایش شهر",
                            Controller = "Geo",
                            Action = "EditCity",
                            FrontEndName = "GeoEditCity"
                        },
                        new ()
                        {
                            Name = "حذف شهر",
                            Controller = "Geo",
                            Action = "RemoveCity",
                            FrontEndName = "GeoRemoveCity"
                        },
                    }
                },
                new()
                {
                    Title = "نظرسنجی",
                    Permissions = new List<Permission>
                    {
                        new ()
                        {
                            Name = "افزودن سوال نظرسنجی",
                            Controller = "Survey",
                            Action = "AddSurveyQuestion",
                            FrontEndName = "SurveyAddSurveyQuestion"
                        },
                        new ()
                        {
                            Name = "ویرایش سوال نظرسنجی",
                            Controller = "Survey",
                            Action = "EditSurveyQuestion",
                            FrontEndName = "SurveyEditSurveyQuestion"
                        },
                        new ()
                        {
                            Name = "حذف سوال نظرسنجی",
                            Controller = "Survey",
                            Action = "RemoveSurveyQuestion",
                            FrontEndName = "SurveyRemoveSurveyQuestion"
                        },
                        new ()
                        {
                            Name = "افزودن گزینه نظرسنجی",
                            Controller = "Survey",
                            Action = "AddSurveyOption",
                            FrontEndName = "SurveyAddSurveyOption"
                        },
                        new ()
                        {
                            Name = "حذف گزینه نظرسنجی",
                            Controller = "Survey",
                            Action = "RemoveSurveyOption",
                            FrontEndName = "SurveyRemoveSurveyOption"
                        },
                        new ()
                        {
                            Name = "لیست سوالات نظرسنجی",
                            Controller = "Survey",
                            Action = "SurveyQuestionList",
                            FrontEndName = "SurveySurveyQuestionList"
                        },
                        new ()
                        {
                            Name = "دریافت سوال نظرسنجی",
                            Controller = "Survey",
                            Action = "SurveyQuestionItem",
                            FrontEndName = "SurveySurveyQuestionItem"
                        },
                        new ()
                        {
                            Name = "پاسخ‌های کاربر نظرسنجی",
                            Controller = "Survey",
                            Action = "SurveyAnswers",
                            FrontEndName = "SurveySurveyAnswers"
                        },
                        new ()
                        {
                            Name = "نتایج نظرسنجی",
                            Controller = "Survey",
                            Action = "SurveyResults",
                            FrontEndName = "SurveySurveyResults"
                        },
                    }
                },
                new()
                {
                    Title = "نقش‌ها",
                    Permissions = new List<Permission>
                    {
                        new()
                        {
                            Name = "ثبت نقش",
                            Controller = "Role",
                            Action = "AddRole",
                            FrontEndName = "RoleAddRole"
                        },
                        new()
                        {
                            Name = "ویرایش نقش",
                            Controller = "Role",
                            Action = "EditRole",
                            FrontEndName = "RoleEditRole"
                        },
                        new()
                        {
                            Name = "حذف نقش",
                            Controller = "Role",
                            Action = "RemoveRole",
                            FrontEndName = "RoleRemoveRole"
                        },
                        new()
                        {
                            Name = "ویرایش دسترسی‌های نقش",
                            Controller = "Role",
                            Action = "UpdateRolePermissions",
                            FrontEndName = "RoleUpdateRolePermissions"
                        },
                        new()
                        {
                            Name = "اطلاعات نقش",
                            Controller = "Role",
                            Action = "RoleItem",
                            FrontEndName = "RoleRoleItem"
                        },
                        new()
                        {
                            Name = "لیست نقش‌ها",
                            Controller = "Role",
                            Action = "RoleList",
                            FrontEndName = "RoleRoleList"
                        },
                        new()
                        {
                            Name = "دسترسی‌های نقش",
                            Controller = "Role",
                            Action = "RolePermissions",
                            FrontEndName = "RoleRolePermissions"
                        }
                    }
                },
                new()
                {
                    Title = "دسترسی‌ها",
                    Permissions = new List<Permission>
                    {
                        new()
                        {
                            Name = "ثبت دسترسی",
                            Controller = "Permission",
                            Action = "AddPermission",
                            FrontEndName = "PermissionAddPermission"
                        },
                        new()
                        {
                            Name = "ویرایش دسترسی",
                            Controller = "Permission",
                            Action = "EditPermission",
                            FrontEndName = "PermissionEditPermission"
                        },
                        new()
                        {
                            Name = "حذف دسترسی",
                            Controller = "Permission",
                            Action = "RemovePermission",
                            FrontEndName = "PermissionRemovePermission"
                        },
                        new()
                        {
                            Name = "اطلاعات دسترسی",
                            Controller = "Permission",
                            Action = "PermissionItem",
                            FrontEndName = "PermissionPermissionItem"
                        },
                        new()
                        {
                            Name = "لیست دسترسی‌ها",
                            Controller = "Permission",
                            Action = "PermissionList",
                            FrontEndName = "PermissionPermissionList"
                        },
                        new()
                        {
                            Name = "لیست دسترسی‌ها به همراه دسته‌بندی",
                            Controller = "Permission",
                            Action = "PermissionWithCategoryList",
                            FrontEndName = "PermissionPermissionWithCategoryList"
                        }
                    }
                },
                new()
                {
                    Title = "دسته‌بندی دسترسی‌ها",
                    Permissions = new List<Permission>
                    {
                        new()
                        {
                            Name = "ثبت دسته‌بندی دسترسی",
                            Controller = "PermissionCategory",
                            Action = "AddPermissionCategory",
                            FrontEndName = "PermissionCategoryAddPermissionCategory"
                        },
                        new()
                        {
                            Name = "ویرایش دسته‌بندی دسترسی",
                            Controller = "PermissionCategory",
                            Action = "EditPermissionCategory",
                            FrontEndName = "PermissionCategoryEditPermissionCategory"
                        },
                        new()
                        {
                            Name = "حذف دسته‌بندی دسترسی",
                            Controller = "PermissionCategory",
                            Action = "RemovePermissionCategory",
                            FrontEndName = "PermissionCategoryRemovePermissionCategory"
                        },
                        new()
                        {
                            Name = "اطلاعات دسته‌بندی دسترسی",
                            Controller = "PermissionCategory",
                            Action = "PermissionCategoryItem",
                            FrontEndName = "PermissionCategoryPermissionCategoryItem"
                        },
                        new()
                        {
                            Name = "لیست دسته‌بندی دسترسی‌ها",
                            Controller = "PermissionCategory",
                            Action = "PermissionCategoryList",
                            FrontEndName = "PermissionCategoryPermissionCategoryList"
                        }
                    }
                },
                new()
                {
                    Title = "ترتیب ها",
                    Permissions = new List<Permission>
                    {
                        new()
                        {
                            Name = "ویرایش ترتیب",
                            Controller = "Order",
                            Action = "EditOrder",
                            FrontEndName = "OrderEditOrder"
                        },
                        new()
                        {
                            Name = "پر کردن ترتیب",
                            Controller = "Order",
                            Action = "FillNulls",
                            FrontEndName = "OrderFillNulls"
                        },
                        new()
                        {
                            Name = "بازگردانی ترتیب",
                            Controller = "Order",
                            Action = "ResetOrder",
                            FrontEndName = "OrderResetOrder"
                        },
                    }
                },
            };
            if (!_context.Roles.Any())
            {
                _context.PermissionCategories.AddRange(permissionCategories);
                _context.SaveChanges();
                _context.Roles.AddRange(new List<Role>
                {
                    new()
                    {
                        Name = "SuperAdmin",
                        Permissions = permissionCategories.SelectMany(x => x.Permissions ?? new List<Permission>()).ToList()
                    },
                    new()
                    {
                        Name = "User"
                    },
                });
                _context.SaveChanges();

            }
            else
            {
                var adminRole = _context.Roles
                .Include(r => r.Permissions)
                .First(x => x.Name == "Admin");

                var existingPermissionIds = _context.Permissions
                    .Select(p => p.Id)
                    .ToHashSet();

                var newPermissions = permissionCategories
                    .SelectMany(pc => pc.Permissions ?? new List<Permission>())
                    .Where(p => !existingPermissionIds.Contains(p.Id))
                    .ToList();

                foreach (var category in permissionCategories)
                {
                    var dbCategory = _context.PermissionCategories
                        .Include(c => c.Permissions)
                        .FirstOrDefault(c => c.Title == category.Title);

                    if (dbCategory == null)
                    {
                        _context.PermissionCategories.Add(category);
                    }
                    else
                    {
                        var existingNames = dbCategory.Permissions.Select(p => p.Name).ToHashSet();
                        var newOnes = category.Permissions
                            .Where(p => !existingNames.Contains(p.Name))
                            .ToList();

                        if (newOnes.Any())
                            newOnes.ForEach(p => dbCategory.Permissions.Add(p));
                    }
                }

            _context.SaveChanges();

            var adminPermissionIds = adminRole.Permissions.Select(p => p.Id).ToHashSet();

            var permissionsToAddToAdmin = _context.Permissions
                .Where(p => !adminPermissionIds.Contains(p.Id))
                .ToList();

            if (permissionsToAddToAdmin.Any())
            {
                foreach (var permission in permissionsToAddToAdmin)
                {
                    adminRole.Permissions.Add(permission);
                }

                _context.SaveChanges();
            }
        }
    }

}