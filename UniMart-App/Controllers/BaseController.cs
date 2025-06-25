using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UniMart_App.Models;

namespace UniMart_App.Controllers
{
    public class BaseController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly SignInManager<ApplicationUser> _signInManager;

        public BaseController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        protected async Task<string> GetUserRoleAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    return roles.FirstOrDefault() ?? "User";
                }
            }
            return "Guest";
        }

        protected async Task<IActionResult> RedirectBasedOnRoleAsync()
        {
            var role = await GetUserRoleAsync();
            
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Merchant" => RedirectToAction("Dashboard", "Merchant"),
                "User" => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }

    // Role-based authorization attributes
    public class AdminOnlyAttribute : AuthorizeAttribute
    {
        public AdminOnlyAttribute()
        {
            Roles = "Admin";
        }
    }

    public class MerchantOnlyAttribute : AuthorizeAttribute
    {
        public MerchantOnlyAttribute()
        {
            Roles = "Merchant";
        }
    }

    public class UserOnlyAttribute : AuthorizeAttribute
    {
        public UserOnlyAttribute()
        {
            Roles = "User";
        }
    }

    public class AdminOrMerchantAttribute : AuthorizeAttribute
    {
        public AdminOrMerchantAttribute()
        {
            Roles = "Admin,Merchant";
        }
    }
}

