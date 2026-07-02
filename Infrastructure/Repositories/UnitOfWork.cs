using System.Threading.Tasks;
using AutoMatics.Domain.Common;
using AutoMatics.Infrastructure.Data;

namespace AutoMatics.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AutoMaticsDbContext _context;
        public UnitOfWork(AutoMaticsDbContext context) => _context = context;
        public async Task CompleteAsync() => await _context.SaveChangesAsync();
    }
}