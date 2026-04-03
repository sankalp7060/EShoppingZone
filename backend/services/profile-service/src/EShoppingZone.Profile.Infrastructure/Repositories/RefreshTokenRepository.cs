using EShoppingZone.Profile.Domain.Entities;
using EShoppingZone.Profile.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Profile.Infrastructure.Repositories
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

    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshTokenEntity?> GetByTokenAsync(string token) =>
            await _context.Set<RefreshTokenEntity>().FirstOrDefaultAsync(rt => rt.Token == token);

        public async Task<IEnumerable<RefreshTokenEntity>> GetAllByUserIdAsync(int userId) =>
            await _context.Set<RefreshTokenEntity>().Where(rt => rt.UserId == userId).ToListAsync();

        public async Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity token)
        {
            await _context.Set<RefreshTokenEntity>().AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task UpdateAsync(RefreshTokenEntity token)
        {
            _context.Entry(token).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(IEnumerable<RefreshTokenEntity> tokens)
        {
            _context.Set<RefreshTokenEntity>().UpdateRange(tokens);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteExpiredTokensAsync()
        {
            var expiredTokens = await _context
                .Set<RefreshTokenEntity>()
                .Where(rt => rt.ExpiryDate < DateTime.UtcNow || rt.IsRevoked)
                .ToListAsync();

            _context.Set<RefreshTokenEntity>().RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }
}
