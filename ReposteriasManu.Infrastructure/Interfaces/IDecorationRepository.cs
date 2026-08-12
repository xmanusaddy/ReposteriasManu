using ReposteriasManu.Domain.Entities;

namespace ReposteriasManu.Infrastructure.Interfaces
{
    public interface IDecorationRepository
    {
        Task<IEnumerable<Decoration>> GetAllAsync();
        Task<Decoration> GetByIdAsync(int id);
        Task<IEnumerable<Decoration>> GetByOrderIdAsync(int orderId);
        Task AddAsync(Decoration decoration);
        Task UpdateAsync(Decoration decoration);
        Task DeleteAsync(int id);
    }
}