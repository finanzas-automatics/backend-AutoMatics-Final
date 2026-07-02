using System;
using AutoMatics.Domain.Creditos.Model.ValueObjects;

namespace AutoMatics.Domain.Creditos.Services
{
    public class MotorFinancieroDomainService
    {
        public double ConvertirEfectivaAEfectiva(double valorTasa, int diasOrigen, int diasDestino) => 
            Math.Pow(1 + valorTasa, (double)diasDestino / diasOrigen) - 1;

        public double ConvertirNominalAEfectiva(double valorTasa, int diasNominal, int diasCapitalizacion, int diasDestino) => 
            Math.Pow(1 + (valorTasa / ((double)diasNominal / diasCapitalizacion)), (double)diasDestino / diasCapitalizacion) - 1;

        public decimal CalcularCuotaFrancesa(decimal montoPrestamo, decimal montoCuotaFinal, double tem, int plazoMeses)
        {
            if (tem == 0) return (montoPrestamo - montoCuotaFinal) / plazoMeses;
            double factorDescuento = Math.Pow(1 + tem, -plazoMeses);
            decimal valorActualBalon = montoCuotaFinal * (decimal)factorDescuento;
            return (montoPrestamo - valorActualBalon) * (decimal)(tem / (1 - factorDescuento));
        }

        public double CalcularVAN(double[] flujoCaja, int plazoMeses, double tasaCokMensual)
        {
            double van = 0;
            for (int t = 0; t <= plazoMeses; t++) van += flujoCaja[t] / Math.Pow(1 + tasaCokMensual, t);
            return van;
        }

        public double CalcularTIR(double[] flujoCaja, int plazoMeses)
        {
            double limiteInf = 0.0001, limiteSup = 1.0, tirEstimada = 0;
            for (int iteracion = 0; iteracion < 150; iteracion++)
            {
                tirEstimada = (limiteInf + limiteSup) / 2;
                double vanEstimado = 0;
                for (int t = 0; t <= plazoMeses; t++) vanEstimado += flujoCaja[t] / Math.Pow(1 + tirEstimada, t);
                
                // ✨ MAGIA: Corrección de bisección. Si el VAN deudor es mayor a 0, la tasa es muy alta.
                if (vanEstimado > 0) limiteSup = tirEstimada; 
                else limiteInf = tirEstimada;
            }
            return tirEstimada;
        }

        public SimulacionResultado GenerarCronograma(
            decimal precioVenta, decimal porcentajeCuotaInicial, int plazoMeses, double tasaInteresAnual, 
            bool esTasaEfectiva, int diasCapitalizacion, int mesesGraciaTotal, int mesesGraciaParcial, 
            decimal porcentajeCuotaFinal, decimal tasaDesgravamenMensual, decimal seguroVehicularMensual, 
            decimal portesMensuales, double tasaCokAnual)
        {
            var resultado = new SimulacionResultado();
            
            // ✨ Eliminadas las divisiones / 100 porque Flutter ya manda los decimales (ej. 0.2)
            decimal cuotaInicialMonto = precioVenta * porcentajeCuotaInicial;
            resultado.MontoPrestamo = precioVenta - cuotaInicialMonto;
            resultado.CuotaFinalVal = precioVenta * porcentajeCuotaFinal;

            // ✨ TasaInteresAnual y tasaCokAnual ya vienen en 0.15 y 0.1, no dividir entre 100
            double tem = esTasaEfectiva ? ConvertirEfectivaAEfectiva(tasaInteresAnual, 360, 30) : ConvertirNominalAEfectiva(tasaInteresAnual, 360, diasCapitalizacion, 30);
            double cokMensual = ConvertirEfectivaAEfectiva(tasaCokAnual, 360, 30);

            decimal saldoInsoluto = resultado.MontoPrestamo;
            double[] flujoCaja = new double[plazoMeses + 1];
            flujoCaja[0] = (double)resultado.MontoPrestamo; 

            decimal cuotaFijaBase = 0;
            int mesesAmortizacion = plazoMeses - (mesesGraciaTotal + mesesGraciaParcial);

            for (int mes = 1; mes <= plazoMeses; mes++)
            {
                var cuota = new CuotaCronograma { NumeroMes = mes, SaldoInicial = saldoInsoluto };
                if (mes <= mesesGraciaTotal) cuota.TipoPeriodo = TipoPeriodo.TOTAL;
                else if (mes <= mesesGraciaTotal + mesesGraciaParcial) cuota.TipoPeriodo = TipoPeriodo.PARCIAL;
                else cuota.TipoPeriodo = TipoPeriodo.NORMAL;

                cuota.Interes = cuota.SaldoInicial * (decimal)tem;
                resultado.InteresTotalAcumulado += cuota.Interes;

                if (cuota.TipoPeriodo == TipoPeriodo.TOTAL) 
                { 
                    cuota.Amortizacion = 0; 
                    cuota.CuotaBase = 0; 
                    cuota.SaldoFinal = cuota.SaldoInicial + cuota.Interes; 
                }
                else if (cuota.TipoPeriodo == TipoPeriodo.PARCIAL) 
                { 
                    cuota.Amortizacion = 0; 
                    cuota.CuotaBase = cuota.Interes; 
                    cuota.SaldoFinal = cuota.SaldoInicial; 
                }
                else 
                {
                    // ✨ Calculamos la cuota francesa SOLO UNA VEZ basándonos en el saldo después de las gracias
                    if (cuotaFijaBase == 0 && mesesAmortizacion > 0)
                    {
                        cuotaFijaBase = CalcularCuotaFrancesa(cuota.SaldoInicial, resultado.CuotaFinalVal, tem, mesesAmortizacion);
                    }
                    
                    cuota.Amortizacion = cuotaFijaBase - cuota.Interes; 
                    cuota.CuotaBase = cuotaFijaBase; 
                    
                    if (mes == plazoMeses) 
                    {
                        cuota.Amortizacion += resultado.CuotaFinalVal;
                        cuota.CuotaBase += resultado.CuotaFinalVal;
                    }
                    
                    cuota.SaldoFinal = cuota.SaldoInicial - cuota.Amortizacion;
                    
                    // Control de decimales para que el último saldo cierre exactamente en 0
                    if (Math.Abs(cuota.SaldoFinal) < 0.1m) cuota.SaldoFinal = 0;
                }

                cuota.SegurosYGastos = (cuota.SaldoInicial * tasaDesgravamenMensual) + seguroVehicularMensual + portesMensuales;
                cuota.CuotaTotalMensual = cuota.CuotaBase + cuota.SegurosYGastos;

                saldoInsoluto = cuota.SaldoFinal;
                resultado.TotalAPagar += cuota.CuotaTotalMensual;
                flujoCaja[mes] = -(double)cuota.CuotaTotalMensual;
                resultado.Cronograma.Add(cuota);
            }

            resultado.Van = CalcularVAN(flujoCaja, plazoMeses, cokMensual);
            resultado.Tir = CalcularTIR(flujoCaja, plazoMeses);
            resultado.Tcea = Math.Pow(1 + resultado.Tir, 12) - 1;

            return resultado;
        }
    }
}