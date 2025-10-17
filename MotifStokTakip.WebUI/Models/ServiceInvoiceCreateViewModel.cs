using System.ComponentModel.DataAnnotations;

namespace MotifStokTakip.WebUI.Models;

public class ServiceInvoiceCreateViewModel
{
    [Required] public int ServiceOrderId { get; set; }
    public List<ServiceInvoiceItemVM> Items { get; set; } = new();
}

public class ServiceInvoiceItemVM
{
    public int? ProductId { get; set; }          // barkoddan bulununca set edilir
    public string? ItemName { get; set; }        // serbest kalemde doldurulabilir
    public string? Barcode { get; set; }         // sadece UI için (server’da gerekmez)

    [Range(0, 1_000_000)] public decimal CostPrice { get; set; }  // ALIŞ (otomatik dolar)
    [Range(1, 1_000_000)] public int Quantity { get; set; } = 1;
    [Range(0, 1_000_000)] public decimal SalePrice { get; set; }  // SATIŞ (sen dolduracaksın)
}
