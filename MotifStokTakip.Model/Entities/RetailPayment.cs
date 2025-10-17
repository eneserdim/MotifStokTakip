using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities
{
    public class RetailPayment : BaseEntity
    {
        public int RetailSaleId { get; set; }
        public RetailSale RetailSale { get; set; } = default!;

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public decimal Amount { get; set; }
        public string Method { get; set; } = "Nakit";  // Nakit/Kredi/...
        public string? Note { get; set; }
    }
}
