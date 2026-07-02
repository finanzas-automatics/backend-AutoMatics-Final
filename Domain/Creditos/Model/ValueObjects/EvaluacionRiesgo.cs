using System;

namespace AutoMatics.Domain.Creditos.Model.ValueObjects
{
    public class EvaluacionRiesgo
    {
        public double ScoreRiesgo { get; private set; }
        public string Clasificacion { get; private set; } = string.Empty;
        public string DecisionAutomatica { get; private set; } = string.Empty;

        protected EvaluacionRiesgo() { }

        public EvaluacionRiesgo(decimal ingresos, decimal cuotaMensual)
        {
            decimal ratioEndeudamiento = cuotaMensual / (ingresos == 0 ? 1 : ingresos);
            ScoreRiesgo = (double)(ratioEndeudamiento * 100);

            if (ratioEndeudamiento <= 0.30m)
            {
                Clasificacion = "A - Riesgo Bajo";
                DecisionAutomatica = "Evaluación Óptima";
            }
            else if (ratioEndeudamiento <= 0.45m)
            {
                Clasificacion = "B - Riesgo Medio";
                DecisionAutomatica = "Requiere Sustento Adicional";
            }
            else
            {
                Clasificacion = "C - Riesgo Alto";
                DecisionAutomatica = "Rechazado por Capacidad de Pago";
            }
        }
    }
}