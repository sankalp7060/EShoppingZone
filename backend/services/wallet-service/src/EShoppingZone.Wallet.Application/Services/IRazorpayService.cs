using EShoppingZone.Wallet.Application.DTOs;

namespace EShoppingZone.Wallet.Application.Services
{
    public interface IRazorpayService
    {
        Task<RazorpayOrderResponse> CreateOrderAsync(RazorpayOrderRequest request);
        bool VerifySignature(RazorpayVerifyRequest request);
    }
}
