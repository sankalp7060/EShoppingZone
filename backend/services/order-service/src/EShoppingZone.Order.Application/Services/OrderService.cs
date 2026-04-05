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
        private readonly IWalletServiceClient _walletServiceClient;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IProfileServiceClient profileServiceClient,
            ICartServiceClient cartServiceClient,
            IProductServiceClient productServiceClient,
            IWalletServiceClient walletServiceClient,
            ILogger<OrderService> logger
        )
        {
            _orderRepository = orderRepository;
            _profileServiceClient = profileServiceClient;
            _cartServiceClient = cartServiceClient;
            _productServiceClient = productServiceClient;
            _walletServiceClient = walletServiceClient;
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

        public async Task<WalletPaymentResponse> PlaceOrderWithWalletAsync(
            int userId,
            WalletPaymentRequest request,
            string token
        )
        {
            // 1. Get user's cart
            var cart = await _cartServiceClient.GetCartAsync(userId, token);
            if (cart == null || cart.Items == null || !cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            // 2. Check wallet balance first
            var walletBalance = await _walletServiceClient.GetWalletBalanceAsync(userId, token);
            if (walletBalance.CurrentBalance < cart.TotalPrice)
            {
                throw new InvalidOperationException(
                    $"Insufficient wallet balance. Available: {walletBalance.CurrentBalance:C}, Required: {cart.TotalPrice:C}"
                );
            }

            // 3. Get delivery address from Profile Service
            var address = await _profileServiceClient.GetAddressByIdAsync(
                request.AddressId,
                userId,
                token
            );
            if (address == null)
                throw new InvalidOperationException("Invalid delivery address");

            // 4. Update product stocks
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

            // 5. Create order first (to get OrderId for payment)
            var order = new OrderEntity
            {
                OrderId = 0,
                OrderDate = DateTime.UtcNow,
                CustomerId = userId,
                AmountPaid = cart.TotalPrice,
                ModeOfPayment = "EWALLET",
                OrderStatus = "Pending Payment", // Temporary status
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

            // 6. Process wallet payment
            var paymentResult = await _walletServiceClient.ProcessWalletPaymentAsync(
                userId,
                createdOrder.Id,
                cart.TotalPrice,
                token
            );

            if (!paymentResult.Success)
            {
                // Rollback - delete the order if payment fails
                await _orderRepository.UpdateOrderStatusAsync(createdOrder.Id, "Payment Failed");
                throw new InvalidOperationException(paymentResult.Message);
            }

            // 7. Update order status to confirmed after successful payment
            createdOrder.OrderStatus = "Placed";
            await _orderRepository.UpdateAsync(createdOrder);

            // 8. Clear the cart
            await _cartServiceClient.ClearCartAsync(userId, token);

            _logger.LogInformation(
                "Wallet Order placed successfully for user {UserId}, OrderId: {OrderId}, TransactionId: {TransactionId}",
                userId,
                createdOrder.Id,
                paymentResult.TransactionId
            );

            return new WalletPaymentResponse
            {
                Success = true,
                Message = "Order placed successfully using wallet",
                OrderId = createdOrder.Id,
                AmountPaid = cart.TotalPrice,
                WalletBalanceAfter = paymentResult.NewBalance,
                TransactionId = paymentResult.TransactionId,
            };
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
            string userRole,
            string? remarks = null
        )
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new InvalidOperationException("Order not found");

            var validStatuses = new[]
            {
                "Placed",
                "Confirmed",
                "Processing",
                "Shipped",
                "OutForDelivery",
                "Delivered",
                "Cancelled",
            };
            if (!validStatuses.Contains(status))
                throw new InvalidOperationException("Invalid order status");

            // Role-based validation
            if (userRole == "Customer")
            {
                // Customer can only cancel order if not shipped
                if (status != "Cancelled")
                    throw new UnauthorizedAccessException("Customers can only cancel orders");

                if (order.OrderStatus != "Placed" && order.OrderStatus != "Confirmed")
                    throw new InvalidOperationException(
                        "Cannot cancel order that is already shipped or delivered"
                    );
            }
            else if (userRole != "Admin")
            {
                throw new UnauthorizedAccessException("Only Admin can update order status");
            }

            var updatedOrder = await _orderRepository.UpdateOrderStatusAsync(
                orderId,
                status,
                userRole,
                remarks
            );
            if (updatedOrder == null)
                throw new InvalidOperationException("Failed to update order status");

            _logger.LogInformation(
                "Order {OrderId} status updated to {Status} by {UserRole}",
                orderId,
                status,
                userRole
            );

            return MapToOrderResponse(updatedOrder);
        }

        public async Task<List<OrderResponse>> GetAllOrdersAsync(string userRole)
        {
            if (userRole != "Admin")
                throw new UnauthorizedAccessException("Only Admin can view all orders");

            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToOrderResponse).ToList();
        }

        public async Task<OrderTrackingResponse> GetOrderTrackingAsync(
            int orderId,
            int userId,
            string userRole
        )
        {
            OrderEntity? order;

            if (userRole == "Admin")
            {
                order = await _orderRepository.GetByIdAsync(orderId);
            }
            else
            {
                order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.CustomerId != userId)
                    throw new UnauthorizedAccessException(
                        "You don't have permission to view this order"
                    );
            }

            if (order == null)
                throw new InvalidOperationException("Order not found");

            var statusHistory =
                order
                    .StatusHistory?.Select(h => new OrderStatusHistoryResponse
                    {
                        Id = h.Id,
                        Status = h.Status,
                        UpdatedAt = h.CreatedAt,
                        UpdatedBy = h.UpdatedBy,
                        Remarks = h.Remarks,
                    })
                    .ToList()
                ?? new List<OrderStatusHistoryResponse>();

            // Determine available actions based on current status
            var availableActions = new List<string>();
            var currentStatus = order.OrderStatus;

            if (userRole == "Admin")
            {
                if (currentStatus == "Placed")
                    availableActions.AddRange(new[] { "Confirm", "Cancel" });
                else if (currentStatus == "Confirmed")
                    availableActions.AddRange(new[] { "Process", "Cancel" });
                else if (currentStatus == "Processing")
                    availableActions.Add("Ship");
                else if (currentStatus == "Shipped")
                    availableActions.Add("OutForDelivery");
                else if (currentStatus == "OutForDelivery")
                    availableActions.Add("Deliver");
            }
            else if (userRole == "Customer")
            {
                if (currentStatus == "Placed" || currentStatus == "Confirmed")
                    availableActions.Add("Cancel");
            }

            return new OrderTrackingResponse
            {
                OrderId = order.Id,
                CurrentStatus = order.OrderStatus,
                OrderDate = order.OrderDate,
                EstimatedDeliveryDate = order.EstimatedDeliveryDate,
                StatusHistory = statusHistory,
                AvailableActions = availableActions,
            };
        }

        public async Task<OrderListResponse> GetFilteredOrdersAsync(
            int userId,
            OrderFilterRequest filter,
            string userRole
        )
        {
            int? customerId = null;

            if (userRole != "Admin")
            {
                customerId = userId;
            }

            var (orders, totalCount) = await _orderRepository.GetFilteredOrdersAsync(
                customerId: customerId,
                status: filter.Status,
                fromDate: filter.FromDate,
                toDate: filter.ToDate,
                minAmount: filter.MinAmount,
                maxAmount: filter.MaxAmount,
                page: filter.Page,
                pageSize: filter.PageSize,
                sortBy: filter.SortBy
            );

            return new OrderListResponse
            {
                Orders = orders.Select(MapToOrderResponse).ToList(),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
            };
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId, string? reason = null)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null)
                throw new InvalidOperationException("Order not found");

            if (order.CustomerId != userId)
                throw new UnauthorizedAccessException(
                    "You don't have permission to cancel this order"
                );

            if (order.OrderStatus != "Placed" && order.OrderStatus != "Confirmed")
                throw new InvalidOperationException(
                    "Cannot cancel order that is already shipped or delivered"
                );

            var updatedOrder = await _orderRepository.UpdateOrderStatusAsync(
                orderId,
                "Cancelled",
                "Customer",
                reason ?? "Cancelled by customer"
            );

            if (updatedOrder == null)
                throw new InvalidOperationException("Failed to cancel order");

            // If order was paid by wallet, refund the amount
            if (order.ModeOfPayment == "EWALLET")
            {
                // Call wallet service to refund
                _logger.LogInformation(
                    "Order {OrderId} cancelled. Wallet refund needed for amount {Amount}",
                    orderId,
                    order.AmountPaid
                );
            }

            _logger.LogInformation(
                "Order {OrderId} cancelled by customer {UserId}",
                orderId,
                userId
            );

            return true;
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
