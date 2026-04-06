using EShoppingZone.Models.Entities;

namespace EShoppingZone.Data.Repositories
{
    public interface ICartRepository
    {
        Task<CartEntity?> GetByUserIdAsync(int userId);
        Task<CartEntity?> GetWithItemsAsync(int cartId);
        Task<CartEntity> CreateAsync(CartEntity cart);
        Task UpdateAsync(CartEntity cart);
        Task DeleteAsync(int cartId);
        Task<CartItemEntity?> GetCartItemAsync(int cartItemId);
        Task<bool> CartExistsAsync(int userId);
    }
}
