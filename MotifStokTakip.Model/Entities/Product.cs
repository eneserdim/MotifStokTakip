using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = null!;
    public string OemNumber { get; set; } = null!;
    public string BrandName { get; set; } = null!;
    public string BrandCode { get; set; } = null!;
    public decimal PurchasePrice { get; set; }            // Alış Fiyatı
    public string? Barcode { get; set; }                  // Manuel girilmezse üretilecek
    public string ShelfNo { get; set; } = null!;
    public int StockQuantity { get; set; }                // Stok Miktarı
    public string? PurchasedFrom { get; set; }            // Kimden Alındı

    public byte[]? RowVersion { get; set; }   // concurrency token

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    // İlişkiler
    public ICollection<ServiceInvoiceItem>? ServiceInvoiceItems { get; set; }
    public ICollection<SaleItem>? SaleItems { get; set; }
}
