using ReposteriasManu.Application.Contract;
using ReposteriasManu.Domain.Entities;
using ReposteriasManu.Infrastructure.Interfaces;

namespace ReposteriasManu.Application.Services
{
    public class DecorationService : IDecorationService
    {
        private readonly IDecorationRepository _repository;

        public DecorationService(IDecorationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Decoration>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Decoration> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Decoration>> GetByOrderIdAsync(int orderId)
        {
            return await _repository.GetByOrderIdAsync(orderId);
        }

        public async Task AddAsync(Decoration decoration)
        {
            await _repository.AddAsync(decoration);
        }

        public async Task UpdateAsync(Decoration decoration)
        {
            await _repository.UpdateAsync(decoration);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}