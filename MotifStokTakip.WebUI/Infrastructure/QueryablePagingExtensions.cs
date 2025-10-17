using Microsoft.EntityFrameworkCore;
using MotifStokTakip.WebUI.Models.Paging;

namespace MotifStokTakip.WebUI.Infrastructure
{
    public static class QueryablePagingExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query, int page, int pageSize, string? q = null)
        {
            if (page < 1) page = 1;
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                Query = q
            };
        }
    }
}
