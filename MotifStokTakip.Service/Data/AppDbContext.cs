using Microsoft.EntityFrameworkCore;
using MotifStokTakip.Model.Entities;
using MotifStokTakip.Model.Enums;

namespace MotifStokTakip.Service.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<ServicePhoto> ServicePhotos => Set<ServicePhoto>();
    public DbSet<ServiceInvoice> ServiceInvoices => Set<ServiceInvoice>();
    public DbSet<ServiceInvoiceItem> ServiceInvoiceItems => Set<ServiceInvoiceItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<RetailSale> RetailSales => Set<RetailSale>();
    public DbSet<RetailSaleLine> RetailSaleLines => Set<RetailSaleLine>();
    public DbSet<RetailPayment> RetailPayments => Set<RetailPayment>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<ServiceOrderTechnician> ServiceOrderTechnicians => Set<ServiceOrderTechnician>();



    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // AppUser

        // ---- SEED: İlk Admin kullanıcısı ----
        // Parola: Admin123!  (SHA256 hash)
        // 3eb3fe66b31e3b4d10fa70b5cad49c7112294af6ae4e476a1c405155d45aa121
        const string p = "3eb3fe66b31e3b4d10fa70b5cad49c7112294af6ae4e476a1c405155d45aa121";

        b.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = 1,
                FullName = "Sistem Yöneticisi",
                UserName = "admin",
                PasswordHash = p,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new AppUser
            {
                Id = 2,
                FullName = "Muhasebe Kullanıcısı",
                UserName = "muhasebe",
                PasswordHash = p,
                Role = UserRole.Muhasebe,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new AppUser
            {
                Id = 3,
                FullName = "Usta Kullanıcısı",
                UserName = "usta",
                PasswordHash = p,
                Role = UserRole.Usta,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        // Customer
        b.Entity<Customer>()
         .HasMany(x => x.Vehicles)
         .WithOne(v => v.Customer)
         .HasForeignKey(v => v.CustomerId)
         .OnDelete(DeleteBehavior.Cascade);

        // Vehicle
        b.Entity<Vehicle>()
            .HasIndex(x => x.Plate)
            .IsUnique();

        // ServiceOrder
        b.Entity<ServiceOrder>()
         .HasOne(x => x.Customer)
         .WithMany(c => c.ServiceOrders)
         .HasForeignKey(x => x.CustomerId)
         .OnDelete(DeleteBehavior.Restrict); // veya .NoAction()

        b.Entity<ServiceOrder>()
         .HasOne(x => x.Vehicle)
         .WithMany(v => v.ServiceOrders)
         .HasForeignKey(x => x.VehicleId)
         .OnDelete(DeleteBehavior.Restrict); // veya .NoAction()

        b.Entity<ServiceOrder>()
         .HasOne(x => x.AssignedUser)
         .WithMany()
         .HasForeignKey(x => x.AssignedUserId)
         .OnDelete(DeleteBehavior.SetNull);

        // ServicePhoto
        b.Entity<ServicePhoto>()
            .HasOne(p => p.ServiceOrder)
            .WithMany(o => o.Photos)
            .HasForeignKey(p => p.ServiceOrderId);

        // ServiceInvoice & Item
        b.Entity<ServiceInvoice>()
            .HasOne(i => i.ServiceOrder)
            .WithMany(o => o.Invoices)
            .HasForeignKey(i => i.ServiceOrderId);

        b.Entity<ServiceInvoiceItem>()
            .HasOne(i => i.ServiceInvoice)
            .WithMany(inv => inv.Items)
            .HasForeignKey(i => i.ServiceInvoiceId);

        b.Entity<ServiceInvoiceItem>()
            .HasOne(i => i.Product)
            .WithMany(p => p.ServiceInvoiceItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<Product>()
 .HasIndex(p => p.Barcode)
 .IsUnique()
 .HasFilter("[Barcode] IS NOT NULL");


        // Sale & Item
        b.Entity<Sale>()
            .HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<SaleItem>()
            .HasOne(i => i.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.SaleId);

        b.Entity<SaleItem>()
            .HasOne(i => i.Product)
            .WithMany(p => p.SaleItems)
            .HasForeignKey(i => i.ProductId);


        // StockMovement <-> Product
        b.Entity<StockMovement>()
         .HasOne(m => m.Product)
         .WithMany(p => p.StockMovements)   // <— bu satır Product’ta navigasyon ister
         .HasForeignKey(m => m.ProductId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Entity<StockMovement>()
         .Property(m => m.CreatedAt)
         .HasDefaultValueSql("GETUTCDATE()");

        // Product.RowVersion
        b.Entity<Product>()
         .Property(p => p.RowVersion)
         .IsRowVersion();

        b.Entity<RetailSale>(e =>
        {
            e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
            e.HasMany(x => x.Lines)
             .WithOne(x => x.RetailSale)
             .HasForeignKey(x => x.RetailSaleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RetailSaleLine>(e =>
        {
            e.Property(x => x.BuyPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.SellPrice).HasColumnType("decimal(18,2)");
        });

        b.Entity<RetailPayment>(e =>
        {
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        });

        b.Entity<ServiceOrderTechnician>()
 .HasIndex(x => new { x.ServiceOrderId, x.TechnicianId })
 .IsUnique();

        b.Entity<ServiceOrderTechnician>()
         .HasOne(x => x.ServiceOrder)
         .WithMany(o => o.Technicians)              // ServiceOrder tarafına aşağıdaki navigasyonu ekliyoruz
         .HasForeignKey(x => x.ServiceOrderId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ServiceOrderTechnician>()
         .HasOne(x => x.Technician)
         .WithMany(t => t.ServiceOrders)
         .HasForeignKey(x => x.TechnicianId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
