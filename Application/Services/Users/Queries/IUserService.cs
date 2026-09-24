// IUserService.cs
using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace Application.Services.Users.Queries
{
    // ==================== Interface ====================
    public interface IUserService
    {

        ResultDto<List<GetUsersDto>> GetUserList(RequestGetUserDto request, long CurrentUserId);
        ResultDto<ResultGetCurrentUserDto> GetCurrentUser(RequestCurrentUserDto request, long currentUserId);
        ResultDto<ResultGetCurrentUserDetailsDto> GetCurrentUserDetails(long currentUserId);
        ResultDto<ResultUserItemDto> GetUserItem(long id, long currentUserId);
        ResultDto<long> GetUserForLoginInstead(long id, long currentUserId);
        ResultDto<List<string>> GetUserRolePermissions(long userId, long currentUserId);
        ResultDto<List<GetUsersDto>> GetUsers(RequestGetUserDto request, long currentUserId);
        ResultDto<List<GetLimitUsersDto>> GetLimitUsers(RequestGetLimitUserDto request, long currentUserId);
        ResultDto<string> GetTemplateForUser(long userId, long currentUserId);
        ResultDto<ResultUserInfoDto> GetUserInfo(long userId);
        ResultDto HasUserPermission(long userId, string action, string controller);
        ResultDto HasPermission();
    }

    // ==================== Implementation ====================
    public class UserService : IUserService
    {
        private readonly IDataBaseContext _context;

        // Kept in sync with UserCUDService.DateFormat so Birthday round-trips through edit forms.
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        public UserService(IDataBaseContext context)
        {
            _context = context;
        }

        public ResultDto<List<GetUsersDto>> GetUserList(RequestGetUserDto request, long CurrentUserId)
        {
            try
            {
                var users = _context.Users.Include(p => p.Roles).AsQueryable();

                if (request.Role != null)
                {
                    users = users.Where(user => user.Roles
                    .Any(ur => ur.Id == request.Role));
                }

                users = users.ApplySortingAndFiltering(request.Sort, request.Filter);
                int rowsCount = 0;


                if (request.Page == 0)
                {
                    var userList = users.Select(p => new GetUsersDto
                    {
                        Id = p.Id,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        IsActive = p.IsActive,
                        Mobile = p.Mobile,

                    }).ToList();

                    return new ResultDto<List<GetUsersDto>>
                    {
                        Data = userList,
                        IsSuccess = true,
                        Message = "User information list",
                        Status = 200
                    };
                }
                else
                {

                    var userList = users.Select(p => new GetUsersDto
                    {
                        Id = p.Id,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        IsActive = p.IsActive,
                        Mobile = p.Mobile,
                    }).ToPaged(request.Page, request.PageSize, out rowsCount).ToList();

                    return new ResultDto<List<GetUsersDto>>
                    {
                        Data = userList,
                        Result = new ResultListDto()
                        {
                            Current = request.Page,
                            Count = rowsCount
                        },
                        IsSuccess = true,
                        Message = "User information list",
                        Status = 200
                    };
                }
            }
            catch (Exception ex)
            {

                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                var item = new HandleException
                {
                    Message = ex.InnerException?.Message.ToString(),
                    RequestParameter = request.ToJson(),
                    StackTrace = ex.StackTrace,
                    UserId = CurrentUserId,
                };
                _context.HandleExceptions.Add(item);

                _context.SaveChanges();


                return new ResultDto<List<GetUsersDto>>
                {
                    IsSuccess = true,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,


                };

            }


        }

        public ResultDto<ResultGetCurrentUserDto> GetCurrentUser(RequestCurrentUserDto request, long currentUserId)
        {
            try
            {
                var user = _context.UserSessions
                    .Where(p => p.Token == request.Token && p.IsDeleted == false)
                    .Where(p => p.Token == request.Token)
                    .Include(p => p.User)
                    .Select(p => new ResultGetCurrentUserDto
                    {
                        Id = p.User.Id,
                        FirstName = p.User.FirstName,
                        LastName = p.User.LastName,
                        Mobile = p.User.Mobile,
                        LastActivity = p.LastActivity
                    }).FirstOrDefault();

                if (user == null)
                    return new ResultDto<ResultGetCurrentUserDto> { Data = null, IsSuccess = false, Status = 400, Message = string.Empty };

                user.LastActivity = DateTime.Now;
                _context.SaveChanges();

                return new ResultDto<ResultGetCurrentUserDto>
                {
                    Data = new ResultGetCurrentUserDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        LastActivity = user.LastActivity,
                        Mobile = user.Mobile,
                    },
                    IsSuccess = true,
                    Message = "نام کاربر انجام شد"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultGetCurrentUserDto>(ex, request.ToJson(), currentUserId);
            }
        }

        public ResultDto<ResultGetCurrentUserDetailsDto> GetCurrentUserDetails(long currentUserId)
        {
            try
            {
                var user = _context.Users
                    .Where(u => u.Id == currentUserId)
                    .Select(u => new ResultGetCurrentUserDetailsDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Mobile = u.Mobile,
                        Email = u.Email,
                        Password = u.Password
                    }).FirstOrDefault();

                if (user == null)
                    return new ResultDto<ResultGetCurrentUserDetailsDto> { Data = null, IsSuccess = false, Status = 201, Message = "کاربر مورد نظر یافت نشد" };

                return new ResultDto<ResultGetCurrentUserDetailsDto> { Data = user, IsSuccess = true, Message = "اطلاعات کاربر با موفقیت ثبت گردید" };
            }
            catch (Exception ex)
            {
                return HandleException<ResultGetCurrentUserDetailsDto>(ex, currentUserId.ToString(), currentUserId);
            }
        }

        // -------------------- GetUserItem --------------------
        public ResultDto<ResultUserItemDto> GetUserItem(long id, long currentUserId)
        {
            try
            {
                // Add .Include for City and State
                var user = _context.Users
                    .Include(u => u.City)
                    .Include(u => u.State)
                    .Where(x => x.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.FirstName,
                        p.LastName,
                        p.Mobile,
                        p.Email,
                        RoleIds = p.Roles.Select(r => r.Id).ToList(),
                        p.Birthday,
                        p.Gender,
                        p.Job,
                        p.Blend,
                        CityName = p.City != null ? p.City.Name : null,
                        StateName = p.State != null ? p.State.Name : null
                    }).FirstOrDefault();

                if (user == null)
                    return new ResultDto<ResultUserItemDto> { Data = null, IsSuccess = false, Message = "کاربر مورد نظر یافت نشد", Status = 404 };

                var dto = new ResultUserItemDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Mobile = user.Mobile,
                    Email = user.Email,
                    RoleIds = user.RoleIds,
                    Birthday = user.Birthday.HasValue ? user.Birthday.Value.ToString(DateFormat) : null,
                    Gender = user.Gender.ToString(),
                    Job = user.Job,
                    Blend = user.Blend.ToString(),
                    CityName = user.CityName,
                    StateName = user.StateName
                };

                return new ResultDto<ResultUserItemDto> { Data = dto, IsSuccess = true, Message = "...", Status = 200 };
            }
            catch (Exception ex)
            {
                return HandleException<ResultUserItemDto>(ex, id.ToJson(), currentUserId);
            }
        }

        public ResultDto<long> GetUserForLoginInstead(long id, long currentUserId)
        {
            var user = _context.Users.FirstOrDefault(p => p.Id == id);
            if (user == null)
                return new ResultDto<long> { Status = 201, IsSuccess = false, Message = "Invalid token" };

            return new ResultDto<long> { Data = user.Id, Status = 200, IsSuccess = true, Message = "Login successful" };
        }

        // -------------------- GetUserRolePermission --------------------
        public ResultDto<List<string>> GetUserRolePermissions(long userId, long currentUserId)
        {
            try
            {
                var rolePermissions = _context.Users
                    .Where(x => x.Id == userId)
                    .SelectMany(x => x.Roles.SelectMany(r => r.Permissions))
                    .Select(x => x.FrontEndName)
                    .Distinct()
                    .ToList();

                return new ResultDto<List<string>> { Data = rolePermissions, IsSuccess = true, Message = "لیست دسترسی های کاربر", Status = 200 };
            }
            catch (Exception ex)
            {
                return HandleException<List<string>>(ex, userId.ToJson(), currentUserId);
            }
        }

        // -------------------- GetUsers --------------------
        public ResultDto<List<GetUsersDto>> GetUsers(RequestGetUserDto request, long currentUserId)
        {
            try
            {
                var users = _context.Users.Include(p => p.Roles).AsQueryable();

                users = users.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (request.Role != null)
                    users = users.Where(u => u.Roles.Any(r => r.Id == request.Role));

                if (request.Page == 0)
                {
                    var list = users.Select(p => new GetUsersDto
                    {
                        Id = p.Id,
                        Mobile = p.Mobile,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        IsActive = p.IsActive
                    }).ToList();

                    return new ResultDto<List<GetUsersDto>> { Data = list, IsSuccess = true, Message = "لیست اطلاعات کاربران", Status = 200 };
                }
                else
                {
                    int rowsCount = 0;
                    var list = users.Select(p => new GetUsersDto
                    {
                        Id = p.Id,
                        Mobile = p.Mobile,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        IsActive = p.IsActive
                    }).ToPaged(request.Page, request.PageSize, out rowsCount).ToList();

                    return new ResultDto<List<GetUsersDto>>
                    {
                        Data = list,
                        Result = new ResultListDto { Current = request.Page, Count = rowsCount },
                        IsSuccess = true,
                        Message = "لیست اطلاعات کاربران",
                        Status = 200
                    };
                }
            }
            catch (Exception ex)
            {
                return HandleException<List<GetUsersDto>>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- GetLimitUsers --------------------
        public ResultDto<List<GetLimitUsersDto>> GetLimitUsers(RequestGetLimitUserDto request, long currentUserId)
        {
            try
            {
                var users = _context.Users.AsQueryable();

                users = users.ApplySortingAndFiltering(request.Sort, request.Filter);

                int rowsCount = 0;
                var list = users
                    .OrderByDescending(s => s.CreatedAt)
                    .ToPaged(request.Page, request.PageSize, out rowsCount)
                    .Select(p => new GetLimitUsersDto { FirstName = p.FirstName, LastName = p.LastName, IsActive = p.IsActive })
                    .ToList();

                return new ResultDto<List<GetLimitUsersDto>> { Data = list, IsSuccess = true, Message = "لیست کاربران", Status = 200 };
            }
            catch (Exception ex)
            {
                return HandleException<List<GetLimitUsersDto>>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- GetTemplate --------------------
        public ResultDto<string> GetTemplateForUser(long userId, long currentUserId)
        {
            try
            {
                var user = _context.Users
                    .Where(p => p.Id == userId)
                    .Select(p => new { p.FirstName, p.LastName, p.Id, p.Roles })
                    .FirstOrDefault();

                var template = user.Roles.Any(p => p.Name == "Cashier") ? "NotMenu" : "AdminTemplate";

                return new ResultDto<string> { Data = template, IsSuccess = true, Message = "قالب ارسال گردید", Status = 200 };
            }
            catch (Exception ex)
            {
                return HandleException<string>(ex, userId.ToJson(), currentUserId);
            }
        }

        // -------------------- GetUserInfo --------------------
        public ResultDto<ResultUserInfoDto> GetUserInfo(long userId)
        {
            try
            {
                var user = _context.Users
                    .Include(u => u.City)
                    .Include(u => u.State)
                    .Where(p => p.Id == userId)
                    .Select(p => new
                    {
                        p.FirstName,
                        p.LastName,
                        p.Id,
                        p.Mobile,
                        p.Email,
                        p.NewsLetter,
                        p.ReferalCode,
                        p.RequiredChangePassword,
                        p.Birthday,
                        p.Gender,
                        p.Job,
                        p.Blend,
                        CityName = p.City != null ? p.City.Name : null,
                        StateName = p.State != null ? p.State.Name : null,
                        Roles = p.Roles.Select(x => new { x.Id, x.Name }).ToList()
                    }).FirstOrDefault();

                if (user == null)
                {
                    return new ResultDto<ResultUserInfoDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Message = "کاربر مورد نظر یافت نشد",
                        Status = 404
                    };
                }

                var template = user.Roles.Any(p => p.Name == "Cashier") ? "NotMenu" : "AdminTemplate";

                return new ResultDto<ResultUserInfoDto>
                {
                    Data = new ResultUserInfoDto
                    {
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Mobile = user.Mobile,
                        Email = user.Email,
                        NewsLetter = user.NewsLetter,
                        ReferalCode = user.ReferalCode,
                        RequiredChangePassword = user.RequiredChangePassword,
                        Template = template,
                        Birthday = user.Birthday.HasValue ? user.Birthday.Value.ToString(DateFormat) : null,
                        Gender = user.Gender.ToString(),
                        Job = user.Job,
                        Blend = user.Blend.ToString(),
                        CityName = user.CityName,
                        StateName = user.StateName,
                    },
                    IsSuccess = true,
                    Message = "اطلاعات کاربر با موفقیت ارسال گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultUserInfoDto>(ex, userId.ToString(), userId);
            }
        }

        // -------------------- HasUserPermission --------------------
        public ResultDto HasUserPermission(long userId, string action, string controller)
        {
            var permission = _context.Permissions
                .Where(p => p.Action == action && p.Controller == controller)
                .FirstOrDefault();

            if (permission == null)
                return new ResultDto { IsSuccess = false, Message = "دسترسی یافت نشد", Status = 402 };

            var hasPermission = _context.Users
                .Where(x => x.Id == userId)
                .Any(x => x.Roles.Any(r => r.Permissions.Select(rp => rp.Id).Contains(permission.Id)));

            return new ResultDto
            {
                IsSuccess = hasPermission,
                Message = "کاربر دسترسی دارد",
                Status = hasPermission ? 200 : 402
            };
        }

        // --------------------- HasPermission ----------------------
        public ResultDto HasPermission()
        {
            return new ResultDto
            {
                IsSuccess = true,
                Message = "Permission bypassed",
                Status = 200
            };

            // Existing code...
        }

        // ==================== Private Helpers ====================
        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
                entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }

        private ResultDto<T> HandleException<T>(Exception ex, string requestParam, long? userId)
        {
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
    public class RequestCurrentUserDto { public string? Token { get; set; } }

    public class ResultGetCurrentUserDto
    {
        public long Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Mobile { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class ResultGetCurrentUserDetailsDto
    {
        public long Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Mobile { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class ResultUserItemDto
    {
        public long Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public int? ChildCount { get; set; }
        public List<long>? RoleIds { get; set; }
        public long? UserJobId { get; set; }
        public long? Rate { get; set; }
        // "yyyy-MM-dd HH:mm:ss" string, matches the format accepted by AddUser/EditUser.
        public string? Birthday { get; set; }
        public string? Gender { get; set; } // Assuming you want the string representation
        public string? Job { get; set; }
        public string? Blend { get; set; }  // Assuming string representation
        public string? CityName { get; set; }
        public string? StateName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class GetUsersDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Mobile { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
    }

    public class RequestGetUserDto
    {
        public long? Role { get; set; }
        public string? Sort { get; set; }
        public string? Filter { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; } = 20;
    }

    public class GetLimitUsersDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MeliCode { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestGetLimitUserDto
    {
        public string? Sort { get; set; }
        public string? Filter { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; } = 20;
    }

    public class ResultUserInfoDto
    {
        public long UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public bool NewsLetter { get; set; }
        public string ReferalCode { get; set; }
        public bool RequiredChangePassword { get; set; }
        public string? Template { get; set; }
        // "yyyy-MM-dd HH:mm:ss" string, matches the format accepted by UpdateUserInfo.
        public string? Birthday { get; set; }
        public string? Gender { get; set; }
        public string? Job { get; set; }
        public string? Blend { get; set; }
        public string? CityName { get; set; }
        public string? StateName { get; set; }
    }

    public class UserRoleDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}