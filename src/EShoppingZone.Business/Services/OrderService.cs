using System.Text.Json;
using EShoppingZone.Business.DTOs;
using EShoppingZone.Common.Constants;
using EShoppingZone.Common.Exceptions;
using EShoppingZone.Data.Repositories;
using EShoppingZone.Models.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Business.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IProductService _productService;
        private readonly IProfileService _profileService;
        private readonly IWalletService _walletService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IProductService productService,
            IProfileService profileService,
            IWalletService walletService,
            IDistributedCache cache,
            ILogger<OrderService> logger
        )
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _productService = productService;
            _profileService = profileService;
            _walletService = walletService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<OrderResponse> PlaceOrderAsync(int userId, PlaceOrderRequest request)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null || cart.Items == null || !cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            var address = await _profileService.GetAddressByIdAsync(userId, request.AddressId);
            if (address == null)
                throw new InvalidOperationException("Invalid delivery address");

            foreach (var item in cart.Items)
            {
                var stockUpdated = await _productService.UpdateStockAsync(
                    item.ProductId,
                    -item.Quantity
                );
                if (!stockUpdated)
                    throw new InvalidOperationException(
                        $"Insufficient stock for product: {item.ProductName}"
                    );
            }

            var order = new OrderEntity
            {
                OrderId = 0,
                OrderDate = DateTime.UtcNow,
                CustomerId = userId,
                AmountPaid = cart.TotalPrice,
                ModeOfPayment = request.ModeOfPayment,
                OrderStatus = ServiceConstants.OrderStatuses.Placed,
                Quantity = cart.Items.Sum(i => i.Quantity),
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
                        Subtotal = i.Price * i.Quantity,
                        ImageUrl = i.ImageUrl,
                    })
                    .ToList(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var createdOrder = await _orderRepository.CreateAsync(order);

            cart.Items.Clear();
            cart.TotalPrice = 0;
            cart.LastUpdatedAt = DateTime.UtcNow;
            await _cartRepository.UpdateAsync(cart);

            _logger.LogInformation(
                "Order placed successfully for user {UserId}, OrderId: {OrderId}",
                userId,
                createdOrder.Id
            );
            return MapToOrderResponse(createdOrder);
        }

        public async Task<WalletPaymentResponse> PlaceOrderWithWalletAsync(
            int userId,
            WalletPaymentRequest request
        )
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null || cart.Items == null || !cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            var walletBalance = await _walletService.GetBalanceAsync(userId);
            if (walletBalance.CurrentBalance < cart.TotalPrice)
                throw new InvalidOperationException(
                    $"Insufficient wallet balance. Available: {walletBalance.CurrentBalance:C}, Required: {cart.TotalPrice:C}"
                );

            var address = await _profileService.GetAddressByIdAsync(userId, request.AddressId);
            if (address == null)
                throw new InvalidOperationException("Invalid delivery address");

            var order = new OrderEntity
            {
                OrderId = 0,
                OrderDate = DateTime.UtcNow,
                CustomerId = userId,
                AmountPaid = cart.TotalPrice,
                ModeOfPayment = "EWALLET",
                OrderStatus = ServiceConstants.OrderStatuses.PendingPayment,
                Quantity = cart.Items.Sum(i => i.Quantity),
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
                        Subtotal = i.Price * i.Quantity,
                        ImageUrl = i.ImageUrl,
                    })
                    .ToList(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var createdOrder = await _orderRepository.CreateAsync(order);

            var paymentResult = await _walletService.PayMoneyAsync(
                userId,
                new PayMoneyRequest
                {
                    OrderId = createdOrder.Id,
                    Amount = cart.TotalPrice,
                    Remarks = $"Payment for order #{createdOrder.Id}",
                }
            );

            if (!paymentResult.Success)
            {
                createdOrder.OrderStatus = ServiceConstants.OrderStatuses.PaymentFailed;
                await _orderRepository.UpdateAsync(createdOrder);
                throw new InvalidOperationException(paymentResult.Message);
            }

            foreach (var item in cart.Items)
            {
                var stockUpdated = await _productService.UpdateStockAsync(
                    item.ProductId,
                    -item.Quantity
                );
                if (!stockUpdated)
                {
                    await _walletService.RefundAsync(
                        userId,
                        createdOrder.Id,
                        paymentResult.TransactionId.Value,
                        $"Refund for failed order #{createdOrder.Id}"
                    );
                    createdOrder.OrderStatus = "Stock Failed - Refunded";
                    await _orderRepository.UpdateAsync(createdOrder);
                    throw new InvalidOperationException(
                        $"Insufficient stock for product: {item.ProductName}. Payment refunded."
                    );
                }
            }

            createdOrder.OrderStatus = ServiceConstants.OrderStatuses.Placed;
            await _orderRepository.UpdateAsync(createdOrder);

            cart.Items.Clear();
            cart.TotalPrice = 0;
            cart.LastUpdatedAt = DateTime.UtcNow;
            await _cartRepository.UpdateAsync(cart);

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
                TransactionId = paymentResult.TransactionId.Value,
            };
        }

        public async Task<List<OrderResponse>> GetUserOrdersAsync(int userId)
        {
            var cacheKey = $"user_orders_{userId}";
            var cachedOrders = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedOrders))
                return JsonSerializer.Deserialize<List<OrderResponse>>(cachedOrders)!;

            var orders = await _orderRepository.GetByCustomerIdAsync(userId);
            var orderResponses = orders.Select(MapToOrderResponse).ToList();

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(orderResponses),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                }
            );

            return orderResponses;
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(int orderId, int userId)
        {
            var cacheKey = $"order_{orderId}";
            var cachedOrder = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedOrder))
                return JsonSerializer.Deserialize<OrderResponse>(cachedOrder)!;

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.CustomerId != userId)
                return null;

            var response = MapToOrderResponse(order);
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                }
            );

            return response;
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

            if (userRole == "Customer")
            {
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

            await _cache.RemoveAsync($"order_{orderId}");
            await _cache.RemoveAsync($"user_orders_{order.CustomerId}");

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
                order = await _orderRepository.GetByIdAsync(orderId);
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
            int? customerId = userRole != "Admin" ? userId : null;

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

            if (order.ModeOfPayment == "EWALLET")
            {
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
