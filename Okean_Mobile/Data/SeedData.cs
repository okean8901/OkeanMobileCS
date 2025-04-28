using System.Linq;
using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Models;

namespace Okean_Mobile.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider, ApplicationDbContext context)
        {
            // Kiểm tra nếu database chưa có dữ liệu
            if (context.Categories.Any() && context.Products.Any())
            {
                return;   // Nếu đã có dữ liệu, không cần seed
            }

            // Thêm dữ liệu mẫu vào bảng Categories
            var categories = new Category[]
            {
            new Category { Name = "Smartphones" },
            new Category { Name = "Accessories" },
            new Category { Name = "Tablets" }
            };

            foreach (var category in categories)
            {
                context.Categories.Add(category);
            }

            // Thêm dữ liệu mẫu vào bảng Products
            var products = new Product[]
            {
            new Product { Name = "iPhone 14", Description = "Latest Apple iPhone", Price = 99999, CategoryId = 1 },
            new Product { Name = "Samsung Galaxy S22", Description = "Latest Samsung Galaxy", Price = 79999, CategoryId = 1 },
            new Product { Name = "Apple AirPods", Description = "Wireless Bluetooth earphones", Price = 15999, CategoryId = 2 },
            new Product { Name = "iPad Pro", Description = "Apple tablet with high performance", Price = 109999, CategoryId = 3 }
            };

            foreach (var product in products)
            {
                context.Products.Add(product);
            }

            // Lưu vào database
            context.SaveChanges();
        }
    }
}
