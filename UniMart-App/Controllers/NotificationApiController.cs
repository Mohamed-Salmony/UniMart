using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using UniMart_App.Models;
using UniMart_App.Services;
using UniMart_App.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace UniMart_App.Controllers
{
    [Authorize]
    public class NotificationApiController : Controller
    {
        private readonly NotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public NotificationApiController(NotificationService notificationService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _context = context;
        }

        // POST: /NotificationApi/Generate
        [HttpPost]
        public async Task<IActionResult> Generate(string eventType)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            string message = "";
            string title = "";
            string type = "System";

            // Example logic for different roles and events
            if (roles.Contains("Merchant"))
            {
                switch (eventType)
                {
                    case "NewOrder":
                        title = "New Order Received";
                        message = "You have received a new order.";
                        type = "Order";
                        break;
                    case "ProductApproved":
                        title = "Product Approved";
                        message = "Your product has been approved by admin.";
                        type = "System";
                        break;
                    case "LowStock":
                        title = "Low Stock Alert";
                        message = "One or more of your products are low in stock.";
                        type = "Alert";
                        break;
                }
            }
            else if (roles.Contains("Admin"))
            {
                switch (eventType)
                {
                    case "NewUser":
                        title = "New User Registered";
                        message = "A new user has signed up.";
                        type = "System";
                        break;
                    case "ProductPending":
                        title = "Product Pending Approval";
                        message = "A product is awaiting your approval.";
                        type = "Alert";
                        break;
                }
            }
            else // User
            {
                switch (eventType)
                {
                    case "OrderShipped":
                        title = "Order Shipped";
                        message = "Your order has been shipped.";
                        type = "Order";
                        break;
                    case "OrderDelivered":
                        title = "Order Delivered";
                        message = "Your order has been delivered.";
                        type = "Order";
                        break;
                }
            }

            if (!string.IsNullOrEmpty(title))
            {
                await _notificationService.CreateNotificationAsync(user.Id, title, message, type);
                return Ok(new { success = true });
            }
            return BadRequest();
        }

        // GET: /NotificationApi/UserNotifications
        [HttpGet]
        public async Task<IActionResult> UserNotifications()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { error = "User is null", identity = User?.Identity?.Name, claims = User?.Claims?.Select(c => new { c.Type, c.Value }) });
                }
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == user.Id)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new {
                        n.Id,
                        n.Title,
                        n.Message,
                        n.CreatedAt,
                        n.IsRead,
                        n.Type
                    })
                    .ToListAsync();
                return Json(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    error = ex.ToString(),
                    userIdentity = User?.Identity?.Name,
                    userClaims = User?.Claims?.Select(c => new { c.Type, c.Value }),
                    dbContextNull = _context == null,
                    userManagerNull = _userManager == null
                });
            }
        }
    }
}
