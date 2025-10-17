using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MotifStokTakip.Model.Entities;         // Product, RetailSale, RetailSaleLine
using MotifStokTakip.WebUI.Models;           // PosCreateViewModel
using MotifStokTakip.Service.Data;           // AppDbContext

namespace MotifStokTakip.WebUI.Controllers
{
    public class RetailPosController : Controller
    {
        private readonly AppDbContext _db;
        public RetailPosController(AppDbContext db) => _db = db;

        // ===================== LISTE =====================
        [HttpGet]
        public async Task<IActionResult> Index(string? q, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            // Temel sorgu (müşteriyi left join ediyoruz)
            var baseQuery =
                from s in _db.RetailSales.AsNoTracking()
                join c in _db.Customers.AsNoTracking() on s.CustomerId equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                select new
                {
                    Sale = s,
                    CustomerText = s.CustomerId != null
                        ? (c.FullName ?? c.CompanyName ?? "Cari")
                        : (s.WalkInCustomerName ?? "Sisteme Kayıtlı Olmayan Cari")
                };

            // Tarih filtreleri (CreatedAt UTC saklıysa günlük aralık doğru işlensin)
            if (from.HasValue)
            {
                var start = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);   // gün başlangıcı
                baseQuery = baseQuery.Where(x => x.Sale.CreatedAt >= start);
            }
            if (to.HasValue)
            {
                var end = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc); // ertesi gün başı
                baseQuery = baseQuery.Where(x => x.Sale.CreatedAt < end);
            }

            // Metin arama (id, müşteri adı, kayıt dışı müşteri adı)
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.Sale.Id.ToString().Contains(q) ||
                    (x.CustomerText ?? "").Contains(q) ||
                    (x.Sale.WalkInCustomerName ?? "").Contains(q));
            }

            // Toplam kayıt
            var totalCount = await baseQuery.CountAsync();

            // VM projeksiyonu (kalem sayısı + toplam tutar alt sorgu)
            var query =
                from x in baseQuery
                orderby x.Sale.CreatedAt descending
                select new RetailPosIndexVM
                {
                    Id = x.Sale.Id,
                    CreatedAt = x.Sale.CreatedAt,
                    Customer = x.CustomerText,
                    ItemCount = _db.RetailSaleLines.Count(l => l.RetailSaleId == x.Sale.Id),
                    Total = _db.RetailSaleLines
                                .Where(l => l.RetailSaleId == x.Sale.Id)
                                .Sum(l => (decimal?)(l.SellPrice * l.Quantity)) ?? 0m
                };

            // Sayfalama
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new PagedList<RetailPosIndexVM>(items, totalCount, page, pageSize);

            ViewBag.Search = q;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            return View(model);
        }

        // ===================== CREATE (GET) =====================
        [HttpGet]
        public IActionResult Create() => View(new PosCreateViewModel());

        // ===================== CREATE (POST) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PosCreateViewModel vm)
        {
            // 1) Geçerli satırları topla
            var items = (vm.Items ?? new())
                .Where(i => (i.ProductId.HasValue || !string.IsNullOrWhiteSpace(i.Barcode)) && i.Quantity > 0)
                .ToList();

            if (!items.Any())
            {
                ModelState.AddModelError("", "En az bir ürün giriniz.");
                return View(vm);
            }

            // 2) Stok kontrolü
            var shortages = new List<string>();
            foreach (var i in items)
            {
                Product product;
                if (!string.IsNullOrWhiteSpace(i.Barcode))
                    product = await _db.Products.FirstOrDefaultAsync(p => p.Barcode == i.Barcode)
                              ?? throw new InvalidOperationException($"Barkod bulunamadı: {i.Barcode}");
                else
                    product = await _db.Products.FirstOrDefaultAsync(p => p.Id == i.ProductId!.Value)
                              ?? throw new InvalidOperationException($"Ürün bulunamadı (Id={i.ProductId}).");

                var available = product.StockQuantity; // projede 'Stock' ise değiştir
                if (i.Quantity > available)
                    shortages.Add($"{product.Name} → Stok: {available}, İstenen: {i.Quantity}");
            }

            if (shortages.Any())
            {
                ModelState.AddModelError("", "Yetersiz stok nedeniyle satış kaydedilemedi:\n" + string.Join("\n", shortages));
                return View(vm);
            }

            // 3) Kayıt + stok düşümü (transaction)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var sale = new RetailSale
                {
                    CustomerId = vm.CustomerId,
                    WalkInCustomerName = vm.CustomerId == null
                        ? (string.IsNullOrWhiteSpace(vm.WalkInCustomerName) ? "Sisteme Kayıtlı Olmayan Cari" : vm.WalkInCustomerName!.Trim())
                        : null,
                    CreatedAt = DateTime.UtcNow
                };
                _db.RetailSales.Add(sale);
                await _db.SaveChangesAsync(); // sale.Id için

                decimal subtotal = 0m;

                foreach (var i in items)
                {
                    var product = !string.IsNullOrWhiteSpace(i.Barcode)
                        ? await _db.Products.FirstAsync(p => p.Barcode == i.Barcode)
                        : await _db.Products.FirstAsync(p => p.Id == i.ProductId!.Value);

                    var line = new RetailSaleLine
                    {
                        RetailSaleId = sale.Id,
                        ProductId = product.Id,
                        ItemName = string.IsNullOrWhiteSpace(i.ItemName) ? product.Name : i.ItemName,
                        Barcode = string.IsNullOrWhiteSpace(i.Barcode) ? product.Barcode : i.Barcode,
                        Quantity = i.Quantity,
                        BuyPrice = i.BuyPrice == 0 ? product.PurchasePrice : i.BuyPrice,
                        SellPrice = i.SellPrice
                    };
                    _db.RetailSaleLines.Add(line);

                    subtotal += i.SellPrice * i.Quantity;

                    product.StockQuantity -= i.Quantity; // projede 'Stock' ise değiştir
                    _db.Products.Update(product);
                }

                sale.Subtotal = subtotal; // entity'nde Subtotal yoksa kaldır
                _db.RetailSales.Update(sale);
                await _db.SaveChangesAsync();

                // 4) ÖDEME TUTARI TUTULMUYOR — sadece yöntemi bilgi amaçlı kaydedelim (Amount=0)
                if (!string.IsNullOrWhiteSpace(vm.PaymentMethod))
                {
                    var pay = new RetailPayment
                    {
                        RetailSaleId = sale.Id,
                        Amount = 0m,
                        Method = vm.PaymentMethod,
                        Note = "Ödeme yöntemi (tutar girilmedi)"
                    };
                    await _db.RetailPayments.AddAsync(pay);
                    await _db.SaveChangesAsync();
                }

                await tx.CommitAsync();
                TempData["ok"] = $"Satış #{sale.Id} kaydedildi.";
                return RedirectToAction(nameof(Details), new { id = sale.Id });
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError("", "Satış kaydedilemedi.");
                return View(vm);
            }
        }

        // ===================== DETAY =====================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var sale = await _db.RetailSales
                .Include(x => x.Customer)
                .Include(x => x.Payments) // ödeme tipi göstermek için
                .FirstOrDefaultAsync(x => x.Id == id);
            if (sale == null) return NotFound();

            var lines = await _db.RetailSaleLines
                .Where(l => l.RetailSaleId == id)
                .Include(l => l.Product)
                .OrderBy(l => l.Id)
                .ToListAsync();

            var vm = new RetailSaleDetailsVM
            {
                Sale = sale,
                Lines = lines
            };
            return View(vm);
        }

        // ===================== AJAX: Barkod ile ürün bul =====================
        [HttpGet]
        public async Task<IActionResult> FindByBarcode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return NotFound();

            var p = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Barcode == code);

            if (p == null) return NotFound();

            return Json(new
            {
                id = p.Id,
                name = p.Name,
                barcode = p.Barcode,
                buy = p.PurchasePrice,
                stock = p.StockQuantity
            });
        }

        // ===================== AJAX: Cari arama (autocomplete) =====================
        [HttpGet]
        public async Task<IActionResult> SearchCustomers(string q)
        {
            q = (q ?? "").Trim();
            if (q.Length < 2) return Json(Array.Empty<object>());

            var items = await _db.Customers.AsNoTracking()
                .Where(c => c.FullName.Contains(q)
                         || (c.CompanyName ?? "").Contains(q)
                         || (c.Phone ?? "").Contains(q))
                .OrderBy(c => c.FullName)
                .Take(15)
                .Select(c => new { id = c.Id, name = c.FullName, phone = c.Phone })
                .ToListAsync();

            return Json(items);
        }

        // ===================== YAZDIRMA İŞLEMİ =====================
        [HttpGet]
        public async Task<IActionResult> Print(int id)
        {
            var sale = await _db.RetailSales
                .Include(s => s.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null) return NotFound();

            var lines = await _db.RetailSaleLines
                .Where(l => l.RetailSaleId == id)
                .OrderBy(l => l.Id)
                .AsNoTracking()
                .ToListAsync();

            var vm = new RetailSaleDetailsVM
            {
                Sale = sale,
                Lines = lines
            };

            // İstersen burada “ara toplam / genel toplam” gibi ilave hesapları ViewBag’e koyabilirsin
            ViewBag.Subtotal = lines.Sum(l => l.SellPrice * l.Quantity);

            return View("Print", vm);  // Views/RetailPos/Print.cshtml
        }


        // ===================== AJAX: Stokları toplu kontrol (Kaydet öncesi) =====================
        public class StockCheckItemDto
        {
            public int? ProductId { get; set; }
            public string? Barcode { get; set; }
            public int Quantity { get; set; }
        }

        public class StockCheckRequestDto
        {
            public List<StockCheckItemDto> Items { get; set; } = new();
        }

        public class StockShortageDto
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = "";
            public int Available { get; set; }
            public int Wanted { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckStocks([FromBody] StockCheckRequestDto dto)
        {
            var shortages = new List<StockShortageDto>();

            foreach (var i in dto.Items.Where(x =>
                       (x.ProductId.HasValue || !string.IsNullOrWhiteSpace(x.Barcode)) && x.Quantity > 0))
            {
                Product? product;
                if (!string.IsNullOrWhiteSpace(i.Barcode))
                    product = await _db.Products.FirstOrDefaultAsync(p => p.Barcode == i.Barcode);
                else
                    product = await _db.Products.FirstOrDefaultAsync(p => p.Id == i.ProductId!.Value);

                if (product == null)
                {
                    shortages.Add(new StockShortageDto
                    {
                        ProductId = 0,
                        Name = $"(Ürün bulunamadı) {i.Barcode ?? i.ProductId?.ToString()}",
                        Available = 0,
                        Wanted = i.Quantity
                    });
                    continue;
                }

                var available = product.StockQuantity; // projede 'Stock' ise değiştir
                if (i.Quantity > available)
                {
                    shortages.Add(new StockShortageDto
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        Available = available,
                        Wanted = i.Quantity
                    });
                }
            }

            return Ok(new { ok = shortages.Count == 0, shortages });
        }
    }

    // ===================== ViewModel'ler =====================
    public class RetailPosIndexVM
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Customer { get; set; }
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
    }

    public class RetailSaleDetailsVM
    {
        public RetailSale Sale { get; set; } = default!;
        public List<RetailSaleLine> Lines { get; set; } = new();
    }

    public sealed class PagedList<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages { get; }
        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;

        public PagedList(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        }
    }
}
