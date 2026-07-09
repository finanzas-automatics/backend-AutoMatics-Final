using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMatics.Application.Creditos.Interfaces;
using AutoMatics.Domain.Creditos.Model.Commands;
using AutoMatics.Domain.Creditos.Model.Queries;
using AutoMatics.Shared.Responses;

namespace AutoMatics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CreditosController : ControllerBase
    {
        private readonly ISimulacionCommandService _commandService;
        private readonly ISimulacionQueryService _queryService;

        public CreditosController(ISimulacionCommandService commandService, ISimulacionQueryService queryService)
        {
            _commandService = commandService;
            _queryService = queryService;
        }

        [HttpPost("simular")]
            public async Task<IActionResult> Simular([FromBody] CreateSimulacionCommand command)
            {
                try
                {
                    var credito = await _commandService.HandleAsync(command);
                    return Ok(ApiResponse<object>.Success(credito, "Cálculo estructurado bajo SBS exitoso. Guardado como Borrador."));
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ApiResponse<string>.Fail(ex.Message));
                }
            }
        [HttpPost("{id}/aprobar")]
        public async Task<IActionResult> Aprobar(int id)
        {
            try
            {
                await _commandService.AprobarAsync(id);
                return Ok(ApiResponse<bool>.Success(true, "El crédito ha pasado del estado Simulado a Aprobado."));
            }
            catch (System.Exception ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
        }

        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> HistorialCliente(int clienteId)
        {
            var todos = await _queryService.ObtenerTodosAsync();
            var creditosDelCliente = todos.Where(c => c.ClienteId == clienteId).ToList();

            return Ok(ApiResponse<object>.Success(creditosDelCliente));
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> HistorialUsuario(int usuarioId)
        {
            var result = await _queryService.Handle(new GetSimulacionesByUsuarioIdQuery(usuarioId));
            return Ok(ApiResponse<object>.Success(result));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {

                await _commandService.EliminarAsync(id);
                return Ok(ApiResponse<bool>.Success(true, "Simulación eliminada correctamente."));
            }
            catch (System.Exception ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Detalle(int id)
        {
            var result = await _queryService.Handle(new GetSimulacionByIdQuery(id));
            if (result == null) return NotFound(ApiResponse<string>.Fail("No se encontró."));
            return Ok(ApiResponse<object>.Success(result));
        }
    }
}