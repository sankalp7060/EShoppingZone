using EShoppingZone.Product.Domain.Entities;
using EShoppingZone.Product.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Product.Infrastructure.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<ProductEntity>> GetAllAsync();
        Task<ProductEntity?> GetByIdAsync(int id);
        Task<ProductEntity> AddAsync(ProductEntity product);
        Task UpdateAsync(ProductEntity product);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        IQueryable<ProductEntity> GetQueryable();
    }

    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductEntity>> GetAllAsync() =>
            await _context.Products.Where(p => p.IsActive).ToListAsync();

        public async Task<ProductEntity?> GetByIdAsync(int id) =>
            await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        public async Task<ProductEntity> AddAsync(ProductEntity product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task UpdateAsync(ProductEntity product)
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

        public IQueryable<ProductEntity> GetQueryable() => _context.Products.AsQueryable();
    }
}
