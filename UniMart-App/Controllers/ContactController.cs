using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace UniMart_App.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContactController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var tickets = await _context.SupportTickets
                        .Where(t => t.UserId == user.Id)
                        .OrderByDescending(t => t.SentAt)
                        .ToListAsync();
                    ViewBag.SupportTickets = tickets;
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string name, string email, string message)
        {
            var ticket = new SupportTicket
            {
                Subject = $"Contact Form from {name}",
                Message = message,
                SentAt = DateTime.UtcNow,
                Status = "Open"
            };
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ticket.UserId = user.Id;
                }
            }
            await _context.SupportTickets.AddAsync(ticket);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your message has been sent successfully!";
            return RedirectToAction("Index");
        }
    }
}
