using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using MotifStokTakip.Model.Entities;
using MotifStokTakip.Service.Data;
using MotifStokTakip.WebUI.Models;

namespace MotifStokTakip.WebUI.Controllers
{
    [Authorize]
    public class VehiclesController : Controller
    {
        private readonly AppDbContext _db;
        public VehiclesController(AppDbContext db) => _db = db;

        // Liste (Cari kolonunu göstermek için Include)
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            const int PageSize = 10;

            var qry = _db.Vehicles
                         .Include(v => v.Customer)
                         .AsNoTracking()
                         .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                qry = qry.Where(v =>
                      v.Plate.Contains(q) ||
                      (v.Brand != null && v.Brand.Contains(q)) ||
                      (v.Model != null && v.Model.Contains(q)) ||
                      (v.Vin != null && v.Vin.Contains(q)) ||
                      (v.Customer != null && v.Customer.FullName.Contains(q)));
            }

            qry = qry.OrderByDescending(v => v.Id);

            var total = await qry.CountAsync();
            var items = await qry.Skip((page - 1) * PageSize)
                                 .Take(PageSize)
                                 .ToListAsync();

            ViewBag.Search = q;
            ViewBag.Page = page;
            ViewBag.PageSize = PageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);

            return View(items);
        }

        // Dropdown’ı dolduran yardımcı
        private async Task<VehicleFormViewModel> BuildVmAsync(VehicleFormViewModel vm)
        {
            vm.Customers = await _db.Customers
                .OrderBy(c => c.FullName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(c.CompanyName)
                           ? c.FullName
                           : $"{c.FullName} • {c.CompanyName}"
                })
                .ToListAsync();
            return vm;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(await BuildVmAsync(new VehicleFormViewModel()));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VehicleFormViewModel vm)
        {
            if (!ModelState.IsValid) return View(await BuildVmAsync(vm));

            var entity = new Vehicle
            {
                Plate = vm.Plate.Trim(),
                Brand = vm.Brand.Trim(),
                Model = vm.Model.Trim(),
                Year = vm.Year,
                Vin = vm.Vin?.Trim(),
                Note = vm.Note,
                CustomerId = vm.CustomerId   // MÜŞTERİ ZORUNLU
            };

            _db.Vehicles.Add(entity);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Araç kaydedildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var v = await _db.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            var vm = new VehicleFormViewModel
            {
                Id = v.Id,
                Plate = v.Plate,
                Brand = v.Brand,
                Model = v.Model,
                Year = v.Year,
                Vin = v.Vin,
                Note = v.Note,
                CustomerId = v.CustomerId
            };
            return View(await BuildVmAsync(vm));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VehicleFormViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View(await BuildVmAsync(vm));

            var v = await _db.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            v.Plate = vm.Plate.Trim();
            v.Brand = vm.Brand.Trim();
            v.Model = vm.Model.Trim();
            v.Year = vm.Year;
            v.Vin = vm.Vin?.Trim();
            v.Note = vm.Note;
            v.CustomerId = vm.CustomerId;

            await _db.SaveChangesAsync();
            TempData["ok"] = "Araç güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var v = await _db.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            _db.Vehicles.Remove(v);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Araç silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlate(int id)
        {
            var v = await _db.Vehicles
                .Include(x => x.ServiceOrders)
                .FirstOrDefaultAsync(x => x.Id == id);
            
            if (v == null) return NotFound();

            // Delete related ServiceOrders (hard delete with cascade)
            if (v.ServiceOrders != null && v.ServiceOrders.Any())
            {
                _db.ServiceOrders.RemoveRange(v.ServiceOrders);
            }

            // Delete the vehicle
            _db.Vehicles.Remove(v);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Araç ve ilgili servis kayıtları silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var v = await _db.Vehicles
                .Include(x => x.Customer)
                .Include(x => x.ServiceOrders)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();

            return View(v);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(Array.Empty<object>());

            q = q.Trim();

            var list = await _db.Vehicles
                .Include(v => v.Customer)
                .Where(v =>
                    v.Plate.Contains(q) ||                           // plaka
                    (v.Brand != null && v.Brand.Contains(q)) ||     // marka
                    (v.Model != null && v.Model.Contains(q))        // model
                /* VIN alanın varsa ekle: || (v.VIN != null && v.VIN.Contains(q)) */
                )
                .OrderBy(v => v.Plate)
                .Take(10)
                .Select(v => new
                {
                    id = v.Id,
                    display = $"{v.Plate} {v.Brand} {v.Model}",
                    customerId = v.CustomerId,
                    customerDisplay = v.Customer != null ? v.Customer.FullName : null
                })
                .ToListAsync();

            return Json(list);
        }


        [HttpGet]
        public async Task<IActionResult> Resolve(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return NotFound();

            var v = await _db.Vehicles.Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.Plate == q /* || x.VIN == q */); // VIN çıkarıldı

            if (v == null) return NotFound();

            return Json(new
            {
                id = v.Id,
                display = $"{v.Plate} {v.Brand} {v.Model}",
                customerId = v.CustomerId,
                customerDisplay = v.Customer?.FullName
            });
        }

    }
}
