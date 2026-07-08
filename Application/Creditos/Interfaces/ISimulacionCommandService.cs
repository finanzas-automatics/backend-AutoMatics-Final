using System.Threading.Tasks;
using AutoMatics.Domain.Creditos.Model.Aggregates;
using AutoMatics.Domain.Creditos.Model.Commands;

namespace AutoMatics.Application.Creditos.Interfaces
{
    public interface ISimulacionCommandService
    {

        Task<Credito> HandleAsync(CreateSimulacionCommand command);

        Task AprobarAsync(int id);
        Task EliminarAsync(int id);
    }
}