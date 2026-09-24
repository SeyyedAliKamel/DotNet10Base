using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AdditionalInformations.Commands
{
    // ==================== Interface ====================
    public interface ISettingCUDService
    {
        ResultDto<SettingResultDto> AddSetting(RequestAddSettingDto request, long currentUserId);
        ResultDto<SettingResultDto> EditSetting(RequestEditSettingDto request, long currentUserId);
        ResultDto RemoveSetting(long id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class SettingCUDService : ISettingCUDService
    {
        private readonly IDataBaseContext _context;

        public SettingCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Add --------------------
        public ResultDto<SettingResultDto> AddSetting(RequestAddSettingDto request, long currentUserId)
        {
            var setting = new Setting
            {
                Title = request.Title,
                Key = request.Key,
                Value = request.Value
            };

            _context.SetUserId(currentUserId);
            _context.Settings.Add(setting);
            _context.SaveChanges();

            return new ResultDto<SettingResultDto>
            {
                Data = MapToResult(setting),
                IsSuccess = true,
                Message = "تنظیمات با موفقیت ثبت گردید",
                Status = 200
            };
        }

        // -------------------- Edit --------------------
        public ResultDto<SettingResultDto> EditSetting(RequestEditSettingDto request, long currentUserId)
        {
            var setting = _context.Settings.FirstOrDefault(s => s.Id == request.Id);

            if (setting == null)
                return new ResultDto<SettingResultDto> { IsSuccess = false, Message = "تنظیمات یافت نشد", Status = 404 };

            setting.Title = request.Title;
            setting.Value = request.Value;

            _context.SetUserId(currentUserId);
            _context.SaveChanges();

            return new ResultDto<SettingResultDto>
            {
                Data = MapToResult(setting),
                IsSuccess = true,
                Message = "تنظیمات با موفقیت به روز گردید",
                Status = 200
            };
        }

        // -------------------- Remove --------------------
        public ResultDto RemoveSetting(long id, long currentUserId)
        {
            var setting = _context.Settings.FirstOrDefault(s => s.Id == id);

            if (setting == null)
                return new ResultDto { IsSuccess = false, Message = "تنظیمات یافت نشد", Status = 404 };

            _context.SetUserId(currentUserId);
            _context.Settings.Remove(setting);
            _context.SaveChanges();

            return new ResultDto { IsSuccess = true, Message = "تنظیمات با موفقیت حذف گردید", Status = 200 };
        }

        // ==================== Helpers ====================
        private static SettingResultDto MapToResult(Setting setting) => new()
        {
            Id = setting.Id,
            Title = setting.Title,
            Key = setting.Key,
            Value = setting.Value
        };
    }

    // ==================== DTOs ====================
    public class RequestAddSettingDto
    {
        public string Title { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class RequestEditSettingDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Value { get; set; }
    }

    public class SettingResultDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}