using MotifStokTakip.Model.Enums;

namespace MotifStokTakip.WebUI.Models
{
    public class CustomerPaymentListItem
    {
        public int Id { get; set; }
        public DateTime PaidAt { get; set; }
        public decimal Amount { get; set; }
        public PaymentDirection Direction { get; set; }
        public string? Method { get; set; }
        public string? Note { get; set; }
    }
}
