using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Data;
using Okean_Mobile.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Okean_Mobile.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: User
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Password,Email,FullName,Role")] User user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Kiểm tra username đã tồn tại chưa
                    if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                    {
                        ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                        return View(user);
                    }

                    // Kiểm tra email đã tồn tại chưa
                    if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                    {
                        ModelState.AddModelError("Email", "Email đã tồn tại");
                        return View(user);
                    }

                    user.CreatedAt = DateTime.Now;
                    user.UpdatedAt = DateTime.Now;

                    _context.Add(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm người dùng. Vui lòng thử lại.");
            }

            return View(user);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Password,Email,FullName,Role")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Kiểm tra username đã tồn tại chưa (trừ user hiện tại)
                    if (await _context.Users.AnyAsync(u => u.Username == user.Username && u.Id != user.Id))
                    {
                        ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                        return View(user);
                    }

                    // Kiểm tra email đã tồn tại chưa (trừ user hiện tại)
                    if (await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != user.Id))
                    {
                        ModelState.AddModelError("Email", "Email đã tồn tại");
                        return View(user);
                    }

                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    existingUser.Email = user.Email;
                    existingUser.FullName = user.FullName;
                    existingUser.Role = user.Role;
                    existingUser.UpdatedAt = DateTime.Now;

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật người dùng. Vui lòng thử lại.");
            }

            return View(user);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                ModelState.AddModelError("", "Có lỗi xảy ra khi xóa người dùng. Vui lòng thử lại.");
                return View("Delete", await _context.Users.FindAsync(id));
            }
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
} 