using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMatics.Application.Creditos.Interfaces;
using AutoMatics.Domain.Creditos.Model.Aggregates;
using AutoMatics.Domain.Creditos.Model.Queries;
using AutoMatics.Domain.Creditos.Repositories;

namespace AutoMatics.Application.Creditos.Internal.QueryServices
{
    public class SimulacionQueryService : ISimulacionQueryService
    {
        private readonly ICreditoRepository _creditoRepository;

        public SimulacionQueryService(ICreditoRepository creditoRepository)
        {
            _creditoRepository = creditoRepository;
        }

        public async Task<IEnumerable<Credito>> Handle(GetSimulacionesByUsuarioIdQuery query)
        {
            return await _creditoRepository.FindByUsuarioIdAsync(query.UsuarioId);
        }

        public async Task<Credito?> Handle(GetSimulacionByIdQuery query)
        {
            return await _creditoRepository.FindByIdAsync(query.CreditoId);
        }

        public async Task<IEnumerable<Credito>> ObtenerTodosAsync()
        {
            return await _creditoRepository.GetAllAsync();
        }
    }
}