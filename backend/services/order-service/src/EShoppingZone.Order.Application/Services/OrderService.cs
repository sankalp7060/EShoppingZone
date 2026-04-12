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

            // 3. Get User Profile for CustomerName
            var profile = await _profileServiceClient.GetProfileAsync(token);

            // 4. Update product stocks with FINAL RE-CHECK
            foreach (var item in cart.Items)
            {
                var product = await _productServiceClient.GetProductAsync(item.ProductId, token);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for product: {item.ProductName}. Only {product?.StockQuantity ?? 0} remaining.");
                }

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

            // 4. Create order items with MerchantId fetched from Product Service
            var orderItems = new List<OrderItemEntity>();
            foreach (var item in cart.Items)
            {
                var product = await _productServiceClient.GetProductAsync(item.ProductId, token);
                orderItems.Add(new OrderItemEntity
                {
                    ProductId = item.ProductId,
                    MerchantId = product?.MerchantId ?? 0, // Fallback to 0 if not found
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    Subtotal = item.Subtotal,
                    ImageUrl = item.ImageUrl,
                });
            }

            var order = new OrderEntity
            {
                OrderId = 0, // Will be auto-generated
                OrderDate = DateTime.UtcNow,
                CustomerId = userId,
                CustomerName = profile?.FullName ?? "Unknown",
                MerchantId = orderItems.FirstOrDefault()?.MerchantId ?? 0,
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
                OrderItems = orderItems,
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

            // 4. Get User Profile for CustomerName
            var profile = await _profileServiceClient.GetProfileAsync(token);

            // 5. Update product stocks with FINAL RE-CHECK
            foreach (var item in cart.Items)
            {
                var product = await _productServiceClient.GetProductAsync(item.ProductId, token);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for product: {item.ProductName}. Only {product?.StockQuantity ?? 0} remaining.");
                }

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

            // 5. Create order items with MerchantId fetched from Product Service
            var orderItems = new List<OrderItemEntity>();
            foreach (var item in cart.Items)
            {
                var product = await _productServiceClient.GetProductAsync(item.ProductId, token);
                orderItems.Add(new OrderItemEntity
                {
                    ProductId = item.ProductId,
                    MerchantId = product?.MerchantId ?? 0,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    Subtotal = item.Subtotal,
                    ImageUrl = item.ImageUrl,
                });
            }

            var order = new OrderEntity
            {
                OrderId = 0,
                OrderDate = DateTime.UtcNow,
                CustomerId = userId,
                CustomerName = profile?.FullName ?? "Unknown",
                MerchantId = orderItems.FirstOrDefault()?.MerchantId ?? 0,
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
                OrderItems = orderItems,
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

            // RELEASE FUNDS TO MERCHANT
            if (createdOrder.MerchantId > 0)
            {
                await _walletServiceClient.CreditMerchantAsync(
                    createdOrder.MerchantId, 
                    createdOrder.Id, 
                    createdOrder.AmountPaid, 
                    token
                );
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
            int userId,
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
                "Packed",
                "Processing",
                "Shipped",
                "OutForDelivery",
                "Delivered",
                "Cancelled",
                "Failed"
            };
            if (!validStatuses.Contains(status))
                throw new InvalidOperationException("Invalid order status");

            // Pipeline sequence validation (#21)
            var currentStatus = order.OrderStatus;
            if (status == "Confirmed" && currentStatus != "Placed")
                throw new InvalidOperationException("Order must be in 'Placed' status to be confirmed.");
            if (status == "Packed" && currentStatus != "Confirmed")
                throw new InvalidOperationException("Order must be in 'Confirmed' status to be packed.");
            if (status == "Shipped" && currentStatus != "Packed")
                throw new InvalidOperationException("Order must be in 'Packed' status to be shipped.");
            if (status == "OutForDelivery" && currentStatus != "Shipped")
                throw new InvalidOperationException("Order must be in 'Shipped' status to be dispatched.");
            if (status == "Delivered" && currentStatus != "OutForDelivery")
                throw new InvalidOperationException("Order must be in 'OutForDelivery' status to be marked as delivered.");
            
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
            else if (userRole == "Merchant")
            {
                // Merchant can only update their own orders
                if (order.MerchantId != userId && !order.OrderItems.Any(i => i.MerchantId == userId))
                {
                    throw new UnauthorizedAccessException("Merchants can only update their own orders");
                }

                // Merchants: Confirmed, Packed, Shipped, Cancelled
                var merchantStatuses = new[] { "Confirmed", "Packed", "Shipped", "Cancelled" };
                if (!merchantStatuses.Contains(status))
                    throw new UnauthorizedAccessException("Merchants can only update to Confirmed, Packed, Shipped, or Cancelled");
            }
            else if (userRole == "DeliveryAgent")
            {
                // DeliveryPartner: OutForDelivery, Delivered, Failed
                var deliveryStatuses = new[] { "OutForDelivery", "Delivered", "Failed" };
                if (!deliveryStatuses.Contains(status))
                    throw new UnauthorizedAccessException("Delivery Partners can only update to OutForDelivery, Delivered, or Failed");

                if (order.DeliveryAgentId != null && order.DeliveryAgentId != userId)
                {
                    throw new UnauthorizedAccessException("Another delivery agent has already picked up this order.");
                }

                if (status == "OutForDelivery" && order.DeliveryAgentId == null)
                {
                    order.DeliveryAgentId = userId;
                    // Note: UpdateOrderStatusAsync in repo will just update status. 
                    // We need to make sure order is explicitly updated.
                    await _orderRepository.UpdateAsync(order);
                }
            }
            else if (userRole != "Admin")
            {
                throw new UnauthorizedAccessException("Only Admin, Merchants and Delivery Agents can update order status");
            }

            // Admin: Allowed Confirmed, Packed, Shipped, Cancelled as well
            if (userRole == "Admin")
            {
                var adminStatuses = new[] { "Confirmed", "Packed", "Shipped", "Cancelled" };
                if (!adminStatuses.Contains(status))
                     throw new UnauthorizedAccessException("Admin status updates are restricted to logistics flow (Confirmed, Packed, Shipped, Cancelled)");
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
                DeliveryAgentId = order.DeliveryAgentId,
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

            if (userRole != "Admin" && userRole != "DeliveryAgent")
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

        public async Task<bool> CancelOrderAsync(
            int orderId,
            int userId,
            string token,
            string? reason = null
        )
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
                    "Order {OrderId} cancelled. Refunding {Amount} to user {UserId} wallet.",
                    orderId,
                    order.AmountPaid,
                    userId
                );

                var refundResult = await _walletServiceClient.RefundWalletPaymentAsync(
                    userId,
                    orderId,
                    order.AmountPaid,
                    token
                );

                if (!refundResult.Success)
                {
                    _logger.LogError(
                        "FAILED to refund wallet for order {OrderId}. User: {UserId}, Amount: {Amount}. Error: {Error}",
                        orderId,
                        userId,
                        order.AmountPaid,
                        refundResult.Message
                    );
                    // Note: We don't throw here to ensure the order stays cancelled, 
                    // but in a production app, we would use a background job/retry mechanism.
                }
            }

            _logger.LogInformation(
                "Order {OrderId} cancelled by customer {UserId}",
                orderId,
                userId
            );

            return true;
        }

        public async Task<OrderListResponse> GetFilteredOrdersByMerchantAsync(
            int merchantId,
            OrderFilterRequest filter
        )
        {
            var (orders, totalCount) = await _orderRepository.GetFilteredOrdersAsync(
                merchantId: merchantId,
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

        private OrderResponse MapToOrderResponse(OrderEntity order)
        {
            return new OrderResponse
            {
                OrderId = order.Id,
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName,
                AmountPaid = order.AmountPaid,
                ModeOfPayment = order.ModeOfPayment,
                OrderStatus = order.OrderStatus,
                Quantity = order.Quantity,
                DeliveryAgentId = order.DeliveryAgentId,
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
                        MerchantId = i.MerchantId,
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
