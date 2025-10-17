using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities;

public class ServiceInvoiceItem : BaseEntity
{
    public int ServiceInvoiceId { get; set; }
    public ServiceInvoice ServiceInvoice { get; set; } = null!;

    public int? ProductId { get; set; }           // stoktan geldiyse dolar, değilse null
    public Product? Product { get; set; }

    public string ItemName { get; set; } = null!; // ürün adı veya serbest satır
    public decimal CostPrice { get; set; }        // alış fiyatı
    public int Quantity { get; set; }
    public decimal SalePrice { get; set; }        // satış fiyatı (kullanıcı burada girer)
}
