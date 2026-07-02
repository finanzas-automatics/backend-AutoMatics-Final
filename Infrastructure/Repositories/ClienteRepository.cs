using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMatics.Domain.Clientes.Model.Aggregates;
using AutoMatics.Domain.Clientes.Repositories;
using AutoMatics.Infrastructure.Data;

namespace AutoMatics.Infrastructure.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly AutoMaticsDbContext _context;

        public ClienteRepository(AutoMaticsDbContext context)
        {
            _context = context;
        }


        public void Delete(Cliente cliente)
        {
            _context.Clientes.Remove(cliente);
        }
        public async Task AddAsync(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
        }

        public async Task<Cliente?> FindByIdAsync(int id)
        {
            // Traemos al cliente junto con su vehículo y sus sustentos usando Include
            return await _context.Clientes
                .Include(c => c.VehiculoObjetivo)
                .Include(c => c.Sustentos)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Cliente>> GetAllAsync()
        {
            // Traemos toda la lista de clientes incluyendo sus vehículos
            return await _context.Clientes
                .Include(c => c.VehiculoObjetivo)
                .ToListAsync();
        }

        public void Update(Cliente cliente)
        {
            _context.Clientes.Update(cliente);
        }
    }
}