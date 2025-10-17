using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities;

public class Sale : BaseEntity
{
    public int? CustomerId { get; set; }          // carisiz perakende satış da olabilir
    public Customer? Customer { get; set; }

    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; } = true;      // kasada anlık ödeme varsayımı
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
