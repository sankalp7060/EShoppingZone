using EShoppingZone.Profile.Domain.Entities;
using EShoppingZone.Profile.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Profile.Infrastructure.Repositories
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

    public class AddressRepository : IAddressRepository
    {
        private readonly ApplicationDbContext _context;

        public AddressRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AddressEntity?> GetByIdAsync(int id) =>
            await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id);

        public async Task<IEnumerable<AddressEntity>> GetByUserIdAsync(int userId) =>
            await _context
                .Addresses.Where(a => a.UserId == userId && a.IsActive)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

        public async Task<AddressEntity> CreateAsync(AddressEntity address)
        {
            await _context.Addresses.AddAsync(address);
            await _context.SaveChangesAsync();
            return address;
        }

        public async Task UpdateAsync(AddressEntity address)
        {
            _context.Entry(address).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var address = await GetByIdAsync(id);
            if (address != null)
            {
                address.IsActive = false;
                await UpdateAsync(address);
            }
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Addresses.AnyAsync(a => a.Id == id && a.IsActive);
    }
}
