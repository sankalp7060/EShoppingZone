using EShoppingZone.Order.Application.DTOs;
using EShoppingZone.Order.Domain.Entities;
using EShoppingZone.Order.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Order.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProfileServiceClient _profileServiceClient;
        private readonly ICartServiceClient _cartServiceClient;
        private readonly IProductServiceClient _productServiceClient;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IProfileServiceClient profileServiceClient,
            ICartServiceClient cartServiceClient,
            IProductServiceClient productServiceClient,
            ILogger<OrderService> logger
        )
        {
            _orderRepository = orderRepository;
            _profileServiceClient = profileServiceClient;
            _cartServiceClient = cartServiceClient;
            _productServiceClient = productServiceClient;
            _logger = logger;
        }

        public async Task<OrderResponse> PlaceOrderAsync(
            int userId,
            PlaceOrderRequest request,
            string token
        )
        {
            // 1. Get user's cart
            var cart = await _cartServiceClient.GetCartAsync(userId, token);
            if (cart == null || cart.Items == null || !cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            // 2. Get delivery address from Profile Service
            var address = await _profileServiceClient.GetAddressByIdAsync(
                request.AddressId,
                userId,
                token
            );
            if (address == null)
                throw new InvalidOperationException("Invalid delivery address");

            // 3. Update product stocks
            foreach (var item in cart.Items)
            {
                var stockUpdated = await _productServiceClient.UpdateStockAsync(
                    item.ProductId,
                    -item.Quantity,
                    token
                );
                if (!stockUpdated)
                    throw new InvalidOperationException(
                        $"Insufficient stock for product: {item.ProductName}"
                    );
            }

            // 4. Create order
            var order = new OrderEntity
            {
                OrderId = 0, // Will be auto-generated
                OrderDate = DateTime.UtcNow,
                CustomerId = userId,
                AmountPaid = cart.TotalPrice,
                ModeOfPayment = request.ModeOfPayment,
                OrderStatus = "Placed",
                Quantity = cart.TotalItems,
                AddressHouseNumber = address.HouseNumber,
                AddressStreetName = address.StreetName,
                AddressColonyName = address.ColonyName ?? string.Empty,
                AddressCity = address.City,
                AddressState = address.State,
                AddressPincode = address.Pincode,
                AddressLandmark = address.Landmark ?? string.Empty,
                OrderItems = cart
                    .Items.Select(i => new OrderItemEntity
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Price = i.Price,
                        Quantity = i.Quantity,
                        Subtotal = i.Subtotal,
                        ImageUrl = i.ImageUrl,
                    })
                    .ToList(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var createdOrder = await _orderRepository.CreateAsync(order);

            // 5. Clear the cart
            await _cartServiceClient.ClearCartAsync(userId, token);

            _logger.LogInformation(
                "Order placed successfully for user {UserId}, OrderId: {OrderId}",
                userId,
                createdOrder.Id
            );

            return MapToOrderResponse(createdOrder);
        }

        public async Task<List<OrderResponse>> GetUserOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetByCustomerIdAsync(userId);
            return orders.Select(MapToOrderResponse).ToList();
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(int orderId, int userId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.CustomerId != userId)
                return null;

            return MapToOrderResponse(order);
        }

        public async Task<OrderResponse> UpdateOrderStatusAsync(
            int orderId,
            string status,
            string userRole
        )
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new InvalidOperationException("Order not found");

            // Validate status transition
            var validStatuses = new[] { "Placed", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(status))
                throw new InvalidOperationException("Invalid order status");

            // Only Admin can update status beyond certain point
            if (userRole != "Admin" && status != "Cancelled")
                throw new UnauthorizedAccessException("Only Admin can update order status");

            // Customer can only cancel if order is not shipped yet
            if (userRole == "Customer" && status == "Cancelled" && order.OrderStatus != "Placed")
                throw new InvalidOperationException(
                    "Cannot cancel order that is already shipped or delivered"
                );

            order.OrderStatus = status;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation(
                "Order {OrderId} status updated to {Status} by {UserRole}",
                orderId,
                status,
                userRole
            );

            return MapToOrderResponse(order);
        }

        public async Task<List<OrderResponse>> GetAllOrdersAsync(string userRole)
        {
            if (userRole != "Admin")
                throw new UnauthorizedAccessException("Only Admin can view all orders");

            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToOrderResponse).ToList();
        }

        private OrderResponse MapToOrderResponse(OrderEntity order)
        {
            return new OrderResponse
            {
                OrderId = order.Id,
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                AmountPaid = order.AmountPaid,
                ModeOfPayment = order.ModeOfPayment,
                OrderStatus = order.OrderStatus,
                Quantity = order.Quantity,
                Address = new AddressSnapshot
                {
                    HouseNumber = order.AddressHouseNumber,
                    StreetName = order.AddressStreetName,
                    ColonyName = order.AddressColonyName,
                    City = order.AddressCity,
                    State = order.AddressState,
                    Pincode = order.AddressPincode,
                    Landmark = order.AddressLandmark,
                },
                Items = order
                    .OrderItems.Select(i => new OrderItemResponse
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Price = i.Price,
                        Quantity = i.Quantity,
                        Subtotal = i.Subtotal,
                        ImageUrl = i.ImageUrl,
                    })
                    .ToList(),
            };
        }
    }
}
