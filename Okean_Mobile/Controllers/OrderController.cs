using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Okean_Mobile.Models;
using Okean_Mobile.Repositories.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Okean_Mobile.Controllers
{
    [Authorize(Roles = "Customer")]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            ILogger<OrderController> logger)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _logger = logger;
        }

        // GET: Order
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
            return View(orders);
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderRepository.GetOrderByIdAsync(id);
            if (order == null || order.UserId != GetCurrentUserId())
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();

            try
            {
                // Get cart items
                var cartItems = await _cartRepository.GetCartItemsByUserIdAsync(userId.ToString());

                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty";
                    return RedirectToAction("Index", "Cart");
                }

                // Create new order
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    Status = "Pending",
                    OrderDetails = new List<OrderDetail>()
                };

                // Add order details from cart items
                foreach (var cartItem in cartItems)
                {
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Product.Price // Assuming Product is included and has Price
                    });
                }

                // Save order
                await _orderRepository.AddOrderAsync(order);

                // Clear cart
                await _cartRepository.ClearCartAsync(userId);

                TempData["SuccessMessage"] = "Order placed successfully!";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["ErrorMessage"] = "There was an error processing your order";
                return RedirectToAction("Index", "Cart");
            }
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }
}