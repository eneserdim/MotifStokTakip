using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotifStokTakip.Model.Entities;
using MotifStokTakip.Service.Data;
using MotifStokTakip.WebUI.Models;

namespace MotifStokTakip.WebUI.Controllers;

[Authorize(Policy = "MuhasebeOrAdmin")]
public class SalesController : Controller
{
    private readonly AppDbContext _db;
    public SalesController(AppDbContext db) { _db = db; }

    [HttpGet]
    public IActionResult Create() => View(new SaleCreateViewModel());

    // Barkoda göre ürün bul (Ürün Satış ekranı için)
    [HttpGet]
    public async Task<IActionResult> FindProduct(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return Json(null);
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Barcode == barcode.Trim());
        if (p == null) return Json(null);
        return Json(new
        {
            id = p.Id,
            name = p.Name,
            price = p.PurchasePrice,
            stock = p.StockQuantity
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleCreateViewModel vm)
    {
        // Boş satırları ayıkla
        var clean = vm.Items
            .Where(i => (i.ProductId != null || !string.IsNullOrWhiteSpace(i.ProductName)) && i.Quantity > 0)
            .ToList();

        if (!clean.Any())
        {
            ModelState.AddModelError("", "En az bir kalem ekleyin.");
            return View(vm);
        }

        var sale = new Sale
        {
            TotalAmount = 0m,
            Items = new List<SaleItem>()
        };

        foreach (var i in clean)
        {
            Product? p = null;
            if (i.ProductId is int pid)
            {
                p = await _db.Products.FirstOrDefaultAsync(x => x.Id == pid);
                if (p == null) continue;

                // Stok düş (nullable güvenlik)
                p.StockQuantity -= i.Quantity;
                if (p.StockQuantity < 0) p.StockQuantity = 0;

                // Fiyat boşsa ürünün alış fiyatını kullan
                if (i.UnitPrice <= 0) i.UnitPrice = p.PurchasePrice;

                // Ürün adı boşsa ürün adını kullan
                i.ProductName ??= p.Name;
            }

            sale.Items.Add(new SaleItem
            {
                ProductId = p?.Id,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                ItemName = i.ProductName ?? "Satış Kalemi"
            });

            sale.TotalAmount += (i.UnitPrice * i.Quantity);
        }

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();

        TempData["ok"] = $"Satış kaydedildi (#{sale.Id}).";
        return RedirectToAction("Index", "Products");
    }
}
