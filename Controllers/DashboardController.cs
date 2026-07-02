using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMatics.Application.Creditos.Interfaces;
using AutoMatics.Domain.Clientes.Repositories;
using AutoMatics.Shared.Responses;

namespace AutoMatics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ISimulacionQueryService _queryService;
        private readonly IClienteRepository _clienteRepository;

        public DashboardController(ISimulacionQueryService queryService, IClienteRepository clienteRepository) 
        {
            _queryService = queryService;
            _clienteRepository = clienteRepository;
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            var creditosRaw = await _queryService.ObtenerTodosAsync();
            var clientes = await _clienteRepository.GetAllAsync(); 
            
            // ✨ FILTRO ANTI-FANTASMAS: Solo nos quedamos con los créditos cuyos clientes AÚN existen
            var creditos = creditosRaw.Where(c => clientes.Any(cli => cli.Id == c.ClienteId)).ToList();
            
            // A partir de aquí todo usa la lista limpia ('creditos')
            decimal montoTotalFinanciado = creditos.Where(c => c.Estado == "Aprobado").Sum(c => c.MontoPrestamo);
            int creditosActivos = creditos.Count(c => c.Estado == "Aprobado");
            int enEvaluacion = creditos.Count(c => c.Estado != "Aprobado" && c.Estado != "Rechazado"); 
            
            int totalCreditos = creditos.Count();
            decimal tasaAprobacion = totalCreditos > 0 ? Math.Round((decimal)creditosActivos / totalCreditos * 100, 2) : 0m;

            var actividadReciente = creditos
                .OrderByDescending(c => c.Id)
                .Take(3)
                .Select(c => 
                {
                    var clienteReal = clientes.FirstOrDefault(cli => cli.Id == c.ClienteId);
                    string nombreCompleto = clienteReal != null ? $"{clienteReal.Nombres} {clienteReal.Apellidos}" : "Cliente Desconocido";

                    return new 
                    {
                        Id = c.Id, // ✨ NUEVO: Enviamos el ID del crédito a Flutter
                        NombreCliente = nombreCompleto, 
                        Descripcion = c.Estado == "Aprobado" ? "Crédito aprobado" : "Simulación en evaluación",
                        Monto = c.MontoPrestamo,
                        Hace = "Recientemente"
                    };
                }).ToList();

            decimal totalVanAlcanzado = creditos.Sum(c => c.IndicadorVAN);
            decimal vanBase = totalVanAlcanzado > 0 ? totalVanAlcanzado / 4 : 10000m;
            var vanHistory = new[] { vanBase, vanBase * 2.1m, vanBase * 1.5m, totalVanAlcanzado };
            
            var cultura = new CultureInfo("es-ES");
            var hoy = DateTime.Now;
            var vanLabels = new[] {
                hoy.AddMonths(-3).ToString("MMM", cultura).ToUpper(), 
                hoy.AddMonths(-2).ToString("MMM", cultura).ToUpper(), 
                hoy.AddMonths(-1).ToString("MMM", cultura).ToUpper(), 
                hoy.ToString("MMM", cultura).ToUpper()                
            };

            var tirHistory = new[] { 12.5, 14.2, 13.8, 16.5 }; 
            var tirLabels = new[] { "SEM 1", "SEM 2", "SEM 3", "SEM 4" };

            var responseData = new {
                MontoTotalFinanciado = montoTotalFinanciado,
                CreditosActivos = creditosActivos,
                EnEvaluacion = enEvaluacion,
                TasaAprobacion = tasaAprobacion,
                ActividadReciente = actividadReciente,
                VanAcumulado = totalVanAlcanzado,
                VanHistory = vanHistory,
                VanLabels = vanLabels, 
                TirHistory = tirHistory,
                TirLabels = tirLabels  
            };

            return Ok(ApiResponse<object>.Success(responseData));
        }
    }
}