using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotifStokTakip.Service.Data;
using MotifStokTakip.Model.Entities;
using MotifStokTakip.Model.Enums;
using MotifStokTakip.WebUI.Models;
using MotifStokTakip.WebUI.Pdf;

namespace MotifStokTakip.WebUI.Controllers;

[Authorize(Policy = "UstaOrAdmin")]
public class ServiceOrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ServiceOrdersController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db; _env = env;
    }

    // ==== LISTE VMs ====
    public class ServiceOrderListItemVM
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "—";
        public string Vehicle { get; set; } = "—";
        public string Technician { get; set; } = "—";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class ServiceOrderIndexVM
    {
        public IReadOnlyList<ServiceOrderListItemVM> Items { get; set; } = Array.Empty<ServiceOrderListItemVM>();
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public ServiceStatus? Status { get; set; }
        public string? Q { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    // -------- INDEX (filtre + 10’lu sayfalama) --------
    [HttpGet]
    public async Task<IActionResult> Index(ServiceStatus? status, string? q, DateTime? from, DateTime? to, int page = 1)
    {
        const int pageSize = 10;

        var query = _db.ServiceOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.AssignedUser)
            // Usta bilgisini listede gösterebilmek için:
            .Include(o => o.Technicians)
                .ThenInclude(ot => ot.Technician)
            .AsQueryable();

        if (status != null)
            query = query.Where(x => x.Status == status);

        if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(o => o.CreatedAt < to.Value.ToUniversalTime());

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(o =>
                (o.Customer != null && o.Customer.FullName.Contains(q)) ||
                (o.Vehicle != null && (
                    o.Vehicle.Plate.Contains(q) ||
                    (o.Vehicle.Brand != null && o.Vehicle.Brand.Contains(q)) ||
                    (o.Vehicle.Model != null && o.Vehicle.Model.Contains(q))
                ))
            );
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        if (totalPages == 0) totalPages = 1;
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var items = await query
            .OrderByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new ServiceOrderListItemVM
            {
                Id = o.Id,
                Customer = o.Customer != null ? o.Customer.FullName : "—",
                Vehicle = o.Vehicle != null ? $"{o.Vehicle.Plate} - {o.Vehicle.Brand} {o.Vehicle.Model}" : "—",

                // Önce Ana usta, yoksa ilk usta, yoksa AssignedUser, yoksa "—"
                Technician =
                    (o.Technicians
                        .OrderByDescending(t => t.IsPrimary)
                        .Select(t => t.Technician.FullName)
                        .FirstOrDefault())
                    ?? (o.AssignedUser != null ? o.AssignedUser.FullName : "—"),

                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        var vm = new ServiceOrderIndexVM
        {
            Items = items,
            Page = page,
            TotalPages = totalPages,
            TotalCount = total,
            Status = status,
            Q = q,
            From = from,
            To = to
        };

        return View(vm);
    }

    // -------- CREATE --------
    [HttpGet]
    public async Task<IActionResult> Create(int? vehicleId, int? customerId)
    {
        var vm = new ServiceOrderCreateViewModel();

        if (vehicleId.HasValue)
        {
            var v = await _db.Vehicles.Include(x => x.Customer)
                                      .FirstOrDefaultAsync(x => x.Id == vehicleId.Value);
            if (v != null)
            {
                vm.VehicleId = v.Id;
                vm.VehicleText = $"{v.Plate} {v.Brand} {v.Model}";
                vm.CustomerId = (v.CustomerId is int cid) ? cid : 0;
                vm.CustomerText = v.Customer?.FullName;
            }
        }

        if (customerId.HasValue && vm.CustomerId == 0)
        {
            var c = await _db.Customers.FindAsync(customerId.Value);
            if (c != null)
            {
                vm.CustomerId = c.Id;
                vm.CustomerText = c.FullName;
            }
        }

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceOrderCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var order = new ServiceOrder
        {
            VehicleId = vm.VehicleId,
            CustomerId = vm.CustomerId,
            CreatedAt = DateTime.UtcNow
        };

        var title = string.IsNullOrWhiteSpace(vm.Title)
            ? $"Servis kaydı {DateTime.Now:dd.MM.yyyy HH:mm}"
            : vm.Title.Trim();

        var complaint = string.IsNullOrWhiteSpace(vm.Description) ? "-" : vm.Description.Trim();

        TrySet(order, "Title", title);
        TrySet(order, "ComplaintText", complaint);
        TrySet(order, "Description", complaint);
        TrySetEnumFirst(order, "Status");
        TrySet(order, "Priority", 0);

        _db.ServiceOrders.Add(order);
        await _db.SaveChangesAsync();

        TempData["ok"] = $"Servis kaydı #{order.Id} oluşturuldu.";
        return RedirectToAction("Details", new { id = order.Id });

        static void TrySet(object target, string prop, object? value)
        {
            var p = target.GetType().GetProperty(prop);
            if (p == null || value == null) return;
            try
            {
                var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                p.SetValue(target, Convert.ChangeType(value, t));
            }
            catch { }
        }

        static void TrySetEnumFirst(object target, string prop)
        {
            var p = target.GetType().GetProperty(prop);
            if (p == null) return;
            var t = p.PropertyType;
            if (!t.IsEnum) return;
            var first = Enum.GetValues(t).GetValue(0);
            p.SetValue(target, first);
        }
    }

    // -------- DETAILS --------
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var o = await _db.ServiceOrders
            .Include(x => x.Customer)
            .Include(x => x.Vehicle)
            .Include(x => x.AssignedUser)
            .Include(x => x.Photos)
            .Include(x => x.Invoices).ThenInclude(i => i.Items)
            .Include(x => x.Technicians)
                .ThenInclude(ot => ot.Technician)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (o == null) return NotFound();

        ViewBag.Technicians = await _db.Technicians
            .Where(t => t.IsActive)
            .OrderBy(t => t.FullName)
            .ToListAsync();

        return View(o);
    }

    // -------- Usta / Statü güncelle --------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAssignee(int id, int? assignedUserId)
    {
        var o = await _db.ServiceOrders.FindAsync(id);
        if (o == null) return NotFound();
        o.AssignedUserId = assignedUserId;
        o.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["ok"] = "Usta ataması güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, ServiceStatus status)
    {
        var o = await _db.ServiceOrders.FindAsync(id);
        if (o == null) return NotFound();
        o.Status = status;
        if (status == ServiceStatus.ServisTamamlandi) o.CompletedAt = DateTime.UtcNow;
        o.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["ok"] = "Durum güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // -------- Fotoğraf yükleme --------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhotos(ServicePhotoUploadViewModel vm)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Details), new { id = vm.ServiceOrderId });

        var order = await _db.ServiceOrders.FindAsync(vm.ServiceOrderId);
        if (order == null) return NotFound();

        var dir = Path.Combine(_env.WebRootPath, "servicephotos", vm.ServiceOrderId.ToString());
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        foreach (var f in vm.Files)
        {
            if (f.Length == 0) continue;
            var name = $"{Guid.NewGuid():N}{Path.GetExtension(f.FileName)}";
            var full = Path.Combine(dir, name);
            using (var fs = System.IO.File.Create(full))
            {
                await f.CopyToAsync(fs);
            }

            _db.ServicePhotos.Add(new ServicePhoto
            {
                ServiceOrderId = vm.ServiceOrderId,
                FilePath = $"/servicephotos/{vm.ServiceOrderId}/{name}",
                IsBefore = vm.IsBefore
            });
        }
        await _db.SaveChangesAsync();
        TempData["ok"] = "Fotoğraflar yüklendi.";
        return RedirectToAction(nameof(Details), new { id = vm.ServiceOrderId });
    }

    // -------- Fatura oluştur --------
    [HttpGet]
    public async Task<IActionResult> CreateInvoice(int id)
    {
        var order = await _db.ServiceOrders
                             .Include(x => x.Customer)
                             .FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();

        return View(new ServiceInvoiceCreateViewModel
        {
            ServiceOrderId = id,
            Items = new() { new ServiceInvoiceItemVM { Quantity = 1 } }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvoice(ServiceInvoiceCreateViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var order = await _db.ServiceOrders.FindAsync(vm.ServiceOrderId);
        if (order == null) return NotFound();

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var invoice = new ServiceInvoice
            {
                ServiceOrderId = vm.ServiceOrderId,
                TotalAmount = 0m,
                IsPaid = false,
                Items = new List<ServiceInvoiceItem>()
            };

            foreach (var it in vm.Items)
            {
                if (string.IsNullOrWhiteSpace(it.ItemName) && it.ProductId == null && string.IsNullOrWhiteSpace(it.Barcode))
                    continue;

                if (it.Quantity < 1) it.Quantity = 1;

                Product? prod = null;
                int? pid = it.ProductId;

                if (pid == null && !string.IsNullOrWhiteSpace(it.Barcode))
                {
                    var code = it.Barcode.Trim();
                    pid = await _db.Products
                                   .Where(p => p.Barcode == code)
                                   .Select(p => (int?)p.Id)
                                   .FirstOrDefaultAsync();
                    it.ProductId = pid;
                }

                if (pid != null)
                {
                    prod = await _db.Products.FirstOrDefaultAsync(p => p.Id == pid.Value);
                    if (prod == null)
                    {
                        ModelState.AddModelError("", $"Ürün bulunamadı (Id: {pid}).");
                        await tx.RollbackAsync();
                        return View(vm);
                    }

                    if (it.CostPrice <= 0)
                        it.CostPrice = prod.PurchasePrice;

                    if (prod.StockQuantity < it.Quantity)
                    {
                        ModelState.AddModelError("", $"{prod.Name} için stok yetersiz. Stok: {prod.StockQuantity}, İstenen: {it.Quantity}");
                        await tx.RollbackAsync();
                        return View(vm);
                    }

                    var affected = await _db.Products
                        .Where(p => p.Id == prod.Id)
                        .ExecuteUpdateAsync(u => u.SetProperty(p => p.StockQuantity, p => p.StockQuantity - it.Quantity));

                    if (affected == 0)
                    {
                        ModelState.AddModelError("", $"{prod.Name} için stok güncellenemedi.");
                        await tx.RollbackAsync();
                        return View(vm);
                    }
                }

                invoice.Items.Add(new ServiceInvoiceItem
                {
                    ProductId = pid,
                    ItemName = !string.IsNullOrWhiteSpace(it.ItemName) ? it.ItemName : (prod?.Name ?? "Servis Kalemi"),
                    CostPrice = it.CostPrice,
                    Quantity = it.Quantity,
                    SalePrice = it.SalePrice
                });

                invoice.TotalAmount += (it.SalePrice * it.Quantity);
            }

            _db.ServiceInvoices.Add(invoice);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["ok"] = "Fatura oluşturuldu ve stok güncellendi.";
            return RedirectToAction(nameof(Details), new { id = vm.ServiceOrderId });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // -------- Yardımcılar / Ajax / Pdf --------
    private async Task FillDropdowns()
    {
        ViewBag.Customers = await _db.Customers.OrderBy(x => x.FullName).ToListAsync();
        ViewBag.Vehicles = await _db.Vehicles.OrderBy(x => x.Plate).ToListAsync();
        ViewBag.Ustalar = await _db.Users.Where(u => u.Role == UserRole.Usta && u.IsActive).OrderBy(u => u.FullName).ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> VehiclesOfCustomer(int customerId)
    {
        var data = await _db.Vehicles.Where(v => v.CustomerId == customerId)
            .OrderBy(v => v.Plate)
            .Select(v => new { v.Id, Text = v.Plate + " - " + v.Brand + " " + v.Model })
            .ToListAsync();
        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> InvoicePdf(int id)
    {
        var inv = await _db.ServiceInvoices
            .Include(i => i.ServiceOrder)!.ThenInclude(o => o.Customer)
            .Include(i => i.ServiceOrder)!.ThenInclude(o => o.Vehicle)
            .Include(i => i.ServiceOrder)!.ThenInclude(o => o.AssignedUser)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inv == null) return NotFound();

        var bytes = InvoicePdfGenerator.Generate(inv);
        return File(bytes, "application/pdf", $"ServisFaturasi_{id}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> FindProductByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return Json(null);
        var code = barcode.Trim();
        var p = await _db.Products.AsNoTracking()
                                  .FirstOrDefaultAsync(x => x.Barcode == code);
        if (p == null) return Json(null);

        return Json(new { id = p.Id, name = p.Name, price = p.PurchasePrice, stock = p.StockQuantity });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTechnician(int id, int technicianId, bool primary = true)
    {
        var order = await _db.ServiceOrders
            .Include(o => o.Technicians)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        var techExists = await _db.Technicians.AnyAsync(t => t.Id == technicianId && t.IsActive);
        if (!techExists) { TempData["err"] = "Usta bulunamadı/aktif değil."; return RedirectToAction("Details", new { id }); }

        bool already = order.Technicians.Any(x => x.TechnicianId == technicianId);
        if (already)
        {
            TempData["err"] = "Bu usta zaten atanmış.";
            return RedirectToAction("Details", new { id });
        }

        // Ana usta seçildiyse diğer ana işaretleri kaldır
        if (primary)
        {
            foreach (var x in order.Technicians)
                x.IsPrimary = false;
        }

        order.Technicians.Add(new ServiceOrderTechnician
        {
            ServiceOrderId = id,
            TechnicianId = technicianId,
            IsPrimary = primary
        });

        await _db.SaveChangesAsync();
        TempData["ok"] = "Usta atandı.";
        return RedirectToAction("Details", new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnassignTechnician(int id, int technicianId)
    {
        var e = await _db.ServiceOrderTechnicians
            .FirstOrDefaultAsync(x => x.ServiceOrderId == id && x.TechnicianId == technicianId);
        if (e == null) return NotFound();

        _db.ServiceOrderTechnicians.Remove(e);
        await _db.SaveChangesAsync();
        TempData["ok"] = "Usta kaldırıldı.";
        return RedirectToAction("Details", new { id });
    }
}
