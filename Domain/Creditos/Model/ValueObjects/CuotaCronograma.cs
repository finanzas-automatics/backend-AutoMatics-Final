namespace AutoMatics.Domain.Creditos.Model.ValueObjects
{
    public enum TipoPeriodo { TOTAL, PARCIAL, NORMAL }

    public class CuotaCronograma
    {
        public int NumeroMes { get; set; }
        public TipoPeriodo TipoPeriodo { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal Interes { get; set; }
        public decimal Amortizacion { get; set; }
        public decimal SegurosYGastos { get; set; }
        public decimal CuotaBase { get; set; } 
        public decimal CuotaTotalMensual { get; set; }
        public decimal SaldoFinal { get; set; }
    }

    public class SimulacionResultado
    {
        public List<CuotaCronograma> Cronograma { get; set; } = new();
        public decimal MontoPrestamo { get; set; }
        public decimal CuotaFinalVal { get; set; }
        public decimal InteresTotalAcumulado { get; set; }
        public decimal TotalAPagar { get; set; }
        public double Van { get; set; }
        public double Tir { get; set; }
        public double Tcea { get; set; }
    }
}