using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotifStokTakip.Model.Entities;
using MotifStokTakip.Model.Enums;
using MotifStokTakip.Service.Data;
using MotifStokTakip.WebUI.Models;

namespace MotifStokTakip.WebUI.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly AppDbContext _db;
        public CustomersController(AppDbContext db) => _db = db;

        // -------------------- LISTE & ARAMA --------------------
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            const int PageSize = 10;

            var qry = _db.Customers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                qry = qry.Where(c =>
                    c.FullName.Contains(q) ||
                    (c.CompanyName != null && c.CompanyName.Contains(q)) ||
                    (c.Phone != null && c.Phone.Contains(q)) ||
                    (c.Address != null && c.Address.Contains(q)));
            }

            qry = qry.OrderByDescending(c => c.Id);

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

        // -------------------- YENI --------------------
        [HttpGet]
        public IActionResult Create() => View(new CustomerFormViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerFormViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var c = new Customer
            {
                FullName = vm.FullName.Trim(),
                CompanyName = vm.CompanyName?.Trim(),
                Phone = vm.Phone?.Trim(),
                Address = vm.Address?.Trim()
            };

            _db.Customers.Add(c);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Müşteri kaydedildi.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------- DÜZENLE --------------------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c == null) return NotFound();

            var vm = new CustomerFormViewModel
            {
                Id = c.Id,
                FullName = c.FullName,
                CompanyName = c.CompanyName,
                Phone = c.Phone,
                Address = c.Address
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerFormViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View(vm);

            var c = await _db.Customers.FindAsync(id);
            if (c == null) return NotFound();

            c.FullName = vm.FullName.Trim();
            c.CompanyName = vm.CompanyName?.Trim();
            c.Phone = vm.Phone?.Trim();
            c.Address = vm.Address?.Trim();

            await _db.SaveChangesAsync();
            TempData["ok"] = "Müşteri güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------- SİL --------------------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c == null) return NotFound();

            _db.Customers.Remove(c);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Müşteri silindi.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------- ORTAK HESAP --------------------
        // Servis + Ürün satışı faturaları DAHİL bakiye hesabı
        private async Task<(decimal invoiceTotal, decimal paymentsInPlusServicePaid, decimal paymentsOut, decimal balance)>
            ComputeBalanceAsync(int customerId, DateTime? from = null, DateTime? to = null)
        {
            var qFrom = from?.ToUniversalTime();
            var qTo = to?.ToUniversalTime();

            // --- SERVİS FATURALARI ---
            var orderIdsQ = _db.ServiceOrders
                               .Where(o => o.CustomerId == customerId)
                               .Select(o => o.Id);

            var serviceInvQ = _db.ServiceInvoices
                                 .Where(i => orderIdsQ.Contains(i.ServiceOrderId));

            if (qFrom.HasValue) serviceInvQ = serviceInvQ.Where(i => i.CreatedAt >= qFrom.Value);
            if (qTo.HasValue) serviceInvQ = serviceInvQ.Where(i => i.CreatedAt < qTo.Value);

            var serviceTotal = await serviceInvQ.SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

            // Ödeme Yapıldı işaretli servis faturalarını tahsilat gibi say
            var servicePaidAsCollections =
                await serviceInvQ.Where(i => i.IsPaid)
                                 .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

            // --- ÜRÜN SATIŞLARI ---
            var salesQ = _db.Sales.Where(s => s.CustomerId == customerId);
            if (qFrom.HasValue) salesQ = salesQ.Where(s => s.CreatedAt >= qFrom.Value);
            if (qTo.HasValue) salesQ = salesQ.Where(s => s.CreatedAt < qTo.Value);

            var salesTotalFromHeader =
                await salesQ.Where(s => s.TotalAmount != null)
                            .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            var salesTotalFromItems =
                await salesQ.Where(s => s.TotalAmount == null)
                            .SelectMany(s => s.Items)
                            .SumAsync(it => (decimal?)(it.UnitPrice * it.Quantity)) ?? 0m;

            var productTotal = salesTotalFromHeader + salesTotalFromItems;

            // --- TAHSİLAT / İADE-ÖDEME ---
            var payQ = _db.CustomerPayments.Where(p => p.CustomerId == customerId);
            if (qFrom.HasValue) payQ = payQ.Where(p => p.PaidAt >= qFrom.Value);
            if (qTo.HasValue) payQ = payQ.Where(p => p.PaidAt < qTo.Value);

            var paymentsIn = await payQ.Where(p => p.Direction == PaymentDirection.In)
                                       .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var paymentsOut = await payQ.Where(p => p.Direction == PaymentDirection.Out)
                                        .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var invoiceTotal = serviceTotal + productTotal;
            var collectionsTotal = paymentsIn + servicePaidAsCollections;
            var balance = invoiceTotal - collectionsTotal + paymentsOut;

            return (invoiceTotal, collectionsTotal, paymentsOut, balance);
        }



        // Sınıf içinde, controller seviyesinde:
        private static readonly string[] _closedKeywords = new[] { "Teslim", "Tamam", "Closed", "Complete", "Kap" };

        // -------------------- DETAY (SON HAL) --------------------
        [HttpGet]
        public async Task<IActionResult> Details(int id, int page = 1)
        {
            const int pageSize = 10;

            var c = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            // Araçlar
            var vehicles = await _db.Vehicles
                                    .Where(v => v.CustomerId == id)
                                    .OrderBy(v => v.Plate)
                                    .ToListAsync();

            // Servis geçmişi (SAYFALAMA)
            var ordersQ = _db.ServiceOrders
                             .Include(o => o.Vehicle)
                             .Where(o => o.CustomerId == id)
                             .OrderByDescending(o => o.Id);

            var totalOrders = await ordersQ.CountAsync();
            var totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var orders = await ordersQ
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Servis geçmişi için ÖDEME ETİKETLERİ (yalnızca mevcut sayfadaki servisler)
            var orderIds = orders.Select(o => o.Id).ToList();
            var payInfo = await _db.ServiceInvoices
                .Where(i => orderIds.Contains(i.ServiceOrderId))
                .GroupBy(i => i.ServiceOrderId)
                .Select(g => new { OrderId = g.Key, AnyPaid = g.Any(x => x.IsPaid), AnyUnpaid = g.Any(x => !x.IsPaid) })
                .ToListAsync();

            var paymentMap = payInfo.ToDictionary(
                x => x.OrderId,
                x => x.AnyPaid
                        ? (x.AnyUnpaid ? "Kısmi Ödeme" : "Ödeme Alındı")
                        : "Ödeme Alınmadı"
            );

            // Özetler (fatura toplamı, tahsilatlar, bakiye)
            var (invoiceTotal, inPlusServicePaid, outSum, balance) = await ComputeBalanceAsync(id);

            // AÇIK SERVİS SAYISI — EF içinde ToString/Contains çevrilemediği için HAFIZADA hesapla
            var allStatuses = await _db.ServiceOrders
                .Where(o => o.CustomerId == id)
                .Select(o => o.Status)
                .ToListAsync();

            int openCount = allStatuses.Count(s =>
            {
                var name = s.ToString();
                foreach (var k in _closedKeywords)
                    if (name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                        return false; // kapalı
                return true; // açık
            });

            // Son servis zamanı (tüm kayıtlar)
            var lastServiceAt = await _db.ServiceOrders
                .Where(o => o.CustomerId == id)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => (DateTime?)o.CreatedAt)
                .FirstOrDefaultAsync();

            // ViewBags
            ViewBag.Vehicles = vehicles;
            ViewBag.Orders = orders;
            ViewBag.TotalInvoice = invoiceTotal;
            ViewBag.PaymentsIn = inPlusServicePaid;   // müşteri ödemeleri + IsPaid servis faturaları
            ViewBag.PaymentsOut = outSum;
            ViewBag.Balance = balance;
            ViewBag.OpenCount = openCount;
            ViewBag.LastService = lastServiceAt;
            ViewBag.OrderPaymentMap = paymentMap;     // Servis geçmişindeki ödeme rozetleri
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(c);
        }



        // -------------------- TAHSİLAT LİSTE --------------------
        [HttpGet]
        public async Task<IActionResult> Payments(int id)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            var items = await _db.CustomerPayments
                .Where(p => p.CustomerId == id)
                .OrderByDescending(p => p.PaidAt)
                .Select(p => new CustomerPaymentListItem
                {
                    Id = p.Id,
                    PaidAt = p.PaidAt,
                    Amount = p.Amount,
                    Direction = p.Direction,
                    Method = p.Method,
                    Note = p.Note
                })
                .ToListAsync();

            var (invoiceTotal, inPlusServicePaid, outSum, balance) = await ComputeBalanceAsync(id);

            ViewBag.Customer = customer;
            ViewBag.InvoiceTotal = invoiceTotal;
            ViewBag.InSum = inPlusServicePaid;
            ViewBag.OutSum = outSum;
            ViewBag.Balance = balance;

            return View(items);
        }

        // -------------------- TAHSİLAT EKLE/SİL --------------------
        [HttpGet]
        public async Task<IActionResult> CreatePayment(int id)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            ViewBag.Customer = customer;
            return View(new CustomerPaymentFormViewModel
            {
                CustomerId = id,
                Direction = PaymentDirection.In
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(CustomerPaymentFormViewModel vm)
        {
            var customer = await _db.Customers.FindAsync(vm.CustomerId);
            if (customer == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Customer = customer;
                return View(vm);
            }

            var entity = new CustomerPayment
            {
                CustomerId = vm.CustomerId,
                Amount = vm.Amount,
                Direction = vm.Direction,
                Method = vm.Method,
                Note = vm.Note,
                PaidAt = DateTime.UtcNow
            };

            _db.CustomerPayments.Add(entity);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Tahsilat kaydedildi.";

            return RedirectToAction(nameof(Payments), new { id = vm.CustomerId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePayment(int id, int customerId)
        {
            var p = await _db.CustomerPayments.FindAsync(id);
            if (p == null) return NotFound();

            _db.CustomerPayments.Remove(p);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Kayıt silindi.";
            return RedirectToAction(nameof(Payments), new { id = customerId });
        }

        // -------------------- HIZLI MÜŞTERİ ARAMA (JSON) --------------------
        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(Array.Empty<object>());

            q = q.Trim();

            var list = await _db.Customers
                .Where(c =>
                    c.FullName.Contains(q) ||
                    (c.CompanyName != null && c.CompanyName.Contains(q)) ||
                    (c.Phone != null && c.Phone.Contains(q))
                )
                .OrderBy(c => c.FullName)
                .Take(10)
                .Select(c => new
                {
                    id = c.Id,
                    display = c.FullName
                })
                .ToListAsync();

            return Json(list);
        }

        // -------------------- CARİ EKSTRE --------------------
        [HttpGet]
        public async Task<IActionResult> Ledger(int id, DateTime? from, DateTime? to, int page = 1)
        {
            const int pageSize = 10;

            var customer = await _db.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            var qFrom = from?.ToUniversalTime();
            var qTo = to?.ToUniversalTime();

            var rows = new List<CustomerLedgerRow>(256);

            // --- SERVİS FATURALARI -> BORÇ (ödeme etiketi Type içine yazılıyor) ---
            var orderIds = _db.ServiceOrders.Where(o => o.CustomerId == id).Select(o => o.Id);

            var invQ = _db.ServiceInvoices
                .Include(i => i.ServiceOrder).ThenInclude(o => o.Vehicle)
                .Where(i => orderIds.Contains(i.ServiceOrderId));

            if (qFrom.HasValue) invQ = invQ.Where(i => i.CreatedAt >= qFrom.Value);
            if (qTo.HasValue) invQ = invQ.Where(i => i.CreatedAt < qTo.Value);

            rows.AddRange(await invQ.Select(i => new CustomerLedgerRow
            {
                Date = i.CreatedAt,
                Type = i.IsPaid ? "Servis Faturası | Ödeme Yapıldı" : "Servis Faturası | Ödeme Bekleniyor",
                RefNo = $"SO#{i.ServiceOrderId}/F#{i.Id}",
                Description = (i.ServiceOrder.Vehicle != null)
                                ? $"{i.ServiceOrder.Vehicle.Plate} - {i.ServiceOrder.Vehicle.Brand} {i.ServiceOrder.Vehicle.Model}"
                                : $"Servis #{i.ServiceOrderId}",
                Debit = i.TotalAmount,
                Credit = 0m,
                ServiceOrderId = i.ServiceOrderId,
                InvoiceId = i.Id
            }).ToListAsync());

            // --- ÜRÜN SATIŞLARI -> BORÇ ---
            var salesQ = _db.Sales.Include(s => s.Items).Where(s => s.CustomerId == id);
            if (qFrom.HasValue) salesQ = salesQ.Where(s => s.CreatedAt >= qFrom.Value);
            if (qTo.HasValue) salesQ = salesQ.Where(s => s.CreatedAt < qTo.Value);

            rows.AddRange(await salesQ.Select(s => new CustomerLedgerRow
            {
                Date = s.CreatedAt,
                Type = "Ürün Satış",
                RefNo = $"SALE#{s.Id}",
                Description = "Ürün satış",
                Debit = (decimal?)s.TotalAmount ?? s.Items.Sum(it => (decimal)(it.UnitPrice * it.Quantity)),
                Credit = 0m,
                SaleId = s.Id
            }).ToListAsync());

            // --- TAHSİLAT / İADE-ÖDEME ---
            var payQ = _db.CustomerPayments.Where(p => p.CustomerId == id);
            if (qFrom.HasValue) payQ = payQ.Where(p => p.PaidAt >= qFrom.Value);
            if (qTo.HasValue) payQ = payQ.Where(p => p.PaidAt < qTo.Value);

            rows.AddRange(await payQ.Select(p => new CustomerLedgerRow
            {
                Date = p.PaidAt,
                Type = p.Direction == PaymentDirection.In ? "Tahsilat" : "İade/Ödeme",
                RefNo = $"PAY#{p.Id}",
                Description = p.Direction == PaymentDirection.In ? "Müşteriden tahsilat" : "Müşteriye ödeme",
                Debit = p.Direction == PaymentDirection.Out ? p.Amount : 0m,
                Credit = p.Direction == PaymentDirection.In ? p.Amount : 0m,
                Method = p.Method,
                Note = p.Note,
                PaymentId = p.Id
            }).ToListAsync());

            // --- SIRALA (ARTAN) — running balance doğru hesaplanır
            rows = rows.OrderBy(r => r.Date).ThenBy(r => r.RefNo).ToList();

            // --- DESC SAYFALAMA: en yeni 10 hareket 1. sayfada ---
            var totalRows = rows.Count;
            var totalPages = (int)Math.Ceiling(totalRows / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            // artan listede sondan pencere seç
            var startIndex = Math.Max(0, totalRows - page * pageSize);
            var takeCount = Math.Min(pageSize, totalRows - startIndex);

            // sayfadan ÖNCEKİ hareketlerin etkisi -> açılış bakiye
            decimal opening = rows.Take(startIndex).Sum(r => r.Debit - r.Credit);

            // sayfa içeriği (artan sırada) + akan bakiye
            var pageRowsAsc = rows.Skip(startIndex).Take(takeCount).ToList();
            decimal running = opening;
            var pageWithRunning = pageRowsAsc
                .Select(r => { running += (r.Debit - r.Credit); return (row: r, balance: running); })
                .ToList();

            // ekranda YENİDEN ESKİYE göstermek için ters çevir
            pageWithRunning.Reverse();

            // üst özetler
            var (invoiceTotal, inSumPlusServicePaid, outSum, balance) = await ComputeBalanceAsync(id, from, to);

            ViewBag.Customer = customer;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.InvoiceTotal = invoiceTotal;
            ViewBag.InSum = inSumPlusServicePaid;
            ViewBag.OutSum = outSum;
            ViewBag.Balance = balance;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(pageWithRunning);
        }



        [HttpGet]
        public async Task<IActionResult> Resolve(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return NotFound();
            var c = await _db.Customers
                .FirstOrDefaultAsync(x => x.FullName == q || x.CompanyName == q || x.Phone == q);
            if (c == null) return NotFound();
            return Json(new { id = c.Id, display = c.FullName });
        }
    }
}
