namespace EcommerceAPI.DTOs
{
    public class OrderPageDto
    {
        public IEnumerable<OrderDto> Items { get; set; } = Enumerable.Empty<OrderDto>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
