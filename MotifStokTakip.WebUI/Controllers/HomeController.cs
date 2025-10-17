using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotifStokTakip.Model.Enums;
using MotifStokTakip.Service.Data;

namespace MotifStokTakip.WebUI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) => _db = db;

        // ---------- ViewModels ----------
        public sealed class YearValueVM
        {
            public int Year { get; set; }
            public decimal Value { get; set; }
        }

        public sealed class YearPaidBreakdownVM
        {
            public int Year { get; set; }
            public decimal Paid { get; set; }
            public decimal Unpaid { get; set; }
        }

        public sealed class DashboardVM
        {
            // (1) Bu ay �r�n sat��lar�
            public decimal ThisMonthProductTotal { get; set; }
            public int ThisMonthProductCount { get; set; }

            // (2) Y�llara g�re �r�n sat�� toplam�
            public IReadOnlyList<YearValueVM> ProductSalesByYear { get; set; } = Array.Empty<YearValueVM>();

            // (3) Servis adetleri
            public int ServiceTotal { get; set; }
            public int ServiceCompleted { get; set; }
            public int ServiceOpen { get; set; }

            // (4) Bu ay servis faturalar� (toplam / paid / unpaid)
            public decimal ThisMonthServiceTotal { get; set; }
            public decimal ThisMonthServicePaid { get; set; }
            public decimal ThisMonthServiceUnpaid { get; set; }

            // (5) Y�llara g�re servis faturalar� (paid / unpaid)
            public IReadOnlyList<YearPaidBreakdownVM> ServiceByYear { get; set; } = Array.Empty<YearPaidBreakdownVM>();

            // (6) En �ok stoktan �r�n alan cari
            public string TopRetailCustomerName { get; set; } = "�";

            // (7) Servise en �ok gelen cari
            public string TopServiceCustomerName { get; set; } = "�";
        }

        // ---------- Dashboard ----------
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Tarih aral�klar� (sunucu saatiyle)
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);
            var yearStart = new DateTime(now.Year, 1, 1);

            // (1) Bu ay: �r�n sat�� toplam tutar ve adet
            // RetailSaleLine -> RetailSale (CreatedAt; Lines: SellPrice, Quantity)
            var thisMonthProductTotal = await _db.RetailSaleLines
                .Where(l => l.RetailSale.CreatedAt >= monthStart && l.RetailSale.CreatedAt < nextMonthStart)
                .SumAsync(l => (decimal?)(l.SellPrice * l.Quantity)) ?? 0m;

            var thisMonthProductCount = await _db.RetailSaleLines
                .Where(l => l.RetailSale.CreatedAt >= monthStart && l.RetailSale.CreatedAt < nextMonthStart)
                .SumAsync(l => (int?)l.Quantity) ?? 0;

            // (2) Y�llara g�re �r�n sat�� toplam�
            var productSalesByYear = await _db.RetailSaleLines.AsNoTracking()
                .GroupBy(l => l.RetailSale.CreatedAt.Year)
                .Select(g => new YearValueVM
                {
                    Year = g.Key,
                    Value = g.Sum(l => (decimal?)(l.SellPrice * l.Quantity)) ?? 0m
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            // (3) Servis adetleri (toplam, tamamlanan, tamamlanmayan)
            var svcTotal = await _db.ServiceOrders.AsNoTracking().CountAsync();
            var svcCompleted = await _db.ServiceOrders.AsNoTracking()
                                   .CountAsync(o => o.Status == MotifStokTakip.Model.Enums.ServiceStatus.ServisTamamlandi);
            var svcOpen = svcTotal - svcCompleted;

            // (4) Bu ay servis faturalar� (toplam / paid / unpaid)
            var invMonthQ = _db.ServiceInvoices.AsNoTracking()
                              .Where(i => i.CreatedAt >= monthStart && i.CreatedAt < nextMonthStart);

            var thisMonthServiceTotal = await invMonthQ.SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;
            var thisMonthServicePaid = await invMonthQ.Where(i => i.IsPaid).SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;
            var thisMonthServiceUnpaid = thisMonthServiceTotal - thisMonthServicePaid;

            // (5) Y�llara g�re servis faturalar� (paid/unpaid)
            var serviceByYear = await _db.ServiceInvoices.AsNoTracking()
                .GroupBy(i => i.CreatedAt.Year)
                .Select(g => new YearPaidBreakdownVM
                {
                    Year = g.Key,
                    Paid = g.Sum(i => i.IsPaid ? (decimal?)i.TotalAmount : 0) ?? 0m,
                    Unpaid = g.Sum(i => !i.IsPaid ? (decimal?)i.TotalAmount : 0) ?? 0m
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            // (6) En �ok stoktan �r�n sat�n alan cari (adet bazl�)
            var topRetailCustomerId = await _db.RetailSaleLines
                .GroupBy(l => l.RetailSale.CustomerId)
                .Select(g => new { Id = g.Key, Qty = g.Sum(l => (int?)l.Quantity) ?? 0 })
                .OrderByDescending(x => x.Qty)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            string topRetailCustomerName = "�";
            if (topRetailCustomerId.HasValue)
                topRetailCustomerName = await _db.Customers
                    .Where(c => c.Id == topRetailCustomerId.Value)
                    .Select(c => c.FullName)
                    .FirstOrDefaultAsync() ?? "�";

            // (7) Servise en �ok gelen cari (adet bazl�)
            var topServiceCustomerId = await _db.ServiceOrders
                .GroupBy(s => s.CustomerId)
                .Select(g => new { Id = g.Key, Cnt = g.Count() })
                .OrderByDescending(x => x.Cnt)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            string topServiceCustomerName = "�";
            if (topServiceCustomerId != 0)
                topServiceCustomerName = await _db.Customers
                    .Where(c => c.Id == topServiceCustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefaultAsync() ?? "�";

            var vm = new DashboardVM
            {
                ThisMonthProductTotal = thisMonthProductTotal,
                ThisMonthProductCount = thisMonthProductCount,

                ProductSalesByYear = productSalesByYear,

                ServiceTotal = svcTotal,
                ServiceCompleted = svcCompleted,
                ServiceOpen = svcOpen,

                ThisMonthServiceTotal = thisMonthServiceTotal,
                ThisMonthServicePaid = thisMonthServicePaid,
                ThisMonthServiceUnpaid = thisMonthServiceUnpaid,

                ServiceByYear = serviceByYear,

                TopRetailCustomerName = topRetailCustomerName,
                TopServiceCustomerName = topServiceCustomerName
            };

            return View(vm);
        }


        [HttpGet]
        public async Task<IActionResult> DashboardData(int? year, int? month)
        {
            // Se�ilmemi�se bug�n�n y�l�/ay�
            var now = DateTime.UtcNow;
            int y = year ?? now.Year;
            int m = month ?? now.Month;

            // Ay aral��� (UTC)
            var monthStart = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonthStart = monthStart.AddMonths(1);

            // 1) Bu AY �r�n sat��lar� (tutar / adet)
            // RetailSaleLines -> RetailSale (header.CreatedAt)
            var thisMonthProductTotal =
                await _db.RetailSaleLines
                    .Where(l => l.RetailSale.CreatedAt >= monthStart && l.RetailSale.CreatedAt < nextMonthStart)
                    .SumAsync(l => (decimal?)(l.SellPrice * l.Quantity)) ?? 0m;

            var thisMonthProductCount =
                await _db.RetailSaleLines
                    .Where(l => l.RetailSale.CreatedAt >= monthStart && l.RetailSale.CreatedAt < nextMonthStart)
                    .SumAsync(l => (int?)l.Quantity) ?? 0;

            // 2) �R�N sat��lar� � y�llara g�re (grafik)
            var productSalesByYear = await _db.RetailSaleLines.AsNoTracking()
                .GroupBy(l => l.RetailSale.CreatedAt.Year)
                .Select(g => new {
                    year = g.Key,
                    value = g.Sum(x => x.SellPrice * x.Quantity)
                })
                .OrderBy(x => x.year)
                .ToListAsync();

            // 3) Servis adetleri (toplam / tamamlanan / a��k) - global
            var svcTotal = await _db.ServiceOrders.AsNoTracking().CountAsync();
            var svcCompleted = await _db.ServiceOrders.AsNoTracking().CountAsync(o => o.Status == ServiceStatus.ServisTamamlandi);
            var svcOpen = svcTotal - svcCompleted;

            // 4) Bu AY servis faturalar� (toplam / �denen / bekleyen)
            var invMonthQ = _db.ServiceInvoices.AsNoTracking()
                .Where(i => i.CreatedAt >= monthStart && i.CreatedAt < nextMonthStart);

            var thisMonthServiceTotal = await invMonthQ.SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;
            var thisMonthServicePaid = await invMonthQ.Where(i => i.IsPaid).SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;
            var thisMonthServiceUnpaid = thisMonthServiceTotal - thisMonthServicePaid;

            // 5) Servis faturalar� � y�llara g�re (�denen/bekleyen) (grafik)
            var serviceByYear = await _db.ServiceInvoices.AsNoTracking()
                .GroupBy(i => i.CreatedAt.Year)
                .Select(g => new {
                    year = g.Key,
                    paid = g.Where(x => x.IsPaid).Sum(x => x.TotalAmount),
                    unpaid = g.Where(x => !x.IsPaid).Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.year)
                .ToListAsync();

            // 6) En �ok stoktan �r�n alan (retail)
            var topRetailCustomerName = await _db.RetailSales.AsNoTracking()
                .GroupBy(s => s.CustomerId)
                .Select(g => new {
                    CustomerId = g.Key,
                    Qty = g.SelectMany(s => s.Lines).Sum(l => l.Quantity)
                })
                .OrderByDescending(x => x.Qty)
                .Take(1)
                .Join(_db.Customers, x => x.CustomerId, c => c.Id, (x, c) => c.FullName)
                .FirstOrDefaultAsync() ?? "�";

            // 7) Servise en �ok gelen cari
            var topServiceCustomerName = await _db.ServiceOrders.AsNoTracking()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, Cnt = g.Count() })
                .OrderByDescending(x => x.Cnt)
                .Take(1)
                .Join(_db.Customers, x => x.CustomerId, c => c.Id, (x, c) => c.FullName)
                .FirstOrDefaultAsync() ?? "�";

            return Json(new
            {
                year = y,
                month = m,

                // Kartlar
                thisMonthProductTotal,
                thisMonthProductCount,
                serviceTotal = svcTotal,
                serviceCompleted = svcCompleted,
                serviceOpen = svcOpen,
                thisMonthServiceTotal,
                thisMonthServicePaid,
                thisMonthServiceUnpaid,

                // Grafikleri g�ncellemek i�in
                productSalesByYear,
                serviceByYear,

                // Etiketler
                topRetailCustomerName,
                topServiceCustomerName
            });
        }
    }
}
