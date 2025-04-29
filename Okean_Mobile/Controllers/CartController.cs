using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Data;
using Okean_Mobile.Models;
using System.Security.Claims;

namespace Okean_Mobile.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = GetCurrentUserId();
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(newItem);
            }

            await _context.SaveChangesAsync();
            UpdateCartCountInSession();

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            if (quantity <= 0)
            {
                return await RemoveFromCart(id);
            }

            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem == null || cartItem.UserId != GetCurrentUserId())
            {
                return NotFound();
            }

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();
            UpdateCartCountInSession();

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem == null || cartItem.UserId != GetCurrentUserId())
            {
                return NotFound();
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            UpdateCartCountInSession();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            var count = HttpContext.Session.GetInt32("CartCount") ?? 0;
            return Json(new { count });
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        private void UpdateCartCountInSession()
        {
            var userId = GetCurrentUserId();
            var count = _context.CartItems
                .Where(c => c.UserId == userId)
                .Sum(c => c.Quantity);

            HttpContext.Session.SetInt32("CartCount", count);
        }
    }
}