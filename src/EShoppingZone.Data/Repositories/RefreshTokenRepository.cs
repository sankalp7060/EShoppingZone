using EShoppingZone.Data.Context;
using EShoppingZone.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Data.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshTokenEntity?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<IEnumerable<RefreshTokenEntity>> GetAllByUserIdAsync(int userId)
        {
            return await _context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
        }

        public async Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity token)
        {
            await _context.RefreshTokens.AddAsync(token);
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
            _context.RefreshTokens.UpdateRange(tokens);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteExpiredTokensAsync()
        {
            var expiredTokens = await _context
                .RefreshTokens.Where(rt => rt.ExpiryDate < DateTime.UtcNow || rt.IsRevoked)
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }
}
