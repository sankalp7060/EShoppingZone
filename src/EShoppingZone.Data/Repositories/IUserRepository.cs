using EShoppingZone.Models.Entities;

namespace EShoppingZone.Data.Repositories
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
}
