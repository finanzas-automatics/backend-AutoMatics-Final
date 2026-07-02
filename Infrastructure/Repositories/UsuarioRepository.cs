using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMatics.Domain.IAM.Model.Aggregates;
using AutoMatics.Domain.IAM.Repositories;
using AutoMatics.Infrastructure.Data;

namespace AutoMatics.Infrastructure.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AutoMaticsDbContext _context;

        public UsuarioRepository(AutoMaticsDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Usuario usuario)
        {
            await _context.Usuarios.AddAsync(usuario);
        }

        public async Task<Usuario?> FindByCorreoAsync(string correo)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
        }
    }
}