using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Okean_Mobile.Models;
using Okean_Mobile.Repositories.Interfaces;
using System.Threading.Tasks;

namespace Okean_Mobile.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;

        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // GET: Product
        [AllowAnonymous] // Cho phép cả người dùng chưa đăng nhập xem
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllProductsAsync();
            return View(products);
        }

        // GET: Product/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // GET: Product/ByCategory/5
        [AllowAnonymous]
        public async Task<IActionResult> ByCategory(int categoryId)
        {
            var products = await _productRepository.GetProductsByCategoryAsync(categoryId);
            if (products == null || !products.Any())
            {
                return View("EmptyCategory");
            }
            return View("Index", products);
        }
    }
}