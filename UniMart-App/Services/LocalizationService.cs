using Microsoft.Extensions.Localization;
using System.Globalization;

namespace UniMart_App.Services
{
    public interface ILocalizationService
    {
        string GetLocalizedString(string key);
        void SetLanguage(string culture);
        string GetCurrentLanguage();
        List<string> GetSupportedLanguages();
    }

    public class LocalizationService : ILocalizationService
    {
        private readonly IStringLocalizer<LocalizationService> _localizer;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalizationService(IStringLocalizer<LocalizationService> localizer, IHttpContextAccessor httpContextAccessor)
        {
            _localizer = localizer;
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetLocalizedString(string key)
        {
            return _localizer[key];
        }

        public void SetLanguage(string culture)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            };

            _httpContextAccessor.HttpContext?.Response.Cookies.Append("Culture", culture, cookieOptions);
        }

        public string GetCurrentLanguage()
        {
            var culture = _httpContextAccessor.HttpContext?.Request.Cookies["Culture"];
            return culture ?? "en-US";
        }

        public List<string> GetSupportedLanguages()
        {
            return new List<string> { "en-US", "ar-EG" };
        }
    }
}

