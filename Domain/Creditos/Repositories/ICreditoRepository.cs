using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMatics.Domain.Creditos.Model.Aggregates;

namespace AutoMatics.Domain.Creditos.Repositories
{
    public interface ICreditoRepository
    {
        Task AddAsync(Credito credito);
        Task<Credito?> FindByIdAsync(int id);
        Task<IEnumerable<Credito>> FindByUsuarioIdAsync(int usuarioId);
        Task<IEnumerable<Credito>> GetAllAsync();
        Task<IEnumerable<Credito>> FindByClienteIdAsync(int clienteId);
        void Update(Credito credito);

        void Remove(Credito credito);
    }
}