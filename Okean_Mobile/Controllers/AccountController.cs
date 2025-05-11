using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Okean_Mobile.Models;
using Okean_Mobile.Repositories.Interfaces;
using Okean_Mobile.ViewModels;
using Okean_Mobile.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Okean_Mobile.Controllers
{
    public class AccountController : Controller
    {
        // Khai báo repository để tương tác với database
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;

        // Constructor: Inject UserRepository để sử dụng
        public AccountController(
            IUserRepository userRepository,
            IEmailService emailService,
            IOtpService otpService)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _otpService = otpService;
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userRepository.GetByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại trong hệ thống");
                return View(model);
            }

            // Tạo OTP
            var otp = _otpService.GenerateOtp();
            
            // Lưu OTP vào TempData để sử dụng sau
            TempData["ResetPasswordOtp"] = otp;
            TempData["ResetPasswordEmail"] = model.Email;

            // Gửi email chứa OTP
            var subject = "Mã xác nhận đặt lại mật khẩu - Okean Mobile";
            var body = $@"
                <h2>Xin chào,</h2>
                <p>Bạn đã yêu cầu đặt lại mật khẩu tại Okean Mobile.</p>
                <p>Mã xác nhận của bạn là: <strong>{otp}</strong></p>
                <p>Mã này sẽ hết hạn sau 5 phút.</p>
                <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
            ";

            await _emailService.SendEmailAsync(model.Email, subject, body);

            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = TempData.Peek("ResetPasswordEmail") as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var storedOtp = TempData["ResetPasswordOtp"] as string;
            var storedEmail = TempData["ResetPasswordEmail"] as string;

            if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(storedEmail))
            {
                ModelState.AddModelError("", "Phiên làm việc đã hết hạn. Vui lòng thử lại.");
                return RedirectToAction("ForgotPassword");
            }

            if (!_otpService.ValidateOtp(storedOtp, model.OtpCode))
            {
                ModelState.AddModelError("", "Mã OTP không chính xác");
                return View(model);
            }

            var user = await _userRepository.GetByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại trong hệ thống");
                return View(model);
            }

            // Cập nhật mật khẩu mới
            user.Password = HashPassword(model.NewPassword);
            await _userRepository.UpdateUserAsync(user);

            // Xóa TempData
            TempData.Remove("ResetPasswordOtp");
            TempData.Remove("ResetPasswordEmail");

            TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công";
            return RedirectToAction("Login");
        }
    }
}
