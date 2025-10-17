using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotifStokTakip.Service.Data;
using MotifStokTakip.Model.Entities;

namespace MotifStokTakip.WebUI.Controllers
{
    [Authorize]
    public class TechniciansController : Controller
    {
        private readonly AppDbContext _db;
        public TechniciansController(AppDbContext db) => _db = db;

        // LISTE + ARAMA + SAYFALAMA
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _db.Technicians.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(t => t.FullName.Contains(q));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(t => t.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Q = q;

            return View(items);
        }

        // YENI
        [HttpGet]
        public IActionResult Create() => View(new Technician { IsActive = true });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Technician m)
        {
            if (string.IsNullOrWhiteSpace(m.FullName))
                ModelState.AddModelError(nameof(m.FullName), "Ad Soyad zorunludur.");

            if (!ModelState.IsValid) return View(m);

            m.FullName = m.FullName.Trim();
            _db.Technicians.Add(m);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Usta kaydedildi.";
            return RedirectToAction(nameof(Index));
        }

        // DÜZENLE
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _db.Technicians.FindAsync(id);
            return entity == null ? NotFound() : View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Technician m)
        {
            if (id != m.Id) return BadRequest();
            if (string.IsNullOrWhiteSpace(m.FullName))
                ModelState.AddModelError(nameof(m.FullName), "Ad Soyad zorunludur.");
            if (!ModelState.IsValid) return View(m);

            var entity = await _db.Technicians.FindAsync(id);
            if (entity == null) return NotFound();

            entity.FullName = m.FullName.Trim();
            entity.IsActive = m.IsActive;

            await _db.SaveChangesAsync();
            TempData["ok"] = "Usta güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // SİL
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Technicians.FindAsync(id);
            if (entity == null) return NotFound();

            // Servis ataması varsa silme
            var inUse = await _db.ServiceOrderTechnicians.AnyAsync(x => x.TechnicianId == id);
            if (inUse)
            {
                TempData["err"] = "Bu usta servis kayıtlarına atanmış. Önce ilişkileri kaldırın.";
                return RedirectToAction(nameof(Index));
            }

            _db.Technicians.Remove(entity);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Usta silindi.";
            return RedirectToAction(nameof(Index));
        }

        // AJAX – Servis detayı için hızlı arama (yalnızca aktif olanlar)
        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(Array.Empty<object>());

            q = q.Trim();
            var list = await _db.Technicians
                .Where(t => t.IsActive && t.FullName.Contains(q))
                .OrderBy(t => t.FullName)
                .Take(10)
                .Select(t => new { id = t.Id, text = t.FullName })
                .ToListAsync();

            return Json(list);
        }
    }
}
