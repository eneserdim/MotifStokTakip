using MotifStokTakip.Model.Common;
using System.Collections.Generic;

namespace MotifStokTakip.Model.Entities
{
    public class RetailSale : BaseEntity
    {
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Kayıtlı olmayan müşteri ise
        public string? WalkInCustomerName { get; set; }

        public decimal Subtotal { get; set; }
        public decimal PaidAmount { get; set; }
        public bool IsPaid { get; set; }

        public ICollection<RetailSaleLine> Lines { get; set; } = new List<RetailSaleLine>();
        public ICollection<RetailPayment> Payments { get; set; } = new List<RetailPayment>();
    }
}
