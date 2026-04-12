using System.Security.Cryptography;
using System.Text;
using EShoppingZone.Wallet.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Razorpay.Api;

namespace EShoppingZone.Wallet.Application.Services
{
    public class RazorpayService : IRazorpayService
    {
        private readonly IConfiguration _configuration;
        private readonly string _keyId;
        private readonly string _keySecret;

        public RazorpayService(IConfiguration configuration)
        {
            _configuration = configuration;
            _keyId = _configuration["Razorpay:KeyId"] ?? "";
            _keySecret = _configuration["Razorpay:KeySecret"] ?? "";
        }

        public async Task<RazorpayOrderResponse> CreateOrderAsync(RazorpayOrderRequest request)
        {
            RazorpayClient client = new RazorpayClient(_keyId, _keySecret);

            Dictionary<string, object> options = new Dictionary<string, object>();
            options.Add("amount", (int)(request.Amount * 100)); // amount in the smallest currency unit (paise)
            options.Add("receipt", request.Receipt);
            options.Add("currency", "INR");
            options.Add("payment_capture", "1");

            Order order = client.Order.Create(options);

            return new RazorpayOrderResponse
            {
                OrderId = order["id"].ToString(),
                Amount = request.Amount,
                Currency = "INR",
                Key = _keyId
            };
        }

        public bool VerifySignature(RazorpayVerifyRequest request)
        {
            try
            {
                // The SDK's Utils.verifyPaymentSignature requires the attributes to have the correct keys
                // and it needs the secret to perform the HMAC-SHA256 check.
                string payload = $"{request.RazorpayOrderId}|{request.RazorpayPaymentId}";
                string secret = _keySecret;
                string signature = request.RazorpaySignature;

                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                {
                    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                    var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    return hashString == signature.ToLower();
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
