using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities;

public class ServiceInvoice : BaseEntity
{
    public int ServiceOrderId { get; set; }
    public ServiceOrder ServiceOrder { get; set; } = null!;

    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; } = false;

    public ICollection<ServiceInvoiceItem> Items { get; set; } = new List<ServiceInvoiceItem>();
}
