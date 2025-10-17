using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotifStokTakip.Model.Entities;
using MotifStokTakip.Service.Data;

namespace MotifStokTakip.WebUI.Controllers
{
    public class ServiceInvoicesController : Controller
    {
        private readonly AppDbContext _db;
        public ServiceInvoicesController(AppDbContext db) => _db = db;

        // Liste: Admin/Muhasebe -> yalnızca ödemesi yapılmışlar
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IQueryable<ServiceInvoice> q = _db.ServiceInvoices
                .Include(x => x.ServiceOrder).ThenInclude(o => o.Customer);

            if (User.IsInRole("Admin") || User.IsInRole("Muhasebe"))
                q = q.Where(x => x.IsPaid);

            var list = await q
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .ToListAsync();

            return View(list);
        }

        // Ödeme durumunu işaretle / kaldır
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Muhasebe")]
        public async Task<IActionResult> SetPaid(int id, bool paid = true)
        {
            var inv = await _db.ServiceInvoices
                               .Include(x => x.ServiceOrder)
                               .FirstOrDefaultAsync(x => x.Id == id);
            if (inv == null) return NotFound();

            inv.IsPaid = paid;
            _db.ServiceInvoices.Update(inv);
            await _db.SaveChangesAsync();

            // Servis detayına geri dön (kullandığınız route buysa)
            return RedirectToAction("Details", "ServiceOrders", new { id = inv.ServiceOrderId });
        }
    }
}
