using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities
{
    public class StockMovement : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // pozitif: giriş, negatif: çıkış
        public decimal Quantity { get; set; }

        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Opsiyonel referans (fatura/satış vb.)
        public string? RefType { get; set; }   // "INV", "SALE", "RET", "ADJ"
        public int? RefId { get; set; }        // ilgili kaydın Id'si
    }
}
