using System.Threading.Tasks;
using AutoMatics.Domain.Clientes.Model.Commands;

namespace AutoMatics.Application.Clientes.Interfaces
{
    public interface IClienteCommandService
    {
        Task<int> HandleAsync(CrearClienteCommand command);
        Task HandleAsync(ActualizarClienteCommand command);
    }
}