using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;
using UniMart_App.ViewModels;
using System.Security.Claims;

namespace UniMart_App.Controllers
{
    [Authorize]
    public class CardsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CardsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cards
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cards = await _context.Cards
                .Where(c => c.UserId == userId)
                .Select(c => new CardDisplayModel
                {
                    Id = c.Id,
                    CardholderName = c.CardholderName,
                    MaskedCardNumber = MaskCardNumber(c.CardNumber),
                    ExpiryDate = c.ExpiryDate,
                    CardType = c.CardType,
                    IsDefault = c.IsDefault
                })
                .ToListAsync();

            var viewModel = new CardListViewModel { Cards = cards };
            return View(viewModel);
        }

        // POST: Cards/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CardViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModel = await GetCardListViewModel();
                viewModel.NewCard = model;
                return View("Index", viewModel);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string cardType = DetermineCardType(model.CardNumber);

            var card = new Card
            {
                UserId = userId,
                CardholderName = model.CardholderName ?? "",
                CardNumber = model.CardNumber ?? "",
                ExpiryDate = model.ExpiryDate ?? "",
                CVV = model.CVV ?? "",
                CardType = cardType,
                IsDefault = model.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            if (model.IsDefault)
            {
                var existingDefaultCards = await _context.Cards
                    .Where(c => c.UserId == userId && c.IsDefault)
                    .ToListAsync();

                foreach (var existingCard in existingDefaultCards)
                {
                    existingCard.IsDefault = false;
                }
            }
            else if (!await _context.Cards.AnyAsync(c => c.UserId == userId))
            {
                card.IsDefault = true;
            }

            _context.Cards.Add(card);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Card added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cards/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var card = await _context.Cards.FindAsync(id);
            
            if (card == null || card.UserId != userId)
            {
                return NotFound();
            }

            bool wasDefault = card.IsDefault;
            
            _context.Cards.Remove(card);
            await _context.SaveChangesAsync();

            // If the deleted card was the default, set another card as default if available
            if (wasDefault)
            {
                var newDefaultCard = await _context.Cards
                    .Where(c => c.UserId == userId)
                    .FirstOrDefaultAsync();

                if (newDefaultCard != null)
                {
                    newDefaultCard.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Card removed successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cards/SetDefault/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Update all cards to not be default
            var userCards = await _context.Cards
                .Where(c => c.UserId == userId)
                .ToListAsync();

            foreach (var card in userCards)
            {
                card.IsDefault = (card.Id == id);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Default card updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Helper methods
        private string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
                return cardNumber;

            return "•••• •••• •••• " + cardNumber.Substring(cardNumber.Length - 4);
        }

        private string DetermineCardType(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 1)
                return "Unknown";

            char firstDigit = cardNumber[0];
            
            return firstDigit switch
            {
                '4' => "Visa",
                '5' => "MasterCard",
                '3' => "American Express",
                '6' => "Discover",
                _ => "Other"
            };
        }

        private async Task<CardListViewModel> GetCardListViewModel()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var cards = await _context.Cards
                .Where(c => c.UserId == userId)
                .Select(c => new CardDisplayModel
                {
                    Id = c.Id,
                    CardholderName = c.CardholderName,
                    MaskedCardNumber = MaskCardNumber(c.CardNumber),
                    ExpiryDate = c.ExpiryDate,
                    CardType = c.CardType,
                    IsDefault = c.IsDefault
                })
                .ToListAsync();

            return new CardListViewModel
            {
                Cards = cards,
                NewCard = new CardViewModel()
            };
        }
    }
}

