namespace EcommerceAPI.DTOs
{
    public class RevenuePointDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class LowStockProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    public class AnalyticsDto
    {
        public string Currency { get; set; } = "TND";
        public int Days { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        /// <summary>Nombre d'articles vendus sur la période.</summary>
        public int ItemsSold { get; set; }
        public List<RevenuePointDto> RevenueOverTime { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<LowStockProductDto> LowStock { get; set; } = new();
        public int LowStockThreshold { get; set; }
    }
}