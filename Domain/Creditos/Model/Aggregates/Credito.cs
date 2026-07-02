using System.Collections.Generic;
using AutoMatics.Domain.Creditos.Model.ValueObjects;

namespace AutoMatics.Domain.Creditos.Model.Aggregates
{
    public class Credito
    {
        public int Id { get; private set; }
        public int ClienteId { get; private set; }
        public int VehiculoId { get; private set; }
        public int UsuarioId { get; private set; }
        
        public decimal MontoPrestamo { get; private set; }
        public string Moneda { get; private set; } = string.Empty;
        public decimal ValorTasa { get; private set; }
        public string TipoTasa { get; private set; } = string.Empty;
        public int PlazoMeses { get; private set; }
        
        public decimal IndicadorVAN { get; private set; }
        public decimal IndicadorTIR { get; private set; }
        public decimal IndicadorTCEA { get; private set; }
        public string Estado { get; private set; } = "Simulado";

        public EvaluacionRiesgo EvaluacionRiesgo { get; private set; } = null!;

        public ICollection<CronogramaPago> CronogramaPagos { get; private set; } = new List<CronogramaPago>();

        protected Credito() { }

        public Credito(int clienteId, int vehiculoId, int usuarioId, decimal montoPrestamo, string moneda, decimal valorTasa, string tipoTasa, int plazoMeses, decimal van, decimal tir, decimal tcea, EvaluacionRiesgo evaluacionRiesgo)
        {
            ClienteId = clienteId;
            VehiculoId = vehiculoId;
            UsuarioId = usuarioId;
            MontoPrestamo = montoPrestamo;
            Moneda = moneda;
            ValorTasa = valorTasa;
            TipoTasa = tipoTasa;
            PlazoMeses = plazoMeses;
            IndicadorVAN = van;
            IndicadorTIR = tir;
            IndicadorTCEA = tcea;
            EvaluacionRiesgo = evaluacionRiesgo;
        }

        public void AgregarCuota(CronogramaPago cuota)
        {
            CronogramaPagos.Add(cuota);
        }

        public void AprobarCredito()
        {
            Estado = "Aprobado";
        }

        // --- NUEVOS MÉTODOS PARA EL FLUJO ---
        public void CambiarEstado(string nuevoEstado)
        {
            Estado = nuevoEstado;
        }

        public void AsignarCliente(int clienteId)
        {
            ClienteId = clienteId;
        }

        public void AsignarUsuario(int usuarioId)
        {
            UsuarioId = usuarioId;
        }
    }
}