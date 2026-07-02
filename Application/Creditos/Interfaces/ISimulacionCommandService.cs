using System.Threading.Tasks;
using AutoMatics.Domain.Creditos.Model.Aggregates;
using AutoMatics.Domain.Creditos.Model.Commands;

namespace AutoMatics.Application.Creditos.Interfaces
{
    public interface ISimulacionCommandService
    {
        /// <summary>
        /// Procesa la simulación financiera bajo estándares SBS y la guarda inicialmente como Borrador.
        /// </summary>
        Task<Credito> HandleAsync(CreateSimulacionCommand command);

        /// <summary>
        /// Cambia el estado del crédito a Aprobado y actualiza el perfil crediticio del cliente relacionado.
        /// </summary>
        Task AprobarAsync(int id);
        Task EliminarAsync(int id);
    }
}