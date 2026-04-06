using EShoppingZone.Models.Entities;

namespace EShoppingZone.Data.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshTokenEntity?> GetByTokenAsync(string token);
        Task<IEnumerable<RefreshTokenEntity>> GetAllByUserIdAsync(int userId);
        Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity token);
        Task UpdateAsync(RefreshTokenEntity token);
        Task UpdateRangeAsync(IEnumerable<RefreshTokenEntity> tokens);
        Task DeleteExpiredTokensAsync();
    }
}
