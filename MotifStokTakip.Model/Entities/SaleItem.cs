namespace MotifStokTakip.Model.Entities;

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int? ProductId { get; set; }

    // Satır ismi (ürün adı ya da serbest kalem)
    public string ItemName { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Navigations
    public Sale Sale { get; set; } = null!;
    public Product? Product { get; set; }
}
