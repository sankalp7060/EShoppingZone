namespace EShoppingZone.Common.Constants
{
    public static class ServiceConstants
    {
        public static class ServiceNames
        {
            public const string Profile = "profile-service";
            public const string Product = "product-service";
            public const string Cart = "cart-service";
            public const string Order = "order-service";
            public const string Wallet = "wallet-service";
        }

        public static class Routes
        {
            public const string ProfileBase = "/api/profile";
            public const string ProductBase = "/api/products";
            public const string CartBase = "/api/cart";
            public const string OrderBase = "/api/orders";
            public const string WalletBase = "/api/wallet";
        }

        public static class HealthEndpoint
        {
            public const string Path = "/health";
            public const string ReadyPath = "/health/ready";
            public const string LivePath = "/health/live";
        }

        public static class OrderStatuses
        {
            public const string Placed = "Placed";
            public const string Confirmed = "Confirmed";
            public const string Processing = "Processing";
            public const string Shipped = "Shipped";
            public const string OutForDelivery = "OutForDelivery";
            public const string Delivered = "Delivered";
            public const string Cancelled = "Cancelled";
            public const string PendingPayment = "Pending Payment";
            public const string PaymentFailed = "Payment Failed";
        }

        public static class TransactionTypes
        {
            public const string Credit = "CREDIT";
            public const string Debit = "DEBIT";
        }
    }
}
