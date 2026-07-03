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
            // Esta función se mantiene por seguridad estructural, aunque el nuevo motor Interbank la calcula dinámicamente.
            if (tem == 0) return (montoPrestamo - montoCuotaFinal) / plazoMeses;
            double factorDescuento = Math.Pow(1 + tem, -plazoMeses);
            decimal valorActualBalon = montoCuotaFinal * (decimal)factorDescuento;
            return (montoPrestamo - valorActualBalon) * (decimal)(tem / (1 - factorDescuento));
        }

        public double CalcularVAN(double[] flujoCaja, int plazoMeses, double tasaCokMensual)
        {
            double van = 0;
            // Se calcula hasta N+1 (plazoMeses incluye el mes extra del cuotón)
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

            // 1. Constantes y Tasas Base
            double PV = (double)precioVenta;
            double pCI = (double)porcentajeCuotaInicial;
            double pCF = (double)porcentajeCuotaFinal;
            int N = plazoMeses;
            double Tasa = (double)tasaInteresAnual;
            
            int NDxA = 360;
            int frec = 30;

            double TEM = esTasaEfectiva 
                ? ConvertirEfectivaAEfectiva(Tasa, NDxA, frec) 
                : ConvertirNominalAEfectiva(Tasa, NDxA, diasCapitalizacion, frec);

            double pSegDesPer = (double)tasaDesgravamenMensual;
            // Si el seguro vehicular es < 1, asumimos que es una tasa porcentual (ej. 0.00030 * PV). Si no, es un monto fijo.
            double SegRiePer = (double)seguroVehicularMensual < 1.0 ? PV * (double)seguroVehicularMensual : (double)seguroVehicularMensual;
            double PortesPer = (double)portesMensuales;
            double GPSPer = 0.0;     // Extensible si a futuro mandas estos datos
            double GasAdmPer = 0.0;

            // 2. Costos Iniciales del Préstamo
            double CI = pCI * PV;
            double CF = pCF * PV;
            double Prestamo = PV - CI;

            resultado.MontoPrestamo = (decimal)Prestamo;
            resultado.CuotaFinalVal = (decimal)CF;

            // ✨ ESTILO INTERBANK: El valor presente del cuotón se descuenta elevándolo a la (N+1)
            double SICF_mes1 = CF / Math.Pow(1 + TEM + pSegDesPer, N + 1);
            double SaldoRegular_mes1 = Prestamo - SICF_mes1;

            // Variables iterativas para los dos cronogramas paralelos
            double SICF = SICF_mes1;
            double SI = SaldoRegular_mes1;

            // El flujo de caja ahora dura N + 1 meses (el mes extra es exclusivo para la cuota final)
            double[] flujoCaja = new double[N + 2]; 
            flujoCaja[0] = Prestamo; 

            for (int mes = 1; mes <= N + 1; mes++)
            {
                var cuota = new CuotaCronograma { NumeroMes = mes };

                if (mes <= N)
                {
                    if (mes <= mesesGraciaTotal) cuota.TipoPeriodo = TipoPeriodo.TOTAL;
                    else if (mes <= mesesGraciaTotal + mesesGraciaParcial) cuota.TipoPeriodo = TipoPeriodo.PARCIAL;
                    else cuota.TipoPeriodo = TipoPeriodo.NORMAL;
                }
                else
                {
                    cuota.TipoPeriodo = TipoPeriodo.NORMAL; // Para el mes N+1 (Cuota Final)
                }

                // ========================================================
                // CRONOGRAMA 1: CUOTA FINAL (BALÓN)
                // ========================================================
                double ICF = SICF * TEM;
                double SegDesCF = SICF * pSegDesPer;
                double ACF = (mes == N + 1) ? (SICF + ICF + SegDesCF) : 0;
                double SFCF = SICF + ICF + SegDesCF - ACF;

                // ========================================================
                // CRONOGRAMA 2: CUOTA REGULAR
                // ========================================================
                double I = 0, SegDes = 0, CuotaMensualReg = 0, A = 0, SF = 0;
                double SegRie = 0, GPS = 0, Portes = 0, GasAdm = 0;

                // Los gastos periódicos fijos se cobran incluso en el mes N+1 (mes 37)
                if (mes <= N + 1)
                {
                    SegRie = SegRiePer;
                    GPS = GPSPer;
                    Portes = PortesPer;
                    GasAdm = GasAdmPer;
                }

                if (mes <= N)
                {
                    I = SI * TEM;
                    SegDes = SI * pSegDesPer;

                    if (cuota.TipoPeriodo == TipoPeriodo.TOTAL)
                    {
                        CuotaMensualReg = 0;
                        A = 0;
                        SF = SI + I;
                    }
                    else if (cuota.TipoPeriodo == TipoPeriodo.PARCIAL)
                    {
                        CuotaMensualReg = I;
                        A = 0;
                        SF = SI;
                    }
                    else // NORMAL
                    {
                        // ✨ FÓRMULA EXCEL INTERBANK: =PAGO(TEM+pSegDesPer; N-@NC+1; @SI; 0; 0)
                        double rate = TEM + pSegDesPer;
                        int nper = N - mes + 1;
                        CuotaMensualReg = (SI * rate) / (1 - Math.Pow(1 + rate, -nper));
                        
                        A = CuotaMensualReg - I - SegDes;
                        SF = SI - A;
                    }
                }

                // ========================================================
                // CONSOLIDADO Y FLUJO DE CAJA
                // ========================================================
                double pagoRegular = 0;

                if (mes <= N)
                {
                    pagoRegular = CuotaMensualReg + SegRie + GPS + Portes + GasAdm;
                    // Regla bancaria: En periodo de gracia se sigue pagando el seguro de desgravamen de bolsillo
                    if (cuota.TipoPeriodo == TipoPeriodo.TOTAL || cuota.TipoPeriodo == TipoPeriodo.PARCIAL)
                    {
                        pagoRegular += SegDes; 
                    }
                }
                else // mes == N+1 (El gran cuotón)
                {
                    pagoRegular = ACF + SegRie + GPS + Portes + GasAdm;
                }

                flujoCaja[mes] = -pagoRegular;

                // Armado del objeto visual para Flutter sumando ambos cronogramas
                cuota.SaldoInicial = (decimal)(SI + SICF);
                cuota.Interes = (decimal)(I + ICF);
                cuota.Amortizacion = (decimal)(A + ACF);
                cuota.SegurosYGastos = (decimal)(SegDes + SegDesCF + SegRie + GPS + Portes + GasAdm);
                cuota.CuotaBase = (decimal)(A + ACF + I + ICF); // Puramente Amortización + Intereses totales
                cuota.CuotaTotalMensual = (decimal)pagoRegular;
                cuota.SaldoFinal = (decimal)(SF + SFCF);

                // Forzar a cero absoluto el último céntimo para evitar notación científica
                if (Math.Abs(cuota.SaldoFinal) < 0.05m) cuota.SaldoFinal = 0;

                resultado.TotalAPagar += cuota.CuotaTotalMensual;
                resultado.InteresTotalAcumulado += cuota.Interes;
                resultado.Cronograma.Add(cuota);

                // Avanzar saldos al siguiente mes
                SICF = SFCF;
                SI = SF;
            }

            // 3. Indicadores Finales (VAN, TIR, TCEA) calculados con N+1 flujos
            double cokMensual = ConvertirEfectivaAEfectiva((double)tasaCokAnual, NDxA, frec);
            resultado.Van = CalcularVAN(flujoCaja, N + 1, cokMensual);
            resultado.Tir = CalcularTIR(flujoCaja, N + 1);
            resultado.Tcea = Math.Pow(1 + resultado.Tir, (double)NDxA / frec) - 1;

            return resultado;
        }
    }
}