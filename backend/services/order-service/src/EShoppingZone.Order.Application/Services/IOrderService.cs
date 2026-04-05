using EShoppingZone.Order.Application.DTOs;

namespace EShoppingZone.Order.Application.Services
{
    public interface IOrderService
    {
        Task<OrderResponse> PlaceOrderAsync(int userId, PlaceOrderRequest request, string token);
        Task<List<OrderResponse>> GetUserOrdersAsync(int userId);
        Task<OrderResponse?> GetOrderByIdAsync(int orderId, int userId);
        Task<OrderResponse> UpdateOrderStatusAsync(int orderId, string status, string userRole);
        Task<List<OrderResponse>> GetAllOrdersAsync(string userRole);
    }
}
