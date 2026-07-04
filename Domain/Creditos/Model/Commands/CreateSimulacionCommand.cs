namespace AutoMatics.Domain.Creditos.Model.Commands
{
    public record CreateSimulacionCommand(
        int ClienteId, int VehiculoId, int UsuarioId, decimal PrecioVenta, string Moneda,
        decimal PorcentajeCuotaInicial, int PlazoMeses, double TasaInteresAnual, bool EsTasaEfectiva,
        int DiasCapitalizacion, int MesesGraciaTotal, int MesesGraciaParcial, decimal PorcentajeCuotaFinal,
        decimal TasaDesgravamenMensual, decimal SeguroVehicularMensual, decimal PortesMensuales, double TasaCokAnual,
        // ✨ NUEVOS CAMPOS AÑADIDOS
        decimal CostesNotariales, bool FinanciarNotariales,
        decimal CostesRegistrales, bool FinanciarRegistrales,
        decimal Tasacion, bool FinanciarTasacion,
        decimal ComisionEstudio, bool FinanciarEstudio,
        decimal ComisionActivacion, bool FinanciarActivacion,
        decimal GpsMensual, decimal GastosAdmMensuales
    );
}