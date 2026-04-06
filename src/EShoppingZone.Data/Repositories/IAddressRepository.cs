using EShoppingZone.Models.Entities;

namespace EShoppingZone.Data.Repositories
{
    public interface IAddressRepository
    {
        Task<AddressEntity?> GetByIdAsync(int id);
        Task<IEnumerable<AddressEntity>> GetByUserIdAsync(int userId);
        Task<AddressEntity> CreateAsync(AddressEntity address);
        Task UpdateAsync(AddressEntity address);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
