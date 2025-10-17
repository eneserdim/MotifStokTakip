using System;

namespace MotifStokTakip.WebUI.Models;

public class ProductDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string OemNumber { get; set; } = null!;
    public string BrandName { get; set; } = null!;
    public string BrandCode { get; set; } = null!;
    public decimal PurchasePrice { get; set; }
    public string ShelfNo { get; set; } = null!;
    public int StockQuantity { get; set; }
    public string? Barcode { get; set; }
    public string? BarcodeImgUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Hızlı istatistik
    public int TotalSoldQty { get; set; }           // bağımsız satış toplam miktar
    public int TotalUsedInServicesQty { get; set; } // servislerde kullanılan toplam miktar
}
