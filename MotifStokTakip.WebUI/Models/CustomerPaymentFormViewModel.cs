using System.ComponentModel.DataAnnotations;
using MotifStokTakip.Model.Enums;

namespace MotifStokTakip.WebUI.Models
{
    public class CustomerPaymentFormViewModel
    {
        public int CustomerId { get; set; }

        [Required, Range(0.01, 9999999)]
        [Display(Name = "Tutar")]
        public decimal Amount { get; set; }

        [Display(Name = "Yön")]
        public PaymentDirection Direction { get; set; } = PaymentDirection.In;

        [MaxLength(40)]
        [Display(Name = "Yöntem")]
        public string? Method { get; set; }  // Nakit/EFT/Kart...

        [MaxLength(300)]
        [Display(Name = "Açıklama")]
        public string? Note { get; set; }
    }
}
