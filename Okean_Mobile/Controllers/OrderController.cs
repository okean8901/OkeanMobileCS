using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Data;
using Okean_Mobile.Models;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace Okean_Mobile.Controllers
{
    [Authorize(Roles = "Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Order
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Order/Create
        public async Task<IActionResult> Create()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            // Calculate subtotal
            ViewBag.Subtotal = cartItems.Sum(c => c.Quantity * c.Product.Price);

            return View();
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShippingAddress,PhoneNumber,Note")] Order order)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!";
                    return RedirectToAction(nameof(Index));
                }

                // Calculate subtotal
                ViewBag.Subtotal = cartItems.Sum(c => c.Quantity * c.Product.Price);

                // Clear ModelState errors for fields we're not binding
                ModelState.Remove("User");
                ModelState.Remove("Status");
                ModelState.Remove("OrderDetails");
                ModelState.Remove("OrderDate");
                ModelState.Remove("UserId");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                .Select(e => e.ErrorMessage)
                                                .ToList();
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + string.Join(", ", errors);
                    return View(order);
                }

                // Create order
                order.UserId = userId;
                order.OrderDate = DateTime.Now;
                order.Status = "Pending";
                order.OrderDetails = new List<OrderDetail>();

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order details
                foreach (var cartItem in cartItems)
                {
                    if (cartItem.Product.StockQuantity < cartItem.Quantity)
                    {
                        throw new Exception($"Sản phẩm {cartItem.Product.Name} không đủ số lượng trong kho");
                    }

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Product.Price
                    };
                    _context.OrderDetails.Add(orderDetail);

                    // Update product stock
                    cartItem.Product.StockQuantity -= cartItem.Quantity;
                }

                // Clear cart
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đơn hàng đã được tạo thành công!";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo đơn hàng: " + ex.Message;
                return View(order);
            }
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}