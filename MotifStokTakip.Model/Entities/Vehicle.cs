namespace MotifStokTakip.Model.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }

        public string Plate { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public string Model { get; set; } = null!;

        public int? Year { get; set; }            // Yıl (opsiyonel)
        public string? Note { get; set; }         // Not (opsiyonel)

        public string? Vin { get; set; }          // Şasi No (VIN) – opsiyonel, istersen MaxLength(17) ekleyebilirsin

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
    }
}
