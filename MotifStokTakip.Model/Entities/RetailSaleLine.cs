using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities
{
    public class RetailSaleLine : BaseEntity
    {
        public int RetailSaleId { get; set; }
        public RetailSale RetailSale { get; set; } = default!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public string ItemName { get; set; } = "";
        public string? Barcode { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal BuyPrice { get; set; }   // Alış
        public decimal SellPrice { get; set; }  // Satış

        public decimal LineTotal => SellPrice * Quantity;
    }
}
