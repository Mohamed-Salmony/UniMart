using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Collections.Generic;

namespace UniMart_App.Controllers
{
    public class OAuthController : Controller
    {
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            // Since we've removed the Google authentication provider due to missing packages,
            // we'll redirect to the regular login page with a message
            TempData["Message"] = "Google authentication is currently unavailable. Please use regular login.";
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult MicrosoftLogin()
        {
            // Since we've removed the Microsoft authentication provider due to missing packages,
            // we'll redirect to the regular login page with a message
            TempData["Message"] = "Microsoft authentication is currently unavailable. Please use regular login.";
            return RedirectToAction("Login", "Account");
        }

        // This method would normally handle the OAuth callback
        public IActionResult ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
                return RedirectToAction("Login", "Account");
            }

            // Since we're not using external authentication providers currently,
            // this is just a placeholder method
            return RedirectToAction("Login", "Account");
        }
    }
}
