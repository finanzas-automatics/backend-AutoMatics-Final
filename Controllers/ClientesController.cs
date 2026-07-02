using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMatics.Controllers.Resources;
using AutoMatics.Application.Clientes.Interfaces;
using AutoMatics.Domain.Clientes.Model.Commands;
using AutoMatics.Domain.Clientes.Repositories;
using AutoMatics.Shared.Responses;
using AutoMatics.Domain.Common;

namespace AutoMatics.Controllers
{
    [ApiController]
    [Route("api/Clients")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteCommandService _commandService;
        private readonly IClienteRepository _clienteRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ClientesController(IClienteCommandService commandService, IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
        {
            _commandService = commandService;
            _clienteRepository = clienteRepository;
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarCliente([FromBody] ClienteCreateResource resource)
        {
            var command = new CrearClienteCommand(
                resource.DocumentType, resource.DocumentNumber, resource.FirstName,
                resource.LastName, resource.Email, resource.Phone, resource.Address,
                resource.MonthlyIncome, resource.Vehicle
            );

            var id = await _commandService.HandleAsync(command);
            return Ok(ApiResponse<int>.Success(id, "Cliente y Vehículo guardados con éxito."));
        }

        [HttpGet]
        public async Task<IActionResult> ListarClientes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var clientesDb = await _clienteRepository.GetAllAsync();

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                clientesDb = clientesDb.Where(c =>
                    c.Nombres.ToLower().Contains(searchLower) ||
                    c.Apellidos.ToLower().Contains(searchLower) ||
                    c.NumeroDocumento.Contains(searchLower) ||
                    (c.VehiculoObjetivo != null && c.VehiculoObjetivo.Marca.ToLower().Contains(searchLower))
                ).ToList();
            }

            if (!string.IsNullOrEmpty(status))
            {
                var statusNorm = NormalizarEstado(status);
                clientesDb = clientesDb
                    .Where(c => NormalizarEstado(c.EstadoCrediticio) == statusNorm)
                    .ToList();
            }

            var totalCount = clientesDb.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var paginatedClientes = clientesDb
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var items = paginatedClientes.Select(c => new ClienteListResponse(
                Id: c.Id,
                FullName: $"{c.Nombres} {c.Apellidos}",
                DocumentNumber: c.NumeroDocumento,
                Email: c.Correo,
                Status: c.EstadoCrediticio,
                VehicleName: c.VehiculoObjetivo != null ? $"{c.VehiculoObjetivo.Marca} {c.VehiculoObjetivo.Modelo}" : null,
                VehiclePrice: c.VehiculoObjetivo?.Precio,
                VehicleCurrency: c.VehiculoObjetivo?.Moneda
            )).ToList();

            var pagedData = new
            {
                items = items,
                totalCount = totalCount,
                currentPage = page,
                pageSize = pageSize,
                totalPages = totalPages
            };

            return Ok(ApiResponse<object>.Success(pagedData));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerClientePorId(int id)
        {
            try
            {
                var cliente = await _clienteRepository.FindByIdAsync(id);
                if (cliente == null)
                    return NotFound(new { exito = false, mensaje = "Cliente no encontrado" });

                VehicleDetalleResource? vehicleResource = null;
                if (cliente.VehiculoObjetivo != null)
                {
                    var v = cliente.VehiculoObjetivo;
                    vehicleResource = new VehicleDetalleResource(
                        Id: v.Id, Brand: v.Marca, Model: v.Modelo, Year: v.Año,
                        Price: v.Precio, Currency: v.Moneda, Status: v.Estado,
                        FuelType: v.TipoCombustible, Transmission: v.Transmision, Engine: v.Motor
                    );
                }

                var resource = new ClienteDetalleResource(
                    Id: cliente.Id, DocumentType: cliente.TipoDocumento,
                    DocumentNumber: cliente.NumeroDocumento, FirstName: cliente.Nombres,
                    LastName: cliente.Apellidos, FullName: $"{cliente.Nombres} {cliente.Apellidos}",
                    Email: cliente.Correo, Phone: cliente.Telefono, Address: cliente.Direccion,
                    MonthlyIncome: cliente.IngresosNetosMensuales,
                    Status: cliente.EstadoCrediticio, Vehicle: vehicleResource
                );

                return Ok(ApiResponse<ClienteDetalleResource>.Success(resource, "Cliente encontrado"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { exito = false, mensaje = $"Error interno: {ex.Message}" });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarCliente(int id, [FromBody] ClienteUpdateResource resource)
        {
            try
            {
                var cliente = await _clienteRepository.FindByIdAsync(id);
                if (cliente == null)
                    return NotFound(new { exito = false, mensaje = "Cliente no encontrado" });

                var estadoNormalizado = resource.Status?.ToLower() switch
                {
                    "aprobado"   => "Aprobado",      // ✅
                    "evaluacion" => "En Evaluación",
                    "pendiente"  => "Pendiente",
                    "mora"       => "Mora",
                    _            => "En Evaluación"
                };

                cliente.UpdateData(
                    resource.FirstName, resource.LastName, resource.Email,
                    resource.Phone, resource.Address, resource.MonthlyIncome,
                    estadoNormalizado
                );

                if (resource.Vehicle != null)
                {
                    cliente.AsignarVehiculo(
                        resource.Vehicle.Brand, resource.Vehicle.Model,
                        resource.Vehicle.Year, resource.Vehicle.Price,
                        resource.Vehicle.Currency, resource.Vehicle.Status,
                        resource.Vehicle.FuelType, resource.Vehicle.Transmission,
                        resource.Vehicle.Engine
                    );
                }

                _clienteRepository.Update(cliente);
                await _unitOfWork.CompleteAsync();

                return Ok(ApiResponse<int>.Success(id, "Cliente actualizado con éxito."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { exito = false, mensaje = $"Error interno: {ex.Message}" });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarCliente(int id)
        {
            try
            {
                var cliente = await _clienteRepository.FindByIdAsync(id);
                if (cliente == null)
                    return NotFound(new { exito = false, mensaje = "Cliente no encontrado" });

                _clienteRepository.Delete(cliente);
                await _unitOfWork.CompleteAsync();

                return Ok(new { exito = true, mensaje = "Cliente eliminado con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { exito = false, mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // ✅ Normaliza estados para comparación sin tildes ni espacios
        private static string NormalizarEstado(string estado)
        {
            var s = estado.ToLower()
                .Replace("á", "a").Replace("é", "e")
                .Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace(" ", "");

            return s switch
            {
                "enevaluacion" or "evaluacion" => "evaluacion",
                "aprobado" or "activo"         => "aprobado",  // ✅ activo sigue funcionando
                "mora"                         => "mora",
                "pendiente"                    => "pendiente",
                _                              => s
            };
        }
    }
}