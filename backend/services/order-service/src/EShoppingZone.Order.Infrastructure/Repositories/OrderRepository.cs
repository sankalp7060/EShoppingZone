using EShoppingZone.Order.Domain.Entities;
using EShoppingZone.Order.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Order.Infrastructure.Repositories
{
    public interface IOrderRepository
    {
        Task<OrderEntity> CreateAsync(OrderEntity order);
        Task<OrderEntity?> GetByIdAsync(int id);
        Task<List<OrderEntity>> GetByCustomerIdAsync(int customerId);
        Task<OrderEntity?> UpdateAsync(OrderEntity order);
        Task<List<OrderEntity>> GetAllAsync();
        Task<bool> ExistsAsync(int orderId);
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OrderEntity> CreateAsync(OrderEntity order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<OrderEntity?> GetByIdAsync(int id)
        {
            return await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.IsActive);
        }

        public async Task<List<OrderEntity>> GetByCustomerIdAsync(int customerId)
        {
            return await _context
                .Orders.Where(o => o.CustomerId == customerId && o.IsActive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<OrderEntity?> UpdateAsync(OrderEntity order)
        {
            _context.Entry(order).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<List<OrderEntity>> GetAllAsync()
        {
            return await _context
                .Orders.Where(o => o.IsActive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int orderId)
        {
            return await _context.Orders.AnyAsync(o => o.Id == orderId && o.IsActive);
        }
    }
}
