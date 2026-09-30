using Common.Dto;
using Kavenegar;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Common
{
    public static class Helper
    {
        public static string ConvertTitleToSlug(string title)
        {
            string slug = title.ToLower().ToEnglish().ToPersian(); // Convert to lowercase
                                                                   //slug = Regex.Replace(slug, @"[^0-9\s-]", "");
                                                                   //  slug = Regex.Replace(slug, @"[^\u0600-\u06FFa-z0-9\s-]", ""); // Remove special characters
            slug = Regex.Replace(slug, @"\s+", " ").Trim(); // Replace multiple whitespaces with a single space
            slug = slug.Replace(" ", "-"); // Replace spaces with hyphens
            slug = Regex.Replace(slug, @"-+", "-").Trim(); // Replace multiple hyphens with a single hyphen
            return slug;
        }

        public static bool IsValidEmail(this string email)
        {
            try
            {
                var regex = new Regex(@"^(([^<>()\[\]\\.,;:\s@\""]+(\.[^<>()\[\]\\.,;:\s@\""]+)*)|(\"".+\""))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$");
                return regex.Match(email).Success;
            }
            catch
            {
                return false;
            }
        }

        public static string? GetValidEmail(this string? email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return null;
                }
                email = email.ToLower();
                if (email.EndsWith("@gmail.com"))
                {
                    var username = email.Split("@gmail.com").First();
                    email = username.Replace(".", "").Split("+").First() + "@gmail.com";
                }
                string emailPattern = @"^(?:[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-zA-Z0-9-]*[a-zA-Z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])$";
                string emailPattern2 = @"^(?:[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:(?:\\[""\\]|[^\r\n""\\])*)"")@(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[A-Za-z]{2,}|(?:\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?\d?\d)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?\d?\d)\]))$";
                if (Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase))
                {
                    return email;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsValidIp(this string ip)
        {
            try
            {
                var parts = ip.Split('.');
                return parts.Length == 4 && parts.All(r => int.TryParse(r, out var y) && y is < 256 and >= 0);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string ToEnglish(this string input)
        {
            var englishNumbers = new Dictionary<string, string>()
        {
            { "۰", "0" }, { "۱", "1" }, { "۲", "2" }, { "۳", "3" }, { "۴", "4" }, { "۵", "5" }, { "۶", "6" }, { "۷", "7" }, { "۸", "8" }, { "۹", "9" },
            { "٠", "0" }, { "١", "1" }, { "٢", "2" }, { "٣", "3" }, { "٤", "4" }, { "٥", "5" }, { "٦", "6" }, { "٧", "7" }, { "٨", "8" }, { "٩", "9" },
        };
            return englishNumbers.Aggregate(input, (current, numbers) => current.Replace(numbers.Key, numbers.Value));
        }

        public static string ToPersian(this string input)
        {
            var englishNumbers = new Dictionary<string, string>()
            {
                { "ك", "ک" }, { "ى", "ی" }, { "ي", "ی" }, { "ئ", "ی" }, { "َ", "" }, { "ُ", "" }, { "ِ", "" }, { "ّ", "" }, { "ؤ", "و" }, { "ۀ", "ه" }, { "ة", "ه" },
            };
            return englishNumbers.Aggregate(input, (current, numbers) => current.Replace(numbers.Key, numbers.Value));
        }

        public static string GetRandom(int length = 10, bool number = true, bool lower = true, bool upper = false, bool symbol = false)
        {
            var chars = "" + (number ? "0123456789" : "") + (lower ? "abcdefghijklmnopqrstuvwxyz" : "") + (upper ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : "") + (symbol ? "!@#$%^&*()-=_+[]{};:|." : "");
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static IQueryable<T> Filter<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, bool>>? access = null) where T : class
        {
            access ??= x => true;
            predicate ??= x => true;
            return query.Where(predicate).Where(access);
        }

        public static ResultDto<string> SendAuthSmsKavenegar(string mobile, string code)
        {
            try
            {
                var kavenegar = new KavenegarApi("");
                var result = kavenegar.VerifyLookup(mobile, code, "OtpLogin");
                return new ResultDto<string>
                {
                    IsSuccess = true,
                    Message = $"پیامک ارسال شد - StatusText: {result.StatusText}, MessageId: {result.Messageid}"
                };
            }
            catch (Kavenegar.Exceptions.ApiException apiEx)
            {
                return new ResultDto<string> { IsSuccess = false, Message = apiEx.Message };
            }
            catch (Exception ex)
            {
                return new ResultDto<string> { IsSuccess = false, Message = ex.Message };
            }
        }

        public static class MobileValidator
        {
            public static (bool isValid, string normalized) Normalize(string mobile)
            {
                if (string.IsNullOrWhiteSpace(mobile))
                    return (false, string.Empty);

                // Replace your ConvertMobile / MobileNormalize logic here
                var normalized = mobile.Trim();
                if (normalized.StartsWith("0098")) normalized = "0" + normalized[4..];
                if (normalized.StartsWith("+98")) normalized = "0" + normalized[3..];
                if (normalized.StartsWith("98") && normalized.Length == 12) normalized = "0" + normalized[2..];
                if (!normalized.StartsWith("0")) normalized = "0" + normalized;

                var isValid = normalized.Length == 11
                              && normalized.StartsWith("09")
                              && normalized.All(char.IsDigit);

                return (isValid, normalized);
            }
        }

        public static string ToPersianDateString(this DateTime date)
        {
            PersianCalendar pc = new PersianCalendar();
            var year = pc.GetYear(date);
            var month = pc.GetMonth(date);
            var day = pc.GetDayOfMonth(date);

            return $"{year:0000}/{month:00}/{day:00}";
        }

        public static string NormalizeSlugTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return title;

            var trimmed = title.Trim();

            trimmed = trimmed
                .Replace("%", "-")
                .Replace("&", "-")
                .Replace("+", "-");

            var normalized = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s+", "-");

            normalized = System.Text.RegularExpressions.Regex.Replace(
                normalized,
                @"[^\p{L}\p{N}\-_]",
                "");

            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"-+", "-");

            normalized = normalized.Trim('-');

            return normalized.ToLowerInvariant();
        }
    }
}