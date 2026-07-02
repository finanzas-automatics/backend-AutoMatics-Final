using System.Threading.Tasks;
using AutoMatics.Domain.IAM.Model.Aggregates;

namespace AutoMatics.Domain.IAM.Repositories
{
    public interface IUsuarioRepository
    {
        Task AddAsync(Usuario usuario);
        Task<Usuario?> FindByCorreoAsync(string correo);
    }
}