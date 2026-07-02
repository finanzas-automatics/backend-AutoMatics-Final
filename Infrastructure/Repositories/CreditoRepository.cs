using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMatics.Domain.Creditos.Model.Aggregates;
using AutoMatics.Domain.Creditos.Repositories;
using AutoMatics.Infrastructure.Data;

namespace AutoMatics.Infrastructure.Repositories
{
    public class CreditoRepository : ICreditoRepository
    {
        private readonly AutoMaticsDbContext _context;

        public CreditoRepository(AutoMaticsDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Credito credito)
        {
            await _context.Creditos.AddAsync(credito);
        }

        public async Task<IEnumerable<Credito>> FindByClienteIdAsync(int clienteId)
        {
            // Usa tu _context para buscar los créditos (asegúrate de incluir el using Microsoft.EntityFrameworkCore;)
            return await _context.Creditos
                                .Where(c => c.ClienteId == clienteId)
                                .ToListAsync();
        }
        public async Task<Credito?> FindByIdAsync(int id)
        {
            return await _context.Creditos
                .Include(c => c.CronogramaPagos)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Credito>> FindByUsuarioIdAsync(int usuarioId)
        {
            return await _context.Creditos
                .Where(c => c.UsuarioId == usuarioId)
                .Include(c => c.CronogramaPagos)
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<Credito>> GetAllAsync()
        {
            return await _context.Creditos
                .Include(c => c.CronogramaPagos)
                .ToListAsync();
        }

        public void Update(Credito credito)
        {
            _context.Creditos.Update(credito);
        }

        public void Remove(Credito credito)
        {
            _context.Creditos.Remove(credito); // O el DbSet equivalente a tus créditos
        }
    }
}