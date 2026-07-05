using System;
using System.Threading.Tasks;
using AutoMatics.Application.Clientes.Interfaces;
using AutoMatics.Domain.Clientes.Model.Aggregates;
using AutoMatics.Domain.Clientes.Model.Commands;
using AutoMatics.Domain.Clientes.Repositories;
using AutoMatics.Domain.Common;

namespace AutoMatics.Application.Clientes.Internal
{
    public class ClienteCommandService : IClienteCommandService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ClienteCommandService(IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
        {
            _clienteRepository = clienteRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> HandleAsync(CrearClienteCommand command)
        {
            // ✨ 1. PASAMOS EL command.UsuarioId AL CLIENTE
            var cliente = new Cliente(
                command.UsuarioId,
                command.TipoDocumento, command.NumeroDocumento, command.Nombres, 
                command.Apellidos, command.Correo, command.Telefono, 
                command.Direccion, command.IngresosNetosMensuales
            );

            // 2. Guardamos al cliente
            await _clienteRepository.AddAsync(cliente);
            await _unitOfWork.CompleteAsync();

            // 3. Le asignamos el vehículo
            if (command.Vehiculo != null)
            {
                cliente.AsignarVehiculo(
                    command.Vehiculo.Brand, command.Vehiculo.Model, command.Vehiculo.Year,
                    command.Vehiculo.Price, command.Vehiculo.Currency, command.Vehiculo.Status,
                    command.Vehiculo.FuelType, command.Vehiculo.Transmission, command.Vehiculo.Engine
                );

                _clienteRepository.Update(cliente);
                await _unitOfWork.CompleteAsync();
            }
            
            return cliente.Id;
        }

        public async Task HandleAsync(ActualizarClienteCommand command)
        {
            var cliente = await _clienteRepository.FindByIdAsync(command.Id) ?? throw new Exception("Cliente no encontrado");
            
            cliente.UpdateData(
                command.Nombres, command.Apellidos, command.Correo, 
                command.Telefono, command.Direccion, command.IngresosNetosMensuales, 
                command.EstadoCrediticio
            );
            
            _clienteRepository.Update(cliente);
            await _unitOfWork.CompleteAsync();
        }
    }
}