using EShoppingZone.Data.Context;
using EShoppingZone.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Data.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly AppDbContext _context;

        public AddressRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AddressEntity?> GetByIdAsync(int id)
        {
            return await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<AddressEntity>> GetByUserIdAsync(int userId)
        {
            return await _context
                .Addresses.Where(a => a.UserId == userId && a.IsActive)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

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

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Addresses.AnyAsync(a => a.Id == id && a.IsActive);
        }
    }
}
