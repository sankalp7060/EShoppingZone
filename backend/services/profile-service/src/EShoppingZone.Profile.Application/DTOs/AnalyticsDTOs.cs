namespace EShoppingZone.Profile.Application.DTOs
{
    public class DashboardAnalyticsResponse
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int SuspendedUsers { get; set; }
        public int Merchants { get; set; }
        public int Customers { get; set; }
        public int DeliveryAgents { get; set; }
        public int Admins { get; set; }
        public List<UserDto> RecentUsers { get; set; } = new();
        public DateTime LastUpdated { get; set; }

        // Order stats (would come from Order Service)
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RevenueAnalyticsResponse
    {
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
}
