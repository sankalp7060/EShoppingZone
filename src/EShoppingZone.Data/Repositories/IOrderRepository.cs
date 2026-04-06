using EShoppingZone.Models.Entities;

namespace EShoppingZone.Data.Repositories
{
    public interface IOrderRepository
    {
        Task<OrderEntity> CreateAsync(OrderEntity order);
        Task<OrderEntity?> GetByIdAsync(int id);
        Task<List<OrderEntity>> GetByCustomerIdAsync(int customerId);
        Task<OrderEntity?> UpdateAsync(OrderEntity order);
        Task<OrderEntity?> UpdateOrderStatusAsync(
            int orderId,
            string status,
            string? updatedBy = null,
            string? remarks = null
        );
        Task<List<OrderEntity>> GetAllAsync();
        Task<bool> ExistsAsync(int orderId);
        Task<(List<OrderEntity> Orders, int TotalCount)> GetFilteredOrdersAsync(
            int? customerId = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "newest"
        );
        Task<OrderStatusHistoryEntity> AddStatusHistoryAsync(OrderStatusHistoryEntity history);
        Task<List<OrderStatusHistoryEntity>> GetStatusHistoryAsync(int orderId);
    }
}
