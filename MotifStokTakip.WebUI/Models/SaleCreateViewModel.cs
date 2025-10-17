using System.ComponentModel.DataAnnotations;

namespace MotifStokTakip.WebUI.Models;

public class SaleCreateViewModel
{
    public List<SaleItemVM> Items { get; set; } = new() { new() };
}

public class SaleItemVM
{
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Barcode { get; set; }

    [Range(1, 1_000_000)]
    public int Quantity { get; set; } = 1;

    [Range(0, 1_000_000)]
    public decimal UnitPrice { get; set; } = 0;
}
