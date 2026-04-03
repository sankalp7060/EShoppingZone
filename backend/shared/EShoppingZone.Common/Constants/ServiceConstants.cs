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
    }
}
