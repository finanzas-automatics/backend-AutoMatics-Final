using System;
using System.Threading.Tasks;
using AutoMatics.Application.Creditos.Interfaces;
using AutoMatics.Domain.Creditos.Model.Aggregates;
using AutoMatics.Domain.Creditos.Model.Commands;
using AutoMatics.Domain.Creditos.Model.ValueObjects;
using AutoMatics.Domain.Creditos.Repositories;
using AutoMatics.Domain.Clientes.Repositories;
using AutoMatics.Domain.Common;
using AutoMatics.Domain.Creditos.Services;

namespace AutoMatics.Application.Creditos.Internal.CommandServices
{
    public class SimulacionCommandService : ISimulacionCommandService
    {
        private readonly ICreditoRepository _creditoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly MotorFinancieroDomainService _motorFinanciero;

        public SimulacionCommandService(
            ICreditoRepository creditoRepository,
            IClienteRepository clienteRepository,
            IUnitOfWork unitOfWork,
            MotorFinancieroDomainService motorFinanciero)
        {
            _creditoRepository = creditoRepository;
            _clienteRepository = clienteRepository;
            _unitOfWork = unitOfWork;
            _motorFinanciero = motorFinanciero;
        }

        public async Task<Credito> HandleAsync(CreateSimulacionCommand command)
        {
            // ✨ LLAMADA AL MOTOR ACTUALIZADA CON LOS 11 PARÁMETROS NUEVOS
            var resultado = _motorFinanciero.GenerarCronograma(
                command.PrecioVenta, command.PorcentajeCuotaInicial, command.PlazoMeses,
                command.TasaInteresAnual, command.EsTasaEfectiva, command.DiasCapitalizacion,
                command.MesesGraciaTotal, command.MesesGraciaParcial, command.PorcentajeCuotaFinal,
                command.TasaDesgravamenMensual, command.SeguroVehicularMensual,
                command.PortesMensuales, command.TasaCokAnual,
                command.CostesNotariales, command.FinanciarNotariales,
                command.CostesRegistrales, command.FinanciarRegistrales,
                command.Tasacion, command.FinanciarTasacion,
                command.ComisionEstudio, command.FinanciarEstudio,
                command.ComisionActivacion, command.FinanciarActivacion,
                command.GpsMensual, command.GastosAdmMensuales
            );

            decimal primeraCuotaTotal = resultado.Cronograma[0].CuotaTotalMensual;

            EvaluacionRiesgo evaluacion;
            if (command.ClienteId > 0)
            {
                var cliente = await _clienteRepository.FindByIdAsync(command.ClienteId)
                    ?? throw new Exception($"Cliente con Id {command.ClienteId} no existe.");
                evaluacion = new EvaluacionRiesgo(cliente.IngresosNetosMensuales, primeraCuotaTotal);
            }
            else
            {
                evaluacion = new EvaluacionRiesgo(999999m, primeraCuotaTotal);
            }

            

            var tipoTasa = command.EsTasaEfectiva ? "TEA" : "TNA";

            var credito = new Credito(
                command.ClienteId, command.VehiculoId, command.UsuarioId,
                resultado.MontoPrestamo, command.Moneda,
                (decimal)command.TasaInteresAnual, tipoTasa, command.PlazoMeses,
                (decimal)resultado.Van, (decimal)resultado.Tir * 100,
                (decimal)resultado.Tcea * 100, evaluacion
            );

            // Marcamos el crédito como borrador antes de guardarlo
            credito.CambiarEstado("Borrador");

            foreach (var cuota in resultado.Cronograma)
            {
                credito.AgregarCuota(new CronogramaPago(
                    cuota.NumeroMes, cuota.TipoPeriodo.ToString(),
                    cuota.SaldoInicial, cuota.Amortizacion, cuota.Interes,
                    cuota.SegurosYGastos, cuota.CuotaTotalMensual, cuota.SaldoFinal
                ));
            }

            await _creditoRepository.AddAsync(credito);
            await _unitOfWork.CompleteAsync();
            return credito;
        }
        private static bool EsEstadoProtegido(string estado) => estado == "Aprobado" || estado == "Activo" || estado == "Mora";

        public async Task AprobarAsync(int id)
        {
            var credito = await _creditoRepository.FindByIdAsync(id)
                ?? throw new Exception("Simulación no encontrada");

            // 1. Aprobamos el crédito seleccionado
            credito.AprobarCredito();
            _creditoRepository.Update(credito);

            // 2. Buscamos TODOS los créditos de este cliente
            var todosLosCreditosDelCliente = await _creditoRepository.FindByClienteIdAsync(credito.ClienteId);


            foreach (var otroCredito in todosLosCreditosDelCliente)
            {
                if (otroCredito.Id != id && !EsEstadoProtegido(otroCredito.Estado))
                {
                    _creditoRepository.Remove(otroCredito);
                }
            }

            // 4. Actualizamos el estado global del cliente
            if (credito.ClienteId > 0)
            {
                var cliente = await _clienteRepository.FindByIdAsync(credito.ClienteId);
                if (cliente != null)
                {
                    cliente.UpdateData(
                        cliente.Nombres, cliente.Apellidos, cliente.Correo,
                        cliente.Telefono, cliente.Direccion,
                        cliente.IngresosNetosMensuales,
                        "Aprobado"
                    );
                    _clienteRepository.Update(cliente);
                }
            }

            await _unitOfWork.CompleteAsync();
        }  
       
        public async Task EliminarAsync(int id)
        {
            var credito = await _creditoRepository.FindByIdAsync(id);
            if (credito == null)
                throw new Exception("La simulación que intentas eliminar no existe o ya fue borrada.");

            if (EsEstadoProtegido(credito.Estado))
                throw new Exception("No se puede eliminar un crédito que ya fue aprobado o está en curso.");

            _creditoRepository.Remove(credito);
            await _unitOfWork.CompleteAsync();
        }
    }
}