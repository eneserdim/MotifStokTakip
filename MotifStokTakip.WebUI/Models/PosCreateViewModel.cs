using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MotifStokTakip.WebUI.Models
{
    public class PosCreateViewModel
    {
        public int? CustomerId { get; set; }
        public string? WalkInCustomerName { get; set; }

        // BAŞLANGIÇTA BOŞ!
        public List<PosLineViewModel> Items { get; set; } = new();

        [Display(Name = "Ara Toplam")]
        public decimal Subtotal => Items.Sum(i => i.SellPrice * i.Quantity);

        [Display(Name = "Ödenen")]
        public decimal PaidAmount { get; set; }

        public string PaymentMethod { get; set; } = "Nakit";
    }

    public class PosLineViewModel
    {
        public int? ProductId { get; set; }
        public string? Barcode { get; set; }
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
    }

    public class RetailSaleListVm
    {
        public int Id { get; init; }
        public System.DateTime CreatedAt { get; init; }
        public string? Customer { get; init; }
        public decimal Subtotal { get; init; }
        public bool IsPaid { get; init; }
    }
}
