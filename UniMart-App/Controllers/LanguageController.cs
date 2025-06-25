using Microsoft.AspNetCore.Mvc;
using UniMart_App.Services;

namespace UniMart_App.Controllers
{
    public class LanguageController : Controller
    {
        private readonly ILocalizationService _localizationService;

        public LanguageController(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (string.IsNullOrEmpty(culture))
            {
                culture = "en-US";
            }

            _localizationService.SetLanguage(culture);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult GetCurrentLanguage()
        {
            var currentLanguage = _localizationService.GetCurrentLanguage();
            return Json(new { language = currentLanguage });
        }

        [HttpGet]
        public IActionResult GetSupportedLanguages()
        {
            var supportedLanguages = _localizationService.GetSupportedLanguages();
            return Json(supportedLanguages);
        }
    }
}

