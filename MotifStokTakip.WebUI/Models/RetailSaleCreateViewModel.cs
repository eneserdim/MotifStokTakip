using System.ComponentModel.DataAnnotations;

namespace MotifStokTakip.WebUI.Models
{
    public class RetailSaleCreateViewModel
    {
        public int? CustomerId { get; set; }         // null => Sisteme Kayıtlı Olmayan Cari
        public string? CustomerName { get; set; }    // sadece UI

        public RetailPaymentOption Payment { get; set; } = RetailPaymentOption.Cash; // Cash | OnAccount
        public string? PaymentMethod { get; set; } = "Nakit"; // Nakit/Kart/EFT vb.

        public List<RetailSaleLineVM> Items { get; set; } = new();

        [Display(Name = "Kaydettikten sonra yazdır")]
        public bool PrintAfterSave { get; set; } = false;
    }

    public class RetailSaleLineVM
    {
        public int? ProductId { get; set; }    // barkoddan bulunur
        public string? Barcode { get; set; }
        public string? Name { get; set; }      // ürün adı (UI için)
        [Range(1, 1_000_000)]
        public decimal Quantity { get; set; } = 1;
        [Range(0, 1_000_000)]
        public decimal UnitPrice { get; set; } = 0; // kullanıcı belirler (boşsa ürün satış/alış fiyatı)
    }

    public enum RetailPaymentOption
    {
        Cash = 0,      // ödeme alındı
        OnAccount = 1  // veresiye/yazdır
    }
}
