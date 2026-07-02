namespace AutoMatics.Domain.Creditos.Model.Aggregates
{
    public class CronogramaPago
    {
        public int Id { get; private set; }
        public int CreditoId { get; private set; }
        public int NumeroMes { get; private set; }
        public string TipoPeriodo { get; private set; } = string.Empty;
        public decimal SaldoInicial { get; private set; }
        public decimal Amortizacion { get; private set; }
        public decimal Interes { get; private set; }
        public decimal SegurosYGastos { get; private set; }
        public decimal CuotaTotalMensual { get; private set; }
        public decimal SaldoFinal { get; private set; }

        protected CronogramaPago() { }

        public CronogramaPago(int numeroMes, string tipoPeriodo, decimal saldoInicial, decimal amortizacion, decimal interes, decimal segurosYGastos, decimal cuotaTotalMensual, decimal saldoFinal)
        {
            NumeroMes = numeroMes;
            TipoPeriodo = tipoPeriodo;
            SaldoInicial = saldoInicial;
            Amortizacion = amortizacion;
            Interes = interes;
            SegurosYGastos = segurosYGastos;
            CuotaTotalMensual = cuotaTotalMensual;
            SaldoFinal = saldoFinal;
        }
    }
}