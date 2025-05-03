using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Data;
using Okean_Mobile.Models;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Account");
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation($"Loading cart for user {userId}");

                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                        .ThenInclude(p => p.Category)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                _logger.LogInformation($"Found {cartItems.Count} items in cart");
                return View(cartItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart items");
                return View(new List<CartItem>());
            }
        }

        // POST: Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromForm] int productId, [FromForm] int quantity)
        {
            try
            {
                _logger.LogInformation($"Attempting to add product {productId} to cart with quantity {quantity}");

                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Json(new { success = false, message = "Lỗi xác thực người dùng" });
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                if (!product.IsActive)
                {
                    return Json(new { success = false, message = "Sản phẩm đã ngừng bán" });
                }

                if (product.StockQuantity < quantity)
                {
                    return Json(new { success = false, message = "Số lượng sản phẩm không đủ" });
                }

                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += quantity;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = quantity
                    };
                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                var cartCount = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);

                return Json(new { success = true, cartCount = cartCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding product {productId} to cart");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm vào giỏ hàng" });
            }
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng" });
            }

            if (cartItem.Product.StockQuantity < quantity)
            {
                return Json(new { success = false, message = "Số lượng sản phẩm không đủ" });
            }

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();

            var total = await CalculateTotal(userId);
            return Json(new { success = true, total = total });
        }

        // POST: Cart/RemoveItem
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            var cartCount = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            var total = await CalculateTotal(userId);
            return Json(new { success = true, cartCount = cartCount, total = total });
        }

        // GET: Cart/GetCartCount
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { count = 0 });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);

                return Json(new { count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { count = 0 });
            }
        }

        private async Task<decimal> CalculateTotal(int userId)
        {
            return await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity * c.Product.Price);
        }
    }
}