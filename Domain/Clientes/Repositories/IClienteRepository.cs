using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMatics.Domain.Clientes.Model.Aggregates;

namespace AutoMatics.Domain.Clientes.Repositories
{
    public interface IClienteRepository
    {
        Task AddAsync(Cliente cliente);
        Task<Cliente?> FindByIdAsync(int id);
        Task<IEnumerable<Cliente>> GetAllAsync();
        void Update(Cliente cliente);

        void Delete(Cliente cliente); 

    }
}