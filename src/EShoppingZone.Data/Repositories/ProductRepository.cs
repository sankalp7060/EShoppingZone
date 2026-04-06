using EShoppingZone.Data.Context;
using EShoppingZone.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync() =>
            await _context.Products.Where(p => p.IsActive).ToListAsync();

        public async Task<Product?> GetByIdAsync(int id) =>
            await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Product> AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await GetByIdAsync(id);
            if (product != null)
            {
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
                await UpdateAsync(product);
            }
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Products.AnyAsync(p => p.Id == id && p.IsActive);

        public IQueryable<Product> GetQueryable() => _context.Products.AsQueryable();
    }
}
