using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Data;
using Okean_Mobile.Models;
using Okean_Mobile.Services;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Okean_Mobile.Controllers
{
    [Authorize(Roles = "Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly VNPayService _vnPayService;

        public OrderController(ApplicationDbContext context, VNPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
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
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction(nameof(Index));
            }

            // Calculate subtotal
            ViewBag.Subtotal = cartItems.Sum(c => c.Quantity * c.Product.Price);
            ViewBag.CartItems = cartItems;

            return View();
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShippingAddress,PhoneNumber,Note,PaymentMethod")] Order order)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users.FindAsync(userId);
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
                ViewBag.CartItems = cartItems;

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
                order.User = user;
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

                // Handle payment based on selected method
                if (order.PaymentMethod == "VNPay")
                {
                    decimal amount = order.OrderDetails.Sum(od => od.Quantity * od.Price);
                    string orderInfo = $"Thanh toán đơn hàng #{order.Id}";
                    string paymentUrl = _vnPayService.CreatePaymentUrl(
                        orderId: order.Id.ToString(),
                        fullName: user.Username,
                        description: orderInfo,
                        amount: (double)amount,
                        context: HttpContext
                    );
                    return Redirect(paymentUrl);
                }

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

        // GET: Order/Payment/5
        public async Task<IActionResult> Payment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Order/Payment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            decimal amount = order.OrderDetails.Sum(od => od.Quantity * od.Price);
            string orderInfo = $"Thanh toán đơn hàng #{order.Id}";
            string paymentUrl = _vnPayService.CreatePaymentUrl(
                orderId: order.Id.ToString(),
                fullName: order.User.Username,
                description: orderInfo,
                amount: (double)amount,
                context: HttpContext
            );

            return Redirect(paymentUrl);
        }

        // GET: Order/PaymentConfirm
        public async Task<IActionResult> PaymentConfirm()
        {
            string queryString = Request.QueryString.Value;
            bool isValid = _vnPayService.ValidatePayment(queryString);

            if (isValid)
            {
                var parameters = HttpUtility.ParseQueryString(queryString);
                int orderId = int.Parse(parameters["vnp_TxnRef"]);

                var order = await _context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    order.Status = "Processing";
                    _context.Update(order);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thanh toán thành công! Đơn hàng của bạn đang được xử lý.";
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }
            }

            TempData["ErrorMessage"] = "Thanh toán thất bại. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Order/MarkAsReceived/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsReceived(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status != "Shipped")
            {
                TempData["ErrorMessage"] = "Chỉ có thể xác nhận đã nhận hàng khi đơn hàng đang trong trạng thái đang giao hàng.";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            order.Status = "Complete";
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xác nhận nhận hàng thành công!";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
    }
}