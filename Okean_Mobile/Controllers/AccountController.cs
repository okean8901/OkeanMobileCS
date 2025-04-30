using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Okean_Mobile.Models;
using Okean_Mobile.Repositories.Interfaces;
using Okean_Mobile.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Okean_Mobile.Controllers
{
    public class AccountController : Controller
    {
        // Khai báo repository để tương tác với database
        private readonly IUserRepository _userRepository;

        // Constructor: Inject UserRepository để sử dụng
        public AccountController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // ======= Hàm băm mật khẩu sử dụng SHA256 =======
        private string HashPassword(string password)
        {
            // Tạo đối tượng SHA256 để băm mật khẩu
            using (var sha256 = SHA256.Create())
            {
                // Chuyển mật khẩu thành mảng byte
                var bytes = Encoding.UTF8.GetBytes(password);
                // Băm mật khẩu
                var hash = sha256.ComputeHash(bytes);
                // Chuyển kết quả băm thành chuỗi base64
                return Convert.ToBase64String(hash);
            }
        }

        // Hàm kiểm tra mật khẩu có khớp không
        private bool VerifyPassword(string enteredPassword, string hashedPassword)
        {
            // Băm mật khẩu nhập vào và so sánh với mật khẩu đã băm trong database
            var enteredHash = HashPassword(enteredPassword);
            return enteredHash == hashedPassword;
        }
        // ==============================================

        // Hiển thị trang đăng ký
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Xử lý form đăng ký
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Kiểm tra dữ liệu nhập vào có hợp lệ không
            if (ModelState.IsValid)
            {
                // Kiểm tra Username đã tồn tại chưa
                var existingUser = await _userRepository.GetByUsernameAsync(model.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Username already exists");
                    return View(model);
                }

                // Kiểm tra Email đã tồn tại chưa
                var existingEmail = await _userRepository.GetByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("", "Email already exists");
                    return View(model);
                }

                // Tạo user mới với thông tin từ form
                var user = new User
                {
                    Username = model.Username,
                    Password = HashPassword(model.Password), // Băm mật khẩu trước khi lưu
                    Email = model.Email,
                    FullName = model.FullName,
                    Role = "Customer" // Mặc định là Customer
                };

                // Lưu user vào database
                await _userRepository.AddUserAsync(user);
                // Chuyển hướng về trang đăng nhập
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // Hiển thị trang đăng nhập
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Xử lý form đăng nhập
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Kiểm tra dữ liệu nhập vào có hợp lệ không
            if (ModelState.IsValid)
            {
                // Lấy user từ database theo username
                var user = await _userRepository.GetByUsernameAsync(model.Username);
                // Kiểm tra user tồn tại và mật khẩu khớp
                if (user == null || !VerifyPassword(model.Password, user.Password))
                {
                    ModelState.AddModelError("", "Invalid username or password");
                    return View(model);
                }

                // Tạo Claims (thông tin xác thực) cho user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username), // Tên đăng nhập
                    new Claim(ClaimTypes.Role, user.Role), // Vai trò (Admin/Customer)
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) // ID của user
                };

                // Tạo identity và principal từ claims
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Đăng nhập user bằng cookie authentication
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // Chuyển hướng về trang chủ
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // Xử lý đăng xuất
        public async Task<IActionResult> Logout()
        {
            // Xóa cookie đăng nhập
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Chuyển hướng về trang đăng nhập
            return RedirectToAction("Login");
        }
    }
}
