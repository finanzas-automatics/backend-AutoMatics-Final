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
            ValidarCommand(command);
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

        private static void ValidarCommand(CreateSimulacionCommand c)
        {
            var errores = new List<string>();

            if (c.PrecioVenta <= 0) errores.Add("El precio de venta debe ser mayor a 0.");
            if (c.PlazoMeses < 1 || c.PlazoMeses > 120) errores.Add("El plazo debe estar entre 1 y 120 meses.");

            if (c.PorcentajeCuotaInicial < 0 || c.PorcentajeCuotaInicial > 1)
                errores.Add("El % de cuota inicial debe estar entre 0 y 1 (ej. 0.10 para 10%).");
            if (c.PorcentajeCuotaFinal < 0 || c.PorcentajeCuotaFinal > 1)
                errores.Add("El % de cuota final debe estar entre 0 y 1 (ej. 0.30 para 30%).");
            if (c.PorcentajeCuotaInicial + c.PorcentajeCuotaFinal >= 1)
                errores.Add("La suma de cuota inicial y cuota final no puede ser mayor o igual al 100% del precio.");

            if (c.TasaInteresAnual <= 0 || c.TasaInteresAnual > 1)
                errores.Add("La tasa de interés anual debe estar entre 0 y 1 (ej. 0.15 para 15%).");
            if (c.TasaCokAnual < 0 || c.TasaCokAnual > 1)
                errores.Add("El COK anual debe estar entre 0 y 1 (ej. 0.10 para 10%).");

            if (c.TasaDesgravamenMensual < 0 || c.TasaDesgravamenMensual > 0.05m)
                errores.Add("El seguro de desgravamen mensual parece fuera de rango (máx. 5%).");
            if (c.SeguroVehicularMensual < 0 || c.SeguroVehicularMensual > 0.05m)
                errores.Add("El seguro vehicular mensual parece fuera de rango (máx. 5%).");

            if (c.MesesGraciaTotal < 0 || c.MesesGraciaParcial < 0)
                errores.Add("Los meses de gracia no pueden ser negativos.");
            if (c.MesesGraciaTotal + c.MesesGraciaParcial >= c.PlazoMeses)
                errores.Add("Los meses de gracia no pueden ser mayores o iguales al plazo total.");

            if (errores.Any())
                throw new ArgumentException(string.Join(" | ", errores));
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