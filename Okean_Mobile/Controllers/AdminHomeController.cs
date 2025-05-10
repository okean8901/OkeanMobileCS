using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Okean_Mobile.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Okean_Mobile.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminHomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminHomeController(ApplicationDbContext context)
        {
            _context = context;
        }
        // Tạo một model cụ thể cho đơn hàng gần đây
        public class RecentOrderViewModel
        {
            public int Id { get; set; }
            public string CustomerName { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
        }

        public async Task<IActionResult> Index()
        {
            // Get statistics
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            // Calculate total revenue
            ViewBag.TotalRevenue = await _context.OrderDetails
                .Include(od => od.Product)
                .SumAsync(od => od.Quantity * od.Product.Price);

            // Get recent orders
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new RecentOrderViewModel
                {
                    Id = o.Id,
                    CustomerName = o.User.Username,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.OrderDetails.Sum(od => od.Quantity * od.Product.Price),
                    Status = o.Status
                })
                .ToListAsync();

            ViewBag.RecentOrders = recentOrders;
            return View();
        }
    }
}