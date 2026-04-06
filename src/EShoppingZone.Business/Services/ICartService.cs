using EShoppingZone.Business.DTOs;

namespace EShoppingZone.Business.Services
{
    public interface ICartService
    {
        Task<CartResponse> GetCartAsync(int userId);
        Task<CartResponse> AddToCartAsync(int userId, AddToCartRequest request);
        Task<CartResponse> UpdateCartItemAsync(int userId, UpdateCartItemRequest request);
        Task<CartResponse> RemoveFromCartAsync(int userId, int cartItemId);
        Task<bool> ClearCartAsync(int userId);
        Task<CartSummaryResponse> GetCartSummaryAsync(int userId);
        Task<int> GetCartItemCountAsync(int userId);
    }
}
