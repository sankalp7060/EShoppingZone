using EShoppingZone.Profile.Domain.Entities;
using EShoppingZone.Profile.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Profile.Infrastructure.Repositories
{
    public interface IUserRepository
    {
        Task<UserEntity?> GetByIdAsync(int id);
        Task<UserEntity?> GetByEmailAsync(string email);
        Task<UserEntity?> GetByOAuthAsync(string provider, string oauthId);
        Task<IEnumerable<UserEntity>> GetAllAsync();
        Task<UserEntity> CreateAsync(UserEntity user);
        Task UpdateAsync(UserEntity user);
        Task DeleteAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> MobileExistsAsync(long mobile);
        Task<UserEntity?> GetByMobileNumberAsync(long mobileNumber);
    }

    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserEntity?> GetByIdAsync(int id) =>
            await _context.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == id);

        public async Task<UserEntity?> GetByEmailAsync(string email) =>
            await _context.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());

        public async Task<UserEntity?> GetByOAuthAsync(string provider, string oauthId) =>
            await _context.Users
                .FirstOrDefaultAsync(u => u.OAuthProvider == provider && u.OAuthId == oauthId);

        public async Task<IEnumerable<UserEntity>> GetAllAsync() =>
            await _context.Users
                .Include(u => u.Addresses)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

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

        public async Task<bool> EmailExistsAsync(string email) =>
            await _context.Users.AnyAsync(u => u.Email == email.ToLower());

        public async Task<bool> MobileExistsAsync(long mobile) =>
            await _context.Users.AnyAsync(u => u.MobileNumber == mobile);

        public async Task<UserEntity?> GetByMobileNumberAsync(long mobileNumber) =>
            await _context.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
    }
}