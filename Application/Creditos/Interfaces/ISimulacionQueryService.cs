using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMatics.Domain.Creditos.Model.Aggregates;
using AutoMatics.Domain.Creditos.Model.Queries;

namespace AutoMatics.Application.Creditos.Interfaces
{
    public interface ISimulacionQueryService
    {
        Task<IEnumerable<Credito>> Handle(GetSimulacionesByUsuarioIdQuery query);
        Task<Credito?> Handle(GetSimulacionByIdQuery query);
        Task<IEnumerable<Credito>> ObtenerTodosAsync();
    }
}