namespace MotifStokTakip.WebUI.Models
{
    public class ProductCreateViewModel
    {
        public string Name { get; set; } = null!;
        public string OemNumber { get; set; } = null!;
        public string BrandName { get; set; } = null!;
        public string BrandCode { get; set; } = null!;
        public decimal PurchasePrice { get; set; }
        public string ShelfNo { get; set; } = null!;
        public int StockQuantity { get; set; }

        // Opsiyonel: Barkod (scanner ile okutulabilir)
        public string? Barcode { get; set; }
    }


}
