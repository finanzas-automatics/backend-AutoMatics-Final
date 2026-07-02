using System.Threading.Tasks;
using AutoMatics.Domain.IAM.Model.Commands;

namespace AutoMatics.Application.IAM.Interfaces
{
    public interface IAuthCommandService
    {
        Task<string> LoginAsync(string correo, string password);
        Task HandleAsync(RegistrarUsuarioCommand command);
    }
}