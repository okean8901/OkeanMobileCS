using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Data;

namespace Okean_Mobile
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            //đăng ký dịch vụ DbContext với SQL Server
            builder.Services.AddDbContext<Data.ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            //đăng ký các dịch vụ repository
            builder.Services.AddScoped<Repositories.IProductRepository, Repositories.ProductRepository>();
            builder.Services.AddScoped<Repositories.ICategoryRepository, Repositories.CategoryRepository>();
            builder.Services.AddScoped<Repositories.IOrderRepository,
              Repositories.OrderRepository>();


            var app = builder.Build();

            //gọi seedData khi khởi động ứng dụng
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();
                SeedData.Initialize(services, context);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
