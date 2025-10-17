// MotifStokTakip.WebUI/Models/ServiceInvoiceDetailViewModel.cs
namespace MotifStokTakip.WebUI.Models
{
    public class ServiceInvoiceDetailViewModel
    {
        public int InvoiceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ServiceOrderId { get; set; }
        public string CustomerName { get; set; } = "";
        public string VehiclePlate { get; set; } = "";
        public string VehicleBrandModel { get; set; } = "";
        public decimal Total { get; set; }
        public List<InvoiceLineVM> Items { get; set; } = new();
    }

    public class InvoiceLineVM
    {
        public string? Name { get; set; }
        public decimal Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
