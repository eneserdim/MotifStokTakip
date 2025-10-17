// MotifStokTakip.WebUI/Models/CustomerLedgerRow.cs
namespace MotifStokTakip.WebUI.Models
{
    public class CustomerLedgerRow
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = "";
        public string RefNo { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? Method { get; set; }
        public string? Note { get; set; }

        // >>> Link verebilmek için
        public int? ServiceOrderId { get; set; }
        public int? InvoiceId { get; set; }
        public int? SaleId { get; set; }
        public int? PaymentId { get; set; }
    }
}
