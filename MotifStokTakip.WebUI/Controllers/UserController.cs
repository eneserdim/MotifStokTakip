using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotifStokTakip.Model.Entities;
using MotifStokTakip.Model.Enums;
using MotifStokTakip.Service.Data;

namespace MotifStokTakip.WebUI.Controllers
{
    [Authorize] // İstersen: [Authorize(Policy="AdminOnly")]
    public class UserController : Controller
    {
        private readonly AppDbContext _db;
        public UserController(AppDbContext db) => _db = db;

        // ---------- Liste + Arama + Sayfalama ----------
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10)
        {
            var query = _db.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(u =>
                    u.FullName.Contains(q) ||
                    u.UserName.Contains(q));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Q = q;

            return View(items);
        }

        // ---------- Create ----------
        [HttpGet]
        public IActionResult Create() =>
            View(new UserFormVM { IsActive = true });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormVM m)
        {
            if (!ModelState.IsValid) return View(m);

            // kullanıcı adı benzersiz mi?
            bool exists = await _db.Users
                .AnyAsync(u => u.UserName.ToLower() == m.UserName.Trim().ToLower());
            if (exists)
            {
                ModelState.AddModelError(nameof(m.UserName), "Bu kullanıcı adı zaten kullanılıyor.");
                return View(m);
            }

            if (string.IsNullOrWhiteSpace(m.Password) || m.Password.Length < 6)
            {
                ModelState.AddModelError(nameof(m.Password), "Şifre en az 6 karakter olmalı.");
                return View(m);
            }

            var e = new AppUser
            {
                FullName = m.FullName.Trim(),
                UserName = m.UserName.Trim(),
                PasswordHash = HashPassword(m.Password),
                Role = m.Role,
                IsActive = m.IsActive
            };

            _db.Users.Add(e);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Kullanıcı oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Edit ----------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            var vm = new UserFormVM
            {
                Id = u.Id,
                FullName = u.FullName,
                UserName = u.UserName,
                Role = u.Role,
                IsActive = u.IsActive
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserFormVM m)
        {
            if (id != m.Id) return BadRequest();
            if (!ModelState.IsValid) return View(m);

            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            // kullanıcı adı benzersiz mi?
            bool exists = await _db.Users
                .AnyAsync(x => x.Id != id && x.UserName.ToLower() == m.UserName.Trim().ToLower());
            if (exists)
            {
                ModelState.AddModelError(nameof(m.UserName), "Bu kullanıcı adı zaten kullanılıyor.");
                return View(m);
            }

            u.FullName = m.FullName.Trim();
            u.UserName = m.UserName.Trim();
            u.Role = m.Role;
            u.IsActive = m.IsActive;

            // Şifre girilirse güncelle
            if (!string.IsNullOrWhiteSpace(m.Password))
            {
                if (m.Password.Length < 6)
                {
                    ModelState.AddModelError(nameof(m.Password), "Şifre en az 6 karakter olmalı.");
                    return View(m);
                }
                u.PasswordHash = HashPassword(m.Password);
            }

            await _db.SaveChangesAsync();
            TempData["ok"] = "Kullanıcı güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Şifre Sıfırla ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["err"] = "Yeni şifre en az 6 karakter olmalı.";
                return RedirectToAction(nameof(Index));
            }

            u.PasswordHash = HashPassword(newPassword);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Şifre güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Aktif/Pasif ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();
            u.IsActive = !u.IsActive;
            await _db.SaveChangesAsync();
            TempData["ok"] = u.IsActive ? "Kullanıcı aktif." : "Kullanıcı pasif edildi.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Delete ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            // Servis kaydında atanmışsa silme
            bool inUse = await _db.ServiceOrders.AnyAsync(o => o.AssignedUserId == id);
            if (inUse)
            {
                TempData["err"] = "Bu kullanıcı servis kayıtlarında kullanılıyor. Önce ilişkileri kaldırın.";
                return RedirectToAction(nameof(Index));
            }

            _db.Users.Remove(u);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Kullanıcı silindi.";
            return RedirectToAction(nameof(Index));
        }

        // --------- Helpers ----------
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    // --------- ViewModel ----------
    public class UserFormVM
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? Password { get; set; } // Edit'te opsiyonel
        public UserRole Role { get; set; } = UserRole.Usta;
        public bool IsActive { get; set; } = true;
    }
}
