using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Data;
using Okean_Mobile.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Okean_Mobile.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ApplicationDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Category
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories.ToListAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách danh mục");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi lấy danh sách danh mục";
                return View(new List<Category>());
            }
        }

        // GET: Category/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
        {
            try
            {
                _logger.LogInformation("Bắt đầu thêm danh mục mới: {Name}", category.Name);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Validation errors: {Errors}", string.Join(", ", errors));
                    
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    
                    return View(category);
                }

                // Kiểm tra trùng tên danh mục
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());
                
                if (existingCategory != null)
                {
                    ModelState.AddModelError("Name", "Tên danh mục đã tồn tại");
                    _logger.LogWarning("Tên danh mục đã tồn tại: {Name}", category.Name);
                    return View(category);
                }

                // Thêm danh mục mới
                _context.Add(category);
                var result = await _context.SaveChangesAsync();
                
                if (result > 0)
                {
                    _logger.LogInformation("Thêm danh mục thành công: {Name}", category.Name);
                    TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("Không thể lưu danh mục vào database");
                    ModelState.AddModelError("", "Không thể lưu danh mục vào database");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm danh mục mới: {Name}", category.Name);
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm danh mục mới: " + ex.Message);
            }
            return View(category);
        }

        // GET: Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin danh mục để chỉnh sửa");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi lấy thông tin danh mục";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Kiểm tra trùng tên danh mục
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id);
                    
                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("Name", "Tên danh mục đã tồn tại");
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Lỗi đồng thời khi cập nhật danh mục");
                if (!CategoryExists(category.Id))
                {
                    return NotFound();
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật danh mục";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật danh mục");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật danh mục";
            }
            return View(category);
        }

        // GET: Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(m => m.Id == id);
                
                if (category == null)
                {
                    return NotFound();
                }

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin danh mục để xóa");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi lấy thông tin danh mục";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return NotFound();
                }

                // Kiểm tra xem danh mục có sản phẩm không
                if (category.Products?.Any() == true)
                {
                    TempData["ErrorMessage"] = "Không thể xóa danh mục vì có sản phẩm thuộc danh mục này";
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa danh mục");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa danh mục";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
} 