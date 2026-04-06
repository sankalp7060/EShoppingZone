using EShoppingZone.Data.Context;
using EShoppingZone.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Data.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;

        public CartRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CartEntity?> GetByUserIdAsync(int userId)
        {
            return await _context
                .Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
        }

        public async Task<CartEntity?> GetWithItemsAsync(int cartId)
        {
            return await _context
                .Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cartId && c.IsActive);
        }

        public async Task<CartEntity> CreateAsync(CartEntity cart)
        {
            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task UpdateAsync(CartEntity cart)
        {
            _context.Entry(cart).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int cartId)
        {
            var cart = await GetWithItemsAsync(cartId);
            if (cart != null)
            {
                cart.IsActive = false;
                await UpdateAsync(cart);
            }
        }

        public async Task<CartItemEntity?> GetCartItemAsync(int cartItemId)
        {
            return await _context.CartItems.FirstOrDefaultAsync(ci => ci.Id == cartItemId);
        }

        public async Task<bool> CartExistsAsync(int userId)
        {
            return await _context.Carts.AnyAsync(c => c.UserId == userId && c.IsActive);
        }
    }
}
