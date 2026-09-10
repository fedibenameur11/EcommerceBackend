namespace EcommerceAPI.DTOs
{
    public class ProductPageDto
    {
        public IEnumerable<ProductDto> Items { get; set; } = Enumerable.Empty<ProductDto>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
