using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.Results;
using MotifStokTakip.Service.Data;
using MotifStokTakip.Model.Entities;
using MotifStokTakip.WebUI.Models;
using MotifStokTakip.Core.Barcodes;

// barkod için:
using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;
using ZXing.Common;

[Authorize] // login zorunlu
public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IValidator<ProductCreateViewModel> _createValidator;
    private readonly IWebHostEnvironment _env;
    public ProductsController(
            AppDbContext db,
            IValidator<ProductCreateViewModel> createValidator,
            IWebHostEnvironment env) // EK
    {
        _db = db;
        _createValidator = createValidator;
        _env = env; // EK
    }

    // ---------- LIST ----------
    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        const int PageSize = 10;

        var qry = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            qry = qry.Where(p =>
                   p.Name.Contains(q) ||
                   (p.BrandName != null && p.BrandName.Contains(q)) ||
                   (p.OemNumber != null && p.OemNumber.Contains(q)) ||
                   (p.BrandCode != null && p.BrandCode.Contains(q)) ||
                   (p.Barcode != null && p.Barcode.Contains(q)));
        }

        qry = qry.OrderByDescending(p => p.Id);

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


    // ---------- CREATE ----------
    [HttpGet]
    [Authorize(Policy = "MuhasebeOrAdmin")]
    public IActionResult Create() => View(new ProductCreateViewModel());

    [HttpPost]
    [Authorize(Policy = "MuhasebeOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel vm)
    {
        ValidationResult result = await _createValidator.ValidateAsync(vm);
        if (!result.IsValid)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return View(vm);
        }

        var sanitizedBarcode = string.IsNullOrWhiteSpace(vm.Barcode) ? null : vm.Barcode!.Trim();

        var entity = new Product
        {
            Name = vm.Name,
            OemNumber = vm.OemNumber,
            BrandName = vm.BrandName,
            BrandCode = vm.BrandCode,
            PurchasePrice = vm.PurchasePrice,
            ShelfNo = vm.ShelfNo,
            StockQuantity = vm.StockQuantity,
            Barcode = sanitizedBarcode // kullanıcı girdiyse sakla; boşsa null
        };


        _db.Products.Add(entity);
        await _db.SaveChangesAsync();

        TempData["ok"] = "Ürün eklendi.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- EDIT ----------
    [HttpGet]
    [Authorize(Policy = "MuhasebeOrAdmin")]
    public async Task<IActionResult> Edit(int id)
    {
        var x = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (x == null) return NotFound();

        var vm = new ProductEditViewModel
        {
            Id = x.Id,
            Name = x.Name,
            OemNumber = x.OemNumber,
            BrandName = x.BrandName,
            BrandCode = x.BrandCode,
            PurchasePrice = x.PurchasePrice,
            ShelfNo = x.ShelfNo,
            StockQuantity = x.StockQuantity,
            Barcode = x.Barcode
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = "MuhasebeOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditViewModel vm)
    {
        var x = await _db.Products.FirstOrDefaultAsync(p => p.Id == vm.Id);
        if (x == null) return NotFound();

        x.Name = vm.Name;
        x.OemNumber = vm.OemNumber;
        x.BrandName = vm.BrandName;
        x.BrandCode = vm.BrandCode;
        x.PurchasePrice = vm.PurchasePrice;
        x.ShelfNo = vm.ShelfNo;
        x.StockQuantity = vm.StockQuantity;
        x.Barcode = string.IsNullOrWhiteSpace(vm.Barcode) ? null : vm.Barcode!.Trim();
        x.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["ok"] = "Ürün güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- DELETE ----------
    [HttpPost]
    [Authorize(Policy = "AdminOnly")] // Silme sadece Admin
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var x = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (x == null) return NotFound();

        _db.Products.Remove(x);
        await _db.SaveChangesAsync();

        // Barkod dosyası varsa silelim
        var path = Path.Combine(Directory.GetCurrentDirectory(), "MotifStokTakip.WebUI", "wwwroot", "barcodes", $"{id}.png");
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);

        TempData["ok"] = "Ürün silindi.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- BARCODE GENERATE ----------
    [HttpGet]
    [Authorize(Policy = "MuhasebeOrAdmin")]
    public async Task<IActionResult> GenerateBarcode(int id)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        if (string.IsNullOrWhiteSpace(p.Barcode))
        {
            p.Barcode = Ean13Helper.MakeEan13FromId(p.Id);
            await _db.SaveChangesAsync();
        }

        var code = p.Barcode!;
        var useEan13 = code.Length == 13 && code.All(char.IsDigit) && Ean13Helper.IsValid(code);

        var bytes = GenerateAutoBarcodePng(code, useEan13);

        // >>> BURASI DEĞİŞTİ: gerçek wwwroot
        var dir = Path.Combine(_env.WebRootPath, "barcodes");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, $"{p.Id}.png");
        System.IO.File.WriteAllBytes(path, bytes);

        TempData["ok"] = $"Barkod hazır: {code}";
        return RedirectToAction(nameof(BarcodePrint), new { id = p.Id });
    }

    // ---- helper ----
    private static byte[] GenerateAutoBarcodePng(string value, bool ean13Preferred, int width = 600, int height = 220)
    {
        var writer = new ZXing.SkiaSharp.BarcodeWriter
        {
            Format = ean13Preferred ? BarcodeFormat.EAN_13 : BarcodeFormat.CODE_128,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 2,
                PureBarcode = false
            }
        };
        using var bitmap = writer.Write(value);
        using var img = SKImage.FromBitmap(bitmap);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }


    // ---------- BARCODE PRINT VIEW ----------
    [HttpGet]
    public async Task<IActionResult> BarcodePrint(int id)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (string.IsNullOrWhiteSpace(p.Barcode))
            return RedirectToAction(nameof(GenerateBarcode), new { id });

        ViewBag.BarcodePath = $"/barcodes/{p.Id}.png";
        return View(p);
    }

    // SkiaSharp ile PNG üretici (helper)
    private static byte[] GenerateEan13Png(string ean13, int width = 600, int height = 220)
    {
        var writer = new ZXing.SkiaSharp.BarcodeWriter
        {
            Format = BarcodeFormat.EAN_13,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 2,
                PureBarcode = false
            }
        };
        using var bitmap = writer.Write(ean13);
        using var img = SKImage.FromBitmap(bitmap);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [HttpGet]
    public async Task<IActionResult> BarcodePrintRaw(int id)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (string.IsNullOrWhiteSpace(p.Barcode))
            return RedirectToAction(nameof(GenerateBarcode), new { id });

        return View(p); // Views/Products/BarcodePrintRaw.cshtml
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var p = await _db.Products
            .Include(x => x.SaleItems)
            .Include(x => x.ServiceInvoiceItems)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        var vm = new ProductDetailViewModel
        {
            Id = p.Id,
            Name = p.Name,
            OemNumber = p.OemNumber,
            BrandName = p.BrandName,
            BrandCode = p.BrandCode,
            PurchasePrice = p.PurchasePrice,
            ShelfNo = p.ShelfNo,
            StockQuantity = p.StockQuantity,
            Barcode = p.Barcode,
            BarcodeImgUrl = string.IsNullOrWhiteSpace(p.Barcode) ? null : Url.Content($"~/barcodes/{p.Id}.png?v={DateTime.UtcNow.Ticks}"),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            TotalSoldQty = p.SaleItems?.Sum(i => i.Quantity) ?? 0,
            TotalUsedInServicesQty = p.ServiceInvoiceItems?.Sum(i => i.Quantity) ?? 0
        };

        return View(vm); // Views/Products/Details.cshtml
    }

    [HttpGet]
    public async Task<IActionResult> LowStock(int threshold = 10)
    {
        var list = await _db.Products
            .Where(p => p.StockQuantity < threshold)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

        ViewBag.Threshold = threshold;
        return View(list);
    }



}
