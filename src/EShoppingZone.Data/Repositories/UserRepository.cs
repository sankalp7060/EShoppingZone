using EShoppingZone.Data.Context;
using EShoppingZone.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserEntity?> GetByIdAsync(int id)
        {
            return await _context
                .Users.Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            return await _context
                .Users.Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());
        }

        public async Task<UserEntity?> GetByOAuthAsync(string provider, string oauthId)
        {
            return await _context.Users.FirstOrDefaultAsync(u =>
                u.OAuthProvider == provider && u.OAuthId == oauthId
            );
        }

        public async Task<IEnumerable<UserEntity>> GetAllAsync()
        {
            return await _context
                .Users.Include(u => u.Addresses)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserEntity> CreateAsync(UserEntity user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(UserEntity user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                await UpdateAsync(user);
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email.ToLower());
        }

        public async Task<bool> MobileExistsAsync(long mobile)
        {
            return await _context.Users.AnyAsync(u => u.MobileNumber == mobile);
        }

        public async Task<UserEntity?> GetByMobileNumberAsync(long mobileNumber)
        {
            return await _context
                .Users.Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
        }
    }
}
