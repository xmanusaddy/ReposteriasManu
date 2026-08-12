using Microsoft.EntityFrameworkCore;
using ReposteriasManu.Domain.Entities;
using ReposteriasManu.Infrastructure.Context;
using ReposteriasManu.Infrastructure.Interfaces;

namespace ReposteriasManu.Infrastructure.Repositories
{
    public class DecorationRepository : IDecorationRepository
    {
        private readonly ReposteriasManuContext _context;

        public DecorationRepository(ReposteriasManuContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Decoration>> GetAllAsync()
        {
            return await _context.Decorations.Include(d => d.Order).ToListAsync();
        }

        public async Task<Decoration> GetByIdAsync(int id)
        {
            return await _context.Decorations.Include(d => d.Order).FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Decoration>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Decorations.Where(d => d.OrderId == orderId).ToListAsync();
        }

        public async Task AddAsync(Decoration decoration)
        {
            await _context.Decorations.AddAsync(decoration);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Decoration decoration)
        {
            _context.Decorations.Update(decoration);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var decoration = await _context.Decorations.FindAsync(id);
            if (decoration != null)
            {
                _context.Decorations.Remove(decoration);
                await _context.SaveChangesAsync();
            }
        }
    }
}