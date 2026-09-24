using System.Globalization;
using Application.Interfaces.Contexts;
using Application.Services.AdditionalInformations.Commands;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Domain.Entities.Scores;
using Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using static Common.Helper;

namespace Application.Services.Users.Commands
{
    // ==================== Interface ====================
    public interface IUserCUDService
    {
        ResultDto<long> AddUser(RequestAddUserDto request, long currentUserId);
        ResultDto EditUser(RequestEditUserDto request, long currentUserId);
        ResultDto RemoveUser(long id, long CurrentUserId);
        ResultDto ChangePasswordByCurrentUser(RequestChangePasswordByCurrentUserDto request, long currentUserId);
        ResultDto ChangePasswordByUser(RequestChangePasswordByUserDto request, long currentUserId);
        ResultDto<ResultUserLoginDto> CheckOtpCode(RequestCheckOtpCodeDto request, long currentUserId, string publicToken);
        ResultDto UpdateUserInfo(RequestUpdateUserInfoDto request, long currentUserId);
        ResultDto<string> GetOtpCode(string mobile, long currentUserId);
        ResultDto<ResultUserLoginDto> LoginWithPassword(LoginUserWithPasswordDto loginUserDto, long currentUserId, string publicToken);
        ResultDto<ResultUserLoginDto> LoginWithUserId(LoginUserWithIdDto loginUserDto, string publicToken);
        ResultDto<ResultRegisterUserDto> RegisterUser(RequestRegisterUserDto request, long currentUserId);
        ResultDto<string> RegisterNewUser(RequestRegisterNewUserDto request);
        ResultDto<long> AcceptUserAuthCode(RequestAcceptUserAuthCodeDto request);
        ResultDto<long> AcceptLoginAuthCodeBtn(RequestAcceptUserAuthCodeDto request);
        ResultDto<string> GetLoginOTPCode(RequestGetLoginOTOCodeDto request);
        ResultDto UserUploadList(List<RequestExcelUserListDto> request, long userCategoryId, long currentUserId);
        ResultDto ChangeStatusUser(long userId, long currentUserId);
        ResultDto ChangeUserPassword(long userId, long currentUserId);
        ResultDto Logout(string token, long currentUserId);
        ResultDto LogoutUser(long userId, long currentUserId);
        ResultDto UpdateUserRole(UpdateUserRoleDto request, long currentUserId);
    }

    // ==================== Implementation ====================
    public class UserCUDService : IUserCUDService
    {
        private readonly IDataBaseContext _context;

        // Format used for all Birthday string <-> DateTime? conversions in this service.
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        public UserCUDService(IDataBaseContext context)
        {
            _context = context;
        }


        // -------------------- AddUser --------------------
        public ResultDto<long> AddUser(RequestAddUserDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var (isValid, normalized) = MobileValidator.Normalize(request.Mobile);
                if (!isValid)
                    return FailData<long>("شماره موبایل معتبر نیست");

                request.Mobile = normalized;

                if (_context.Users.Any(x => x.Mobile == request.Mobile))
                    return FailData<long>("این شماره موبایل قبلاً ثبت شده است", 409);

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        return FailData<long>("ایمیل معتبر نیست");

                    if (_context.Users.Any(x => x.Email == request.Email))
                        return FailData<long>("این ایمیل قبلاً برای کاربر دیگری ثبت شده است", 409);
                }

                if (!TryParseDate(request.Birthday, out var birthday))
                    return FailData<long>("تاریخ تولد معتبر نیست");

                var password = string.IsNullOrWhiteSpace(request.Password)
                    ? Helper.GetRandom(10)
                    : request.Password;

                var user = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Mobile = request.Mobile,
                    Email = request.Email,
                    Password = Hash.Get(password),
                    ReferalCode = Helper.GetRandom(6, true, false, false, false),
                    IsActive = request.IsActive,
                    Status = request.IsActive ? UserStatusEnum.Active : UserStatusEnum.Inactive,
                    RequiredChangePassword = true,
                    Birthday = birthday,
                    Gender = request.Gender,
                    Job = request.Job,
                    Roles = new List<Role>()
                };

                _context.SetUserId(currentUserId);
                _context.Users.Add(user);
                _context.SaveChanges();

                if (request.RoleIds != null && request.RoleIds.Any())
                {
                    var roles = _context.Roles.Where(p => request.RoleIds.Contains(p.Id)).ToList();
                    foreach (var role in roles)
                        user.Roles.Add(role);

                    _context.SaveChanges();
                }

                transaction.Commit();

                return OkData(user.Id, "کاربر با موفقیت ایجاد شد");
            }
            catch (Exception ex)
            {
                return HandleException<long>(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- EditUser --------------------
        public ResultDto EditUser(RequestEditUserDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = _context.Users
                    .Include(p => p.Roles)
                    .FirstOrDefault(p => p.Id == request.UserId);

                if (user == null)
                    return Fail("کاربر یافت نشد");

                if (user.IsDeleted)
                    return Fail("کاربر حذف شده است");

                if (!string.IsNullOrWhiteSpace(request.Mobile))
                {
                    var (isValid, normalized) = MobileValidator.Normalize(request.Mobile);
                    if (!isValid)
                        return Fail("شماره موبایل معتبر نیست");

                    if (_context.Users.Any(x => x.Mobile == normalized && x.Id != user.Id))
                        return Fail("این شماره موبایل قبلاً ثبت شده است", 409);

                    user.Mobile = normalized;
                }

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        return Fail("ایمیل معتبر نیست");

                    if (_context.Users.Any(x => x.Email == request.Email && x.Id != user.Id))
                        return Fail("این ایمیل قبلاً برای کاربر دیگری ثبت شده است", 409);

                    user.Email = request.Email;
                }

                if (!TryParseDate(request.Birthday, out var birthday))
                    return Fail("تاریخ تولد معتبر نیست");

                user.FirstName = request.FirstName;
                user.LastName = request.LastName;

                if (request.IsActive.HasValue)
                {
                    user.IsActive = request.IsActive.Value;
                    user.Status = request.IsActive.Value ? UserStatusEnum.Active : UserStatusEnum.Inactive;
                }

                if (birthday.HasValue)
                    user.Birthday = birthday;

                if (request.Gender.HasValue)
                    user.Gender = request.Gender;

                if (!string.IsNullOrWhiteSpace(request.Job))
                    user.Job = request.Job;

                if (request.RoleIds != null)
                {
                    foreach (var role in user.Roles.ToList())
                        user.Roles.Remove(role);

                    var newRoles = _context.Roles.Where(p => request.RoleIds.Contains(p.Id)).ToList();
                    foreach (var role in newRoles)
                        user.Roles.Add(role);
                }

                _context.SetUserId(currentUserId);
                _context.Users.Update(user);
                _context.SaveChanges();
                transaction.Commit();

                return Ok("اطلاعات کاربر با موفقیت به روز گردید");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- RemoveUser --------------------
        public ResultDto RemoveUser(long userId, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null)
                    return Fail("کاربر یافت نشد");

                if (user.IsDeleted)
                    return Fail("کاربر قبلاً حذف شده است");

                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
                user.IsActive = false;

                _context.SetUserId(currentUserId);
                _context.Users.Update(user);
                _context.SaveChanges();
                transaction.Commit();

                return Ok("کاربر با موفقیت حذف شد");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, userId.ToJson(), currentUserId);
            }
        }

        // -------------------- UpdateUserInfo --------------------
        public ResultDto UpdateUserInfo(RequestUpdateUserInfoDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var existing = _context.Users.FirstOrDefault(u => u.Id == currentUserId);
                if (existing is null)
                    return Fail("کاربر یافت نشد");

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        return Fail("ایمیل معتبر نیست");

                    if (_context.Users.Any(x => x.Email == request.Email && x.Id != currentUserId))
                        return Fail("این ایمیل قبلاً برای کاربر دیگری ثبت شده است", 409);

                    existing.Email = request.Email;
                }

                if (!string.IsNullOrWhiteSpace(request.Birthday))
                {
                    if (!TryParseDate(request.Birthday, out var birthday))
                        return Fail("تاریخ تولد معتبر نیست");

                    if (birthday.HasValue && birthday.Value > DateTime.Now)
                        return Fail("تاریخ تولد نمی‌تواند در آینده باشد");

                    if (existing.Birthday != null)
                        return Fail("شما از قبل تاریخ تولد خود را ثبت کردید");

                    existing.Birthday = birthday;
                }

                if (!string.IsNullOrWhiteSpace(request.ReferalUserIdCode))
                {
                    if (existing.ReferalUserId != null)
                        return Fail("کد معرف قبلاً ثبت شده و قابل تغییر نیست");

                    if (request.ReferalUserIdCode.Length < 4)
                        return Fail("کد معرف باید حداقل ۴ کاراکتر باشد");

                    var referrer = _context.Users.FirstOrDefault(x => x.ReferalCode == request.ReferalUserIdCode);
                    if (referrer == null)
                        return Fail("کد معرف وارد شده معتبر نیست");

                    if (referrer.Id == existing.Id)
                        return Fail("امکان استفاده از کد معرف خودتان وجود ندارد");

                    existing.ReferalUserId = referrer.Id;
                }

                existing.FirstName = request.FirstName;
                existing.LastName = request.LastName;
                existing.Email = request.Email;
                existing.NewsLetter = request.NewsLetter;

                if (request.Gender.HasValue)
                    existing.Gender = request.Gender;

                if (!string.IsNullOrWhiteSpace(request.Job))
                    existing.Job = request.Job;

                if (!string.IsNullOrWhiteSpace(request.ReferalCode))
                {
                    if (request.ReferalCode.Length <= 4)
                        return Fail("کد معرف باید بیشتر از ۴ کاراکتر باشد");

                    existing.ReferalCode = request.ReferalCode;
                }

                _context.SaveChanges();
                transaction.Commit();

                return Ok("اطلاعات کاربر با موفقیت به روز گردید");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- ChangePasswordByCurrentUser --------------------
        public ResultDto ChangePasswordByCurrentUser(RequestChangePasswordByCurrentUserDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = _context.Users.Find(currentUserId);
                if (user == null)
                    return Fail("کاربر یافت نشد");

                if (request.Password != request.RepeatPassword)
                    return Fail("رمز و تکرار آن برابر نمی‌باشد");

                if (user.Password == Hash.Get(request.Password))
                    return Fail("پسورد شما با پسورد قبلی برابر است. لطفا رمز جدید وارد نمایید");

                user.Password = Hash.Get(request.Password);
                user.RequiredChangePassword = false;
                _context.Users.Update(user);
                _context.SaveChanges();
                transaction.Commit();

                return Ok("رمز کاربر با موفقیت تغییر کرد");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- ChangePasswordByUser --------------------
        public ResultDto ChangePasswordByUser(RequestChangePasswordByUserDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = _context.Users.Find(request.UserId);
                if (user == null)
                    return Fail("کاربر یافت نشد");

                if (request.Password != request.RepeatPassword)
                    return Fail("رمز و تکرار آن برابر نمی‌باشد");

                if (user.Password == Hash.Get(request.Password))
                    return Fail("پسورد شما با پسورد قبلی برابر است. لطفا رمز جدید وارد نمایید");

                user.Password = Hash.Get(request.Password);
                user.RequiredChangePassword = false;
                _context.Users.Update(user);
                _context.SaveChanges();
                transaction.Commit();

                return Ok("رمز کاربر با موفقیت تغییر کرد");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- CheckOtpCode --------------------
        public ResultDto<ResultUserLoginDto> CheckOtpCode(RequestCheckOtpCodeDto request, long currentUserId, string publicToken)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                if (request.AuthToken == null)
                    return FailData<ResultUserLoginDto>("توکن ارسالی اشتباه هست", 201);

                var user = _context.Users
                    .Include(p => p.Roles)
                    .FirstOrDefault(p => p.AuthToken == request.AuthToken);

                if (user == null)
                    return FailData<ResultUserLoginDto>("توکن ارسالی اشتباه هست", 201);

                if (user.CodeFail > 10)
                {
                    user.SuspendGenerateCode = DateTime.Now.AddMinutes(10);
                    _context.Users.Update(user);
                    _context.SaveChanges();
                }

                if (user.Code != request.Code)
                {
                    user.CodeFail += 1;
                    _context.SaveChanges();
                    return FailData<ResultUserLoginDto>("کد ارسالی اشتباه می باشد", 201);
                }

                var userSession = new UserSession
                {
                    Platform = request.requestDto.Platform,
                    Ip = request.requestDto.Ip,
                    Browser = request.requestDto.Browser,
                    Token = request.requestDto.Token,
                    UserId = user.Id
                };
                _context.UserSessions.Add(userSession);

                user.AuthToken = null;
                user.CodeFail = 0;
                user.IsActive = true;

                MergeBasket(publicToken, user.Id);

                var template = user.Roles.Any(p => p.Name == "Cashier") ? "NotMenu" : "AdminTemplate";

                if (string.IsNullOrWhiteSpace(user.Password))
                {
                    user.RequiredChangePassword = true;
                }
                user.Status = UserStatusEnum.Active;

                bool isProfileIncomplete =
                    string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName);

                _context.Users.Update(user);
                _context.SaveChanges();
                transaction.Commit();

                return OkData(new ResultUserLoginDto
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = userSession.Token,
                    UserId = user.Id,
                    Mobile = user.Mobile,
                    Email = user.Email,
                    RequiredChangePassword = user.RequiredChangePassword,
                    IsProfileIncomplete = isProfileIncomplete,
                    Roles = user.Roles.Select(r => new RoleItemDto { Id = r.Id, Name = r.Name }).ToList()
                }, "ورود با موفقیت انجام شد");
            }
            catch (Exception ex)
            {
                return HandleException<ResultUserLoginDto>(transaction, ex, request.ToJson(), null);
            }
        }

        // -------------------- GetOtpCode --------------------
        public ResultDto<string> GetOtpCode(string mobile, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var (isValid, normalized) = MobileValidator.Normalize(mobile);
                if (!isValid)
                    return FailData<string>("شماره موبایل معتبر نیست");

                var user = _context.Users.FirstOrDefault(p => p.Mobile == normalized);

                if (user == null)
                {
                    var newUser = new User
                    {
                        Mobile = normalized,
                        AuthToken = Helper.GetRandom(20, true, true, true, true),
                        Code = Helper.GetRandom(4, true, false, false, false),
                        CodeGenerateDate = DateTime.Now,
                        UserGuid = Guid.NewGuid(),
                        Status = UserStatusEnum.Inactive,
                        IsActive = false
                    };
                    _context.Users.Add(newUser);
                    _context.SaveChanges();

                    var smsResultNewUser = Helper.SendAuthSmsKavenegar(newUser.Mobile, newUser.Code);
                    if (!smsResultNewUser.IsSuccess)
                        return FailData<string>("خطا در ارسال پیامک: " + smsResultNewUser.Message);

                    transaction.Commit();
                    return OkData(newUser.AuthToken, "توکن ارسال گردید");
                }

                if (user.SuspendGenerateCode.HasValue && user.SuspendGenerateCode.Value >= DateTime.Now)
                    return FailData<string>("کاربر مسدود می باشد", 403);

                user.AuthToken = Helper.GetRandom(20, true, true, true, true);
                user.Code = Helper.GetRandom(4, true, false, false, false);
                user.CodeGenerateDate = DateTime.Now;
                _context.Users.Update(user);
                _context.SaveChanges();

                var smsResult = Helper.SendAuthSmsKavenegar(user.Mobile, user.Code);
                if (!smsResult.IsSuccess)
                    return FailData<string>("خطا در ارسال پیامک: " + smsResult.Message);

                transaction.Commit();
                return OkData(user.AuthToken, "توکن ارسال گردید");
            }
            catch (Exception ex)
            {
                return HandleException<string>(transaction, ex, mobile.ToJson(), null);
            }
        }

        // -------------------- LoginWithPassword --------------------
        public ResultDto<ResultUserLoginDto> LoginWithPassword(LoginUserWithPasswordDto loginUserDto, long currentUserId, string publicToken)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var (isValid, normalized) = MobileValidator.Normalize(loginUserDto.Mobile);
                if (!isValid)
                    return FailData<ResultUserLoginDto>("شماره موبایل معتبر نیست");

                loginUserDto.Mobile = normalized;

                if (string.IsNullOrWhiteSpace(loginUserDto.Password))
                    return FailData<ResultUserLoginDto>("رمز عبور را وارد نمایید");

                var user = _context.Users
                    .Where(p => p.Mobile == loginUserDto.Mobile && p.IsActive == true)
                    .Select(p => new
                    {
                        p.FirstName,
                        p.LastName,
                        p.Id,
                        Roles = p.Roles.Select(r => new RoleItemDto { Name = r.Name, Id = r.Id }).ToList(),
                        p.IsActive,
                        p.Mobile,
                        p.Email,
                        p.Password,
                        p.RequiredChangePassword
                    })
                    .FirstOrDefault();

                if (user == null)
                    return FailData<ResultUserLoginDto>("کاربر با این شماره در فروشگاه ثبت نام نکرده است");

                if (Hash.Get(loginUserDto.Password) != user.Password)
                    return FailData<ResultUserLoginDto>("رمز عبور اشتباه است");

                var userSession = new UserSession
                {
                    Platform = loginUserDto.requestDto.Platform,
                    Ip = loginUserDto.requestDto.Ip,
                    Browser = loginUserDto.requestDto.Browser,
                    Token = loginUserDto.requestDto.Token,
                    UserId = user.Id
                };
                _context.UserSessions.Add(userSession);
                _context.SaveChanges();

                _context.LoginLogs.Add(new LoginLog { IsActive = true, UserId = user.Id, Status = LoginLogStatus.Login });

                MergeBasket(publicToken, user.Id);

                _context.SaveChanges();
                transaction.Commit();

                return OkData(new ResultUserLoginDto
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = userSession.Token,
                    UserId = user.Id,
                    Mobile = user.Mobile,
                    Email = user.Email,
                    Roles = user.Roles,
                    RequiredChangePassword = user.RequiredChangePassword
                }, "اطلاعات ورود با موفقیت ارسال شد");
            }
            catch (Exception ex)
            {
                return HandleException<ResultUserLoginDto>(transaction, ex, loginUserDto.ToJson(), currentUserId);
            }
        }

        // -------------------- LoginWithUserId --------------------
        public ResultDto<ResultUserLoginDto> LoginWithUserId(LoginUserWithIdDto request, string publicToken)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = _context.Users
                    .Where(p => p.Id == request.userId && p.IsActive == true)
                    .Select(p => new
                    {
                        p.FirstName,
                        p.LastName,
                        p.Id,
                        Roles = p.Roles.Select(r => new RoleItemDto { Name = r.Name, Id = r.Id }).ToList(),
                        p.IsActive,
                        p.Mobile,
                        p.Email,
                        p.Password,
                        p.RequiredChangePassword
                    })
                    .FirstOrDefault();

                if (user == null)
                    return FailData<ResultUserLoginDto>("کاربر یافت نشد");

                var userSession = new UserSession
                {
                    Platform = request.requestDto.Platform,
                    Ip = request.requestDto.Ip,
                    Browser = request.requestDto.Browser,
                    Token = request.requestDto.Token,
                    UserId = user.Id
                };
                _context.UserSessions.Add(userSession);
                _context.SaveChanges();

                _context.LoginLogs.Add(new LoginLog { IsActive = true, UserId = user.Id, Status = LoginLogStatus.Login });

                MergeBasket(publicToken, user.Id);

                _context.SaveChanges();
                transaction.Commit();

                var template = user.Roles.Any(p => p.Name == "Cashier") ? "NotMenu" : "AdminTemplate";

                return OkData(new ResultUserLoginDto
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = userSession.Token,
                    UserId = user.Id,
                    Mobile = user.Mobile,
                    Email = user.Email,
                    Roles = user.Roles,
                    RequiredChangePassword = user.RequiredChangePassword
                }, "اطلاعات ورود با موفقیت ارسال شد");
            }
            catch (Exception ex)
            {
                return HandleException<ResultUserLoginDto>(transaction, ex, request.ToJson(), null);
            }
        }

        // -------------------- RegisterUser --------------------
        public ResultDto<ResultRegisterUserDto> RegisterUser(RequestRegisterUserDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var (isValid, normalized) = MobileValidator.Normalize(request.Mobile);
                if (!isValid)
                    return FailData<ResultRegisterUserDto>("شماره موبایل معتبر نیست");

                request.Mobile = normalized;

                if (_context.Users.Any(x => x.Mobile == request.Mobile))
                    return FailData<ResultRegisterUserDto>("این شماره موبایل قبلاً ثبت شده است", 409);

                var user = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Mobile = request.Mobile,
                    Password = Hash.Get(request.Password),
                    ReferalCode = Helper.GetRandom(6, true, false, false, false),
                    IsActive = true
                };
                _context.Users.Add(user);
                _context.SaveChanges();
                transaction.Commit();

                return OkData(new ResultRegisterUserDto { UserId = user.Id }, "ثبت نام با موفقیت انجام شد");
            }
            catch (Exception ex)
            {
                return HandleException<ResultRegisterUserDto>(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- RegisterNewUser --------------------
        public ResultDto<string> RegisterNewUser(RequestRegisterNewUserDto request)
        {
            var (isValid, normalized) = MobileValidator.Normalize(request.Mobile);
            if (!isValid)
                return FailData<string>("شماره موبایل معتبر نیست");

            request.Mobile = normalized;

            var user = _context.Users.FirstOrDefault(p => p.Mobile == request.Mobile);

            if (user != null && user.Status == UserStatusEnum.Active)
                return FailData<string>("این شماره موبایل قبلا ثبت نام کرده است", 201);

            if (user != null && user.Status == UserStatusEnum.Inactive)
                return FailData<string>("کاربر غیر فعال می باشد", 201);

            long? referrerUserId = null;

            if (!string.IsNullOrWhiteSpace(request.ReferalCode))
            {
                if (request.ReferalCode.Length < 4)
                    return FailData<string>("کد معرف باید حداقل ۴ کاراکتر باشد");

                var referrer = _context.Users.FirstOrDefault(x => x.ReferalCode == request.ReferalCode);
                if (referrer == null)
                    return FailData<string>("کد معرف وارد شده معتبر نیست");

                referrerUserId = referrer.Id;
            }

            var newUser = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Mobile = request.Mobile,
                Password = Hash.Get(Helper.GetRandom(10)),
                ReferalCode = Helper.GetRandom(6, true, false, false, false),
                ReferalUserId = referrerUserId,
                IsActive = false,
                Status = UserStatusEnum.Inactive,
                UserRegisterToken = Helper.GetRandom(50),
                Code = Helper.GetRandom(4, true, false, false, false)
            };
            _context.Users.Add(newUser);
            _context.SaveChanges();

            return OkData(newUser.UserRegisterToken, "توکن ثبت گردید");
        }

        // -------------------- AcceptUserAuthCode --------------------
        public ResultDto<long> AcceptUserAuthCode(RequestAcceptUserAuthCodeDto request)
        {
            var user = _context.Users.FirstOrDefault(p =>
                p.Status == UserStatusEnum.Inactive && p.UserRegisterToken == request.Token);

            if (user == null)
                return FailData<long>("این کاربر قبلا ثبت نام کرده است", 201);

            if (user.Code != request.Code)
                return FailData<long>("کد وارد شده صحیح نمی باشد", 201);

            user.Status = UserStatusEnum.Active;
            user.IsActive = true;
            _context.Users.Update(user);
            _context.SaveChanges();

            return OkData(user.Id, "ورود موفق");
        }

        // -------------------- AcceptLoginAuthCodeBtn --------------------
        public ResultDto<long> AcceptLoginAuthCodeBtn(RequestAcceptUserAuthCodeDto request)
        {
            var user = _context.Users.FirstOrDefault(p => p.UserRegisterToken == request.Token);

            if (user == null)
                return FailData<long>("توکن اشتباه", 201);

            if (user.Code != request.Code)
                return FailData<long>("کد وارد شده صحیح نمی باشد", 201);

            return OkData(user.Id, "ورود موفق");
        }

        // -------------------- GetLoginOTPCode --------------------
        public ResultDto<string> GetLoginOTPCode(RequestGetLoginOTOCodeDto request)
        {
            var (isValid, normalized) = MobileValidator.Normalize(request.Mobile);
            if (!isValid)
                return FailData<string>("شماره موبایل معتبر نیست");

            request.Mobile = normalized;

            var user = _context.Users.FirstOrDefault(p => p.Mobile == request.Mobile);

            if (user == null)
                return FailData<string>("این شماره موبایل قبلا ثبت نام نکرده است", 201);

            if (user.Status == UserStatusEnum.Inactive)
                return FailData<string>("کاربر غیر فعال می باشد", 201);

            user.UserRegisterToken = Helper.GetRandom(50);
            user.Code = Helper.GetRandom(4, true, false, false, false);
            _context.Users.Update(user);
            _context.SaveChanges();

            return OkData(user.UserRegisterToken, "توکن ثبت گردید");
        }

        // -------------------- UserUploadList --------------------
        public ResultDto UserUploadList(List<RequestExcelUserListDto> request, long userCategoryId, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var validatedRequest = request
                    .Select(item =>
                    {
                        var (isValid, normalized) = MobileValidator.Normalize(item.Mobile);
                        return new { Item = item, IsValid = isValid, Normalized = normalized };
                    })
                    .Where(x => x.IsValid)
                    .Select(x =>
                    {
                        x.Item.Mobile = x.Normalized;
                        return x.Item;
                    })
                    .ToList();

                var newUserList = validatedRequest
                    .GroupJoin(_context.Users, req => req.Mobile, user => user.Mobile,
                        (req, users) => new { RequestUser = req, Users = users })
                    .SelectMany(x => x.Users.DefaultIfEmpty(),
                        (x, user) => new { RequestUser = x.RequestUser, User = user })
                    .Skip(1)
                    .Where(x => x.User == null && x.RequestUser.Mobile != "0")
                    .Select(x => new User
                    {
                        FirstName = x.RequestUser.FirstName,
                        LastName = x.RequestUser.LastName,
                        Password = Hash.Get(x.RequestUser.Mobile),
                        Mobile = x.RequestUser.Mobile,
                        IsActive = true,
                        RequiredChangePassword = true
                    })
                    .DistinctBy(x => x.Mobile);

                _context.Users.AddRange(newUserList);

                var distinctUserList = validatedRequest
                    .Join(_context.Users, req => req.Mobile, user => user.Mobile, (req, user) => req)
                    .DistinctBy(x => x.Mobile)
                    .ToList();

                foreach (var item in distinctUserList)
                {
                    if (item.Mobile == "0") continue;
                    var userItem = _context.Users.FirstOrDefault(p => p.Mobile == item.Mobile);
                    if (userItem != null
                        && (!string.Equals(userItem.FirstName, item.FirstName, StringComparison.OrdinalIgnoreCase)
                            || !string.Equals(userItem.LastName, item.LastName, StringComparison.OrdinalIgnoreCase))
                        && (!string.IsNullOrWhiteSpace(item.FirstName) || !string.IsNullOrWhiteSpace(item.LastName)))
                    {
                        userItem.FirstName = item.FirstName;
                        userItem.LastName = item.LastName;
                        _context.Users.Update(userItem);
                    }
                }

                _context.SaveChanges();
                transaction.Commit();
                return Ok("اطلاعات ذخیره گردید");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- ChangeStatusUser --------------------
        public ResultDto ChangeStatusUser(long userId, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null)
                    return Fail("کاربر یافت نشد");

                user.IsActive = !user.IsActive;
                _context.SaveChanges();
                transaction.Commit();

                string status = user.IsActive ? "فعال" : "غیر فعال";
                return Ok($"کاربر با موفقیت {status} گردید");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, userId.ToJson(), currentUserId);
            }
        }

        // -------------------- ChangeUserPassword --------------------
        public ResultDto ChangeUserPassword(long userId, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null)
                    return Fail("کاربر یافت نشد");

                user.Password = Hash.Get(user.Mobile);
                user.RequiredChangePassword = true;
                _context.Users.Update(user);
                _context.SaveChanges();
                transaction.Commit();
                return Ok("رمز کاربر با موفقیت تغییر کرد");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, userId.ToJson(), currentUserId);
            }
        }

        // -------------------- Logout --------------------
        public ResultDto Logout(string token, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var userSession = _context.UserSessions.FirstOrDefault(p => p.Token == token);
                if (userSession == null)
                    return Fail("توکن یافت نشد");

                userSession.IsDeleted = true;
                userSession.DeletedAt = DateTime.Now;
                _context.UserSessions.Update(userSession);
                _context.SaveChanges();
                transaction.Commit();
                return Ok("توکن کاربر غیر فعال گردید");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, token.ToJson(), currentUserId);
            }
        }

        // -------------------- LogoutUser --------------------
        public ResultDto LogoutUser(long userId, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                _context.LoginLogs.Add(new LoginLog { IsActive = true, UserId = userId, Status = LoginLogStatus.Logout });
                _context.SaveChanges();
                transaction.Commit();
                return Ok("کاربر با موفقیت خارج شد");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, userId.ToJson(), currentUserId);
            }
        }

        // -------------------- UpdateUserRole --------------------
        public ResultDto UpdateUserRole(UpdateUserRoleDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = _context.Users
                    .Where(s => s.Id == request.UserId)
                    .Include(p => p.Roles)
                    .FirstOrDefault();

                if (user == null)
                    return Fail("کاربر یافت نشد");

                if (user.Roles != null)
                {
                    foreach (var role in user.Roles.ToList())
                        user.Roles.Remove(role);
                    _context.SaveChanges();
                }

                var newRoles = _context.Roles.Where(p => request.RoleIds.Contains(p.Id)).ToList();
                foreach (var role in newRoles)
                    user.Roles.Add(role);

                _context.SaveChanges();
                transaction.Commit();
                return Ok("اطلاعات نقش های کاربر به روز گردید");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- MergeBasket --------------------
        private void MergeBasket(string publicToken, long userId)
        {
            if (string.IsNullOrWhiteSpace(publicToken))
                return;

            var guestBasket = _context.Baskets
                .Include(b => b.BasketItems)
                .FirstOrDefault(b => b.Token == publicToken && b.BasketStatus == BasketStatus.Created && b.UserId == null);

            if (guestBasket == null)
                return;

            var userBasket = _context.Baskets
                .Include(b => b.BasketItems)
                .FirstOrDefault(b => b.UserId == userId && b.BasketStatus == BasketStatus.Created);

            var address = _context.UserAddresses.FirstOrDefault(p => p.UserId == userId);

            if (userBasket == null)
            {
                // No existing basket for this user - convert the guest basket into their basket,
                // but strip the token so it can never again be reached via publicToken lookups.
                guestBasket.UserId = userId;
                guestBasket.Token = null;
                if (address != null)
                    guestBasket.UserAddressId = address.Id;

                _context.Baskets.Update(guestBasket);
                _context.SaveChanges();
                return;
            }

            // User already has an active basket - merge guest items into it, then hard-delete the guest basket entirely
            var guestBasketId = guestBasket.Id;
            var itemIdsToHardDelete = new List<long>();

            foreach (var guestItem in guestBasket.BasketItems.ToList())
            {
                var product = _context.Products.FirstOrDefault(p => p.Id == guestItem.ProductId);
                var existingItem = userBasket.BasketItems.FirstOrDefault(i => i.ProductId == guestItem.ProductId);

                if (existingItem != null)
                {
                    var mergedCount = existingItem.Count + guestItem.Count;

                    if (product != null)
                    {
                        if (product.MaxBuyLimit.HasValue)
                            mergedCount = Math.Min(mergedCount, product.MaxBuyLimit.Value);

                        mergedCount = Math.Min(mergedCount, product.Quantity);
                    }

                    existingItem.Count = mergedCount;
                    _context.BasketItems.Update(existingItem);
                    itemIdsToHardDelete.Add(guestItem.Id);
                }
                else
                {
                    var count = guestItem.Count;

                    if (product != null)
                    {
                        if (product.MaxBuyLimit.HasValue)
                            count = Math.Min(count, product.MaxBuyLimit.Value);

                        count = Math.Min(count, product.Quantity);
                    }

                    if (count <= 0)
                    {
                        itemIdsToHardDelete.Add(guestItem.Id);
                        continue;
                    }

                    guestItem.BasketId = userBasket.Id;
                    guestItem.Count = count;
                    _context.BasketItems.Update(guestItem);
                }
            }

            if (userBasket.UserAddressId == null && address != null)
                userBasket.UserAddressId = address.Id;

            _context.Baskets.Update(userBasket);
            _context.SaveChanges();

            // Hard-delete leftover guest basket items and the guest basket itself,
            // bypassing SaveChanges (and any soft-delete interceptor) with a direct DELETE.
            if (itemIdsToHardDelete.Count > 0)
            {
                _context.BasketItems
                    .Where(bi => itemIdsToHardDelete.Contains(bi.Id))
                    .ExecuteDelete();
            }

            _context.Baskets
                .Where(b => b.Id == guestBasketId)
                .ExecuteDelete();
        }

        // ==================== Private Helpers ====================
        private ResultDto Ok(string message) =>
            new ResultDto { IsSuccess = true, Message = message, Status = 200 };

        private ResultDto Fail(string message, int status = 400) =>
            new ResultDto { IsSuccess = false, Message = message, Status = status };

        private ResultDto<T> OkData<T>(T data, string message) =>
            new ResultDto<T> { Data = data, IsSuccess = true, Message = message, Status = 200 };

        private ResultDto<T> FailData<T>(string message, int status = 400) =>
            new ResultDto<T> { Data = default, IsSuccess = false, Message = message, Status = status };

        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
                entry.State = EntityState.Detached;
        }

        // Parses "yyyy-MM-dd HH:mm:ss" strings into DateTime?. Empty/null input is treated as "no date" (valid).
        private static bool TryParseDate(string? value, out DateTime? result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = null;
                return true;
            }

            if (DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                result = parsed;
                return true;
            }

            result = null;
            return false;
        }

        private ResultDto HandleException(IDbContextTransaction transaction, Exception ex, string requestParam, long? userId)
        {
            transaction.Rollback();
            DetachAll();
            _context.HandleExceptions.Add(new HandleException
            {
                Message = ex.InnerException?.Message,
                RequestParameter = requestParam,
                StackTrace = ex.StackTrace,
                UserId = userId,
            });
            _context.SaveChanges();
            return new ResultDto { IsSuccess = false, Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد", Status = 500 };
        }

        private ResultDto<T> HandleException<T>(IDbContextTransaction transaction, Exception ex, string requestParam, long? userId)
        {
            transaction.Rollback();
            DetachAll();
            _context.HandleExceptions.Add(new HandleException
            {
                Message = ex.InnerException?.Message,
                RequestParameter = requestParam,
                StackTrace = ex.StackTrace,
                UserId = userId,
            });
            _context.SaveChanges();
            return new ResultDto<T> { IsSuccess = false, Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد", Status = 500 };
        }
    }

    // ==================== DTOs ====================
    public class RequestUpdateUserInfoDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public bool NewsLetter { get; set; }
        public string? ReferalCode { get; set; }
        public string? ReferalUserIdCode { get; set; }
        // Date string in "yyyy-MM-dd HH:mm:ss" format; empty/null leaves the existing value untouched.
        public string? Birthday { get; set; }
        public string? Job { get; set; }
        public Gender? Gender { get; set; }
    }

    public class RequestChangePasswordByCurrentUserDto
    {
        public string Password { get; set; }
        public string RepeatPassword { get; set; }
    }

    public class RequestChangePasswordByUserDto
    {
        public long UserId { get; set; }
        public string Password { get; set; }
        public string RepeatPassword { get; set; }
    }

    public class RequestCheckOtpCodeDto
    {
        public string AuthToken { get; set; }
        public string Code { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public RequestDto? requestDto { get; set; }
    }

    public class RoleItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class ResultUserLoginDto
    {
        public long UserId { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public List<RoleItemDto>? Roles { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Token { get; set; }
        public bool RequiredChangePassword { get; set; }
        public bool IsProfileIncomplete { get; set; }
    }

    public class LoginUserWithPasswordDto
    {
        public string Mobile { get; set; }
        public string Password { get; set; }
        public RequestDto requestDto { get; set; }
    }

    public class RequestLoginUserWithPasswordDto
    {
        public string Mobile { get; set; }
        public string Password { get; set; }
    }

    public class LoginUserWithIdDto
    {
        public long userId { get; set; }
        public RequestDto requestDto { get; set; }
    }

    public class RequestDto
    {
        public string Browser { get; set; }
        public string Ip { get; set; }
        public string Platform { get; set; }
        public string Token { get; set; }
    }

    public class ResultRegisterUserDto
    {
        public long UserId { get; set; }
    }

    public class RequestRegisterUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Mobile { get; set; }
        public string MeliCode { get; set; }
        public string Password { get; set; }
        public List<RolesInRegisterUserDto> Roles { get; set; }
    }

    public class RequestRegisterNewUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Mobile { get; set; }
        public string? ReferalCode { get; set; }
    }

    public class RequestGetLoginOTOCodeDto
    {
        public string Mobile { get; set; }
    }

    public class RequestAcceptUserAuthCodeDto
    {
        public string Code { get; set; }
        public string Token { get; set; }
    }

    public class RolesInRegisterUserDto
    {
        public long Id { get; set; }
    }

    public class RequestExcelUserListDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Mobile { get; set; }
    }

    public class RequestChangeUserPasswordDto
    {
        public long UserId { get; set; }
    }

    public class UpdateUserRoleDto
    {
        public long UserId { get; set; }
        public List<long> RoleIds { get; set; }
    }

    public class RequestAddUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Mobile { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public bool IsActive { get; set; } = true;
        public List<long>? RoleIds { get; set; }
        // Date string in "yyyy-MM-dd HH:mm:ss" format; empty/null means no birthday.
        public string? Birthday { get; set; }
        public Gender? Gender { get; set; }
        public string? Job { get; set; }
    }

    public class RequestEditUserDto
    {
        public long UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public List<long>? RoleIds { get; set; }
        // Date string in "yyyy-MM-dd HH:mm:ss" format; empty/null leaves the existing value untouched.
        public string? Birthday { get; set; }
        public Gender? Gender { get; set; }
        public string? Job { get; set; }
    }
}