namespace MotifStokTakip.WebUI.Models.Paging
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public string? Query { get; set; } // q gibi filtreleri pager'a geri yazmak için
    }
}
