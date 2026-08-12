using Microsoft.EntityFrameworkCore;
using ReposteriasManu.Domain.Entities;
using ReposteriasManu.Infrastructure.Context;
using ReposteriasManu.Infrastructure.Interfaces;

namespace ReposteriasManu.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ReposteriasManuContext _context;

        public CustomerRepository(ReposteriasManuContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task<Customer> GetByIdAsync(int id)
        {
            return await _context.Customers.FindAsync(id);
        }

        public async Task AddAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
        }
    }
}