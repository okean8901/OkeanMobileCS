using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Data;
using Okean_Mobile.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Okean_Mobile.Controllers
{
    public class CustomerProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ... existing actions ...

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }

            // Kiểm tra xem người dùng đã đánh giá sản phẩm này chưa
            var existingReview = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingReview != null)
            {
                return Json(new { success = false, message = "Bạn đã đánh giá sản phẩm này. Vui lòng cập nhật đánh giá của bạn." });
            }

            // Kiểm tra xem người dùng đã mua sản phẩm này chưa
            var hasPurchased = await _context.OrderDetails
                .Include(od => od.Order)
                .AnyAsync(od => od.ProductId == productId && 
                               od.Order.UserId == userId && 
                               od.Order.Status == "Completed");

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                IsVerifiedPurchase = hasPurchased
            };

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            // Lấy thông tin đánh giá mới để trả về
            var newReview = await _context.ProductReviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == review.Id);

            return Json(new { 
                success = true,
                review = new {
                    id = newReview.Id,
                    rating = newReview.Rating,
                    comment = newReview.Comment,
                    userName = newReview.User.FullName,
                    createdAt = newReview.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    isVerifiedPurchase = newReview.IsVerifiedPurchase
                }
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateReview(int reviewId, int rating, string comment)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var review = await _context.ProductReviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá hoặc bạn không có quyền chỉnh sửa" });
            }

            review.Rating = rating;
            review.Comment = comment;
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true,
                review = new {
                    id = review.Id,
                    rating = review.Rating,
                    comment = review.Comment,
                    userName = review.User.FullName,
                    createdAt = review.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    isVerifiedPurchase = review.IsVerifiedPurchase
                }
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa" });
            }

            _context.ProductReviews.Remove(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetReviews(int productId, int skip = 0, int take = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserId = userId != null ? int.Parse(userId) : 0;

            var reviews = await _context.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(r => new {
                    id = r.Id,
                    rating = r.Rating,
                    comment = r.Comment,
                    userName = r.User.FullName,
                    createdAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    isVerifiedPurchase = r.IsVerifiedPurchase,
                    isCurrentUser = r.UserId == currentUserId
                })
                .ToListAsync();

            var totalReviews = await _context.ProductReviews
                .CountAsync(r => r.ProductId == productId);

            var averageRating = await _context.ProductReviews
                .Where(r => r.ProductId == productId)
                .AverageAsync(r => (double)r.Rating);

            return Json(new { 
                success = true,
                reviews = reviews,
                totalReviews = totalReviews,
                averageRating = Math.Round(averageRating, 1)
            });
        }
    }
} 