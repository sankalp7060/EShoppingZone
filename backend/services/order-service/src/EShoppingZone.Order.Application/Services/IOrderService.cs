using EShoppingZone.Order.Application.DTOs;

namespace EShoppingZone.Order.Application.Services
{
    public interface IOrderService
    {
        Task<OrderResponse> PlaceOrderAsync(int userId, PlaceOrderRequest request, string token);
        Task<WalletPaymentResponse> PlaceOrderWithWalletAsync(
            int userId,
            WalletPaymentRequest request,
            string token
        );
        Task<List<OrderResponse>> GetUserOrdersAsync(int userId);
        Task<OrderResponse?> GetOrderByIdAsync(int orderId, int userId);
        Task<OrderResponse> UpdateOrderStatusAsync(
            int orderId,
            string status,
            string userRole,
            string? remarks = null
        );
        Task<List<OrderResponse>> GetAllOrdersAsync(string userRole);
        Task<OrderTrackingResponse> GetOrderTrackingAsync(int orderId, int userId, string userRole);
        Task<OrderListResponse> GetFilteredOrdersAsync(
            int userId,
            OrderFilterRequest filter,
            string userRole
        );
        Task<bool> CancelOrderAsync(int orderId, int userId, string? reason = null);
    }
}
