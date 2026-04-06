using EShoppingZone.Common.Constants;
using EShoppingZone.Data.Context;
using EShoppingZone.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderEntity> CreateAsync(OrderEntity order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            await AddStatusHistoryAsync(
                new OrderStatusHistoryEntity
                {
                    OrderId = order.Id,
                    Status = order.OrderStatus,
                    UpdatedBy = "System",
                    Remarks = "Order placed successfully",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            return order;
        }

        public async Task<OrderEntity?> GetByIdAsync(int id)
        {
            return await _context
                .Orders.Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id && o.IsActive);
        }

        public async Task<List<OrderEntity>> GetByCustomerIdAsync(int customerId)
        {
            return await _context
                .Orders.Include(o => o.StatusHistory)
                .Where(o => o.CustomerId == customerId && o.IsActive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<OrderEntity?> UpdateAsync(OrderEntity order)
        {
            _context.Entry(order).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<OrderEntity?> UpdateOrderStatusAsync(
            int orderId,
            string status,
            string? updatedBy = null,
            string? remarks = null
        )
        {
            var order = await GetByIdAsync(orderId);
            if (order == null)
                return null;

            var oldStatus = order.OrderStatus;
            order.OrderStatus = status;
            order.UpdatedAt = DateTime.UtcNow;

            switch (status.ToLower())
            {
                case "shipped":
                    order.ShippedDate = DateTime.UtcNow;
                    order.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(5);
                    break;
                case "delivered":
                    order.DeliveredDate = DateTime.UtcNow;
                    break;
                case "cancelled":
                    order.CancelledDate = DateTime.UtcNow;
                    order.CancellationReason = remarks;
                    break;
            }

            await UpdateAsync(order);

            await AddStatusHistoryAsync(
                new OrderStatusHistoryEntity
                {
                    OrderId = orderId,
                    Status = status,
                    UpdatedBy = updatedBy ?? "System",
                    Remarks = remarks ?? $"Status changed from {oldStatus} to {status}",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            return order;
        }

        public async Task<List<OrderEntity>> GetAllAsync()
        {
            return await _context
                .Orders.Include(o => o.StatusHistory)
                .Where(o => o.IsActive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int orderId)
        {
            return await _context.Orders.AnyAsync(o => o.Id == orderId && o.IsActive);
        }

        public async Task<(List<OrderEntity> Orders, int TotalCount)> GetFilteredOrdersAsync(
            int? customerId = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "newest"
        )
        {
            var query = _context.Orders.Include(o => o.StatusHistory).Where(o => o.IsActive);

            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId.Value);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.OrderStatus == status);
            if (fromDate.HasValue)
                query = query.Where(o => o.OrderDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(o => o.OrderDate <= toDate.Value);
            if (minAmount.HasValue)
                query = query.Where(o => o.AmountPaid >= minAmount.Value);
            if (maxAmount.HasValue)
                query = query.Where(o => o.AmountPaid <= maxAmount.Value);

            query = sortBy?.ToLower() switch
            {
                "oldest" => query.OrderBy(o => o.OrderDate),
                "amount_asc" => query.OrderBy(o => o.AmountPaid),
                "amount_desc" => query.OrderByDescending(o => o.AmountPaid),
                "status" => query.OrderBy(o => o.OrderStatus),
                _ => query.OrderByDescending(o => o.OrderDate),
            };

            var totalCount = await query.CountAsync();
            var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (orders, totalCount);
        }

        public async Task<OrderStatusHistoryEntity> AddStatusHistoryAsync(
            OrderStatusHistoryEntity history
        )
        {
            await _context.OrderStatusHistories.AddAsync(history);
            await _context.SaveChangesAsync();
            return history;
        }

        public async Task<List<OrderStatusHistoryEntity>> GetStatusHistoryAsync(int orderId)
        {
            return await _context
                .OrderStatusHistories.Where(h => h.OrderId == orderId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }
    }
}
