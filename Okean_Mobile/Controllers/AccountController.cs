using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Okean_Mobile.Models;
using Okean_Mobile.Repositories;
using Okean_Mobile.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Okean_Mobile.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;

        public AccountController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // ======= Helper Hash Password (SHA256) =======
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool VerifyPassword(string enteredPassword, string hashedPassword)
        {
            var enteredHash = HashPassword(enteredPassword);
            return enteredHash == hashedPassword;
        }
        // ==============================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra Username
                var existingUser = await _userRepository.GetByUsernameAsync(model.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Username already exists");
                    return View(model);
                }

                // Kiểm tra Email
                var existingEmail = await _userRepository.GetByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("", "Email already exists");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Password = HashPassword(model.Password), // Hash SHA256
                    Email = model.Email,
                    FullName = model.FullName,
                    Role = "Customer" // Mặc định Customer
                };

                await _userRepository.AddUserAsync(user);
                return RedirectToAction("Login");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.GetByUsernameAsync(model.Username);
                if (user == null || !VerifyPassword(model.Password, user.Password))
                {
                    ModelState.AddModelError("", "Invalid username or password");
                    return View(model);
                }

                // Tạo Claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin"); // Nếu Admin
                }
                else
                {
                    return RedirectToAction("Index", "Home"); // Nếu Customer
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
