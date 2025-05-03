using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Okean_Mobile.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Okean_Mobile.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerHomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerHomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get featured products
            var featuredProducts = await _context.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.StockQuantity)
                .Take(6)
                .ToListAsync();

            ViewBag.FeaturedProducts = featuredProducts;
            return View();
        }
    }
} 