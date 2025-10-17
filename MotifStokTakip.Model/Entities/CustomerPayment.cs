using MotifStokTakip.Model.Common;
using MotifStokTakip.Model.Enums;

namespace MotifStokTakip.Model.Entities
{
    // BaseEntity kullandığını biliyorum, oradan Id/CreatedAt vs. geliyorsa sorun yok.
    public class CustomerPayment : BaseEntity
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public PaymentDirection Direction { get; set; } = PaymentDirection.In;

        public decimal Amount { get; set; }       // her zaman (+) gir
        public string? Method { get; set; }       // Nakit/EFT/Kart vs.
        public string? Note { get; set; }

        // İstersen bunu kullan; yoksa BaseEntity.CreatedAt yeterlidir.
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }
}
