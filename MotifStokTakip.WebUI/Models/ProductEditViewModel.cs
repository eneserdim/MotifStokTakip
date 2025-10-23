namespace MotifStokTakip.WebUI.Models
{
    public class ProductEditViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string OemNumber { get; set; } = null!;
        public string BrandName { get; set; } = null!;
        public string BrandCode { get; set; } = null!;
        public decimal PurchasePrice { get; set; }
        public string ShelfNo { get; set; } = null!;
        public int StockQuantity { get; set; }

        // Opsiyonel: Eldeki barkodu saklayalım
        public string? Barcode { get; set; }
        
        // Kimden Alındı
        public string? PurchasedFrom { get; set; }
    }



}