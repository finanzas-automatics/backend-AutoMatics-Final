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

            // ========================================================
            // 1. CONSTANTES Y MAPEO EXACTO DE FÓRMULAS DE EXCEL
            // ========================================================
            double PV = (double)precioVenta;
            double pCI = (double)porcentajeCuotaInicial;
            double pCF = (double)porcentajeCuotaFinal;
            int N = plazoMeses;
            double Tasa = (double)tasaInteresAnual;
            
            int NDxA = 360;
            int frec = 30;

            // FÓRMULA EXCEL: TEA = SI(tpTasa="TNA"; (1+Tasa/(NDxA/pc))^(NDxA/pc)-1; Tasa)
            double TEA = esTasaEfectiva ? Tasa : Math.Pow(1 + (Tasa / (NDxA / 1.0)), NDxA / 1.0) - 1;

            // FÓRMULA EXCEL: TEM = (1+TEA)^(frec/NDxA)-1
            double TEM = Math.Pow(1 + TEA, (double)frec / NDxA) - 1;

            // FÓRMULAS EXCEL DE GASTOS
            double NCxA = (double)NDxA / frec;
            double pSegDesPer = (double)tasaDesgravamenMensual;
            
            // FÓRMULA EXCEL: SegRePer = pSegRie*PV/NCxA
            double SegRiePer = (double)seguroVehicularMensual * PV / NCxA;
            
            double PortesPer = (double)portesMensuales;
            double GPSPer = 0.0;
            double GasAdmPer = 0.0;

            // FÓRMULAS EXCEL DE MONTOS INICIALES
            double CI = pCI * PV;
            double CF = pCF * PV;
            
            // FÓRMULA EXCEL: Prestamo = PV - CI
            double Prestamo = PV - CI; 

            resultado.MontoPrestamo = (decimal)Prestamo;
            resultado.CuotaFinalVal = (decimal)CF;

            // FÓRMULA EXCEL: Saldo Inicial Cuota final = SI(@NC=1; CF/(1+TEM+pSegDes)^(N+1); G31)
            double SICF_mes1 = CF / Math.Pow(1 + TEM + pSegDesPer, N + 1);

            // FÓRMULA EXCEL: Saldo Inicial Cuota (Regular) = Saldo Total - Saldo Cuota Final
            double SaldoRegular_mes1 = Prestamo - SICF_mes1;

            double SICF = SICF_mes1;
            double SI = SaldoRegular_mes1;

            double[] flujoCaja = new double[N + 2]; 
            flujoCaja[0] = Prestamo; 

            // ========================================================
            // 2. BUCLE DEL CRONOGRAMA (MES A MES)
            // ========================================================
            for (int mes = 1; mes <= N + 1; mes++)
            {
                var cuota = new CuotaCronograma { NumeroMes = mes };

                // Determinar Periodo de Gracia
                if (mes <= N)
                {
                    if (mes <= mesesGraciaTotal) cuota.TipoPeriodo = TipoPeriodo.TOTAL;
                    else if (mes <= mesesGraciaTotal + mesesGraciaParcial) cuota.TipoPeriodo = TipoPeriodo.PARCIAL;
                    else cuota.TipoPeriodo = TipoPeriodo.NORMAL;
                }
                else cuota.TipoPeriodo = TipoPeriodo.NORMAL;

                // --- TABLA DE ABAJO: CRONOGRAMA CUOTA FINAL ---
                // Interes Cuota final = -@SICF*TEM
                double ICF = SICF * TEM;
                
                // Seguro desg. Cuota final = -@SICF*pSegDesPer
                double SegDesCF = SICF * pSegDesPer;
                
                // Amort. Cuota final = SI(@NC=N+1; -@SICF+@ICF+@SegDesCF; 0)
                double ACF = (mes == N + 1) ? (SICF + ICF + SegDesCF) : 0;
                
                // Saldo Final Cuota Final = @SICF - @ICF - @SegDesCF + @ACF
                double SFCF = SICF + ICF + SegDesCF - ACF;

                // --- TABLA DE ABAJO: CRONOGRAMA REGULAR ---
                double I = 0, SegDes = 0, CuotaMensualReg = 0, A = 0, SF = 0;
                
                // SegRie, GPS, Portes, GasAdm
                double SegRie = (mes <= N + 1) ? SegRiePer : 0;
                double GPS = (mes <= N + 1) ? GPSPer : 0;
                double Portes = (mes <= N + 1) ? PortesPer : 0;
                double GasAdm = (mes <= N + 1) ? GasAdmPer : 0;

                if (mes <= N)
                {
                    // Interes = -@SI*TEM
                    I = SI * TEM;
                    
                    // Seguro desg. Cuota = -@SI*pSegDesPer
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
                    else 
                    {
                        // Cuota = PAGO(TEM+pSegDesPer; N-@NC+1; @SI; 0; 0)
                        double rate = TEM + pSegDesPer;
                        int nper = N - mes + 1;
                        CuotaMensualReg = (SI * rate) / (1 - Math.Pow(1 + rate, -nper));
                        
                        // Amort. = @Cuota - @I - @SegDes
                        A = CuotaMensualReg - I - SegDes;
                        
                        // Saldo Final para Cuota
                        SF = SI - A;
                    }
                }

                // --- FLUJO FINAL ---
                // Flujo = @Cuota + @SegRie + @GPS + @Portes + @GasAdm + SI(PG;SegDes;0) + SI(@NC=N+1; ACF; 0)
                double pagoRegular = 0;

                if (mes <= N)
                {
                    pagoRegular = CuotaMensualReg + SegRie + GPS + Portes + GasAdm;
                    if (cuota.TipoPeriodo == TipoPeriodo.TOTAL || cuota.TipoPeriodo == TipoPeriodo.PARCIAL)
                    {
                        pagoRegular += SegDes; 
                    }
                }
                else 
                {
                    pagoRegular = ACF + SegRie + GPS + Portes + GasAdm;
                }

                flujoCaja[mes] = -pagoRegular;

                // --- MAPEO AL OBJETO DE RETORNO FLUTTER ---
                cuota.SaldoInicial = (decimal)(SI + SICF);
                cuota.Interes = (decimal)(I + ICF);
                cuota.Amortizacion = (decimal)(A + ACF);
                cuota.SegurosYGastos = (decimal)(SegDes + SegDesCF + SegRie + GPS + Portes + GasAdm);
                cuota.CuotaBase = (decimal)(A + ACF + I + ICF); 
                cuota.CuotaTotalMensual = (decimal)pagoRegular;
                cuota.SaldoFinal = (decimal)(SF + SFCF);

                if (Math.Abs(cuota.SaldoFinal) < 0.05m) cuota.SaldoFinal = 0;

                resultado.TotalAPagar += cuota.CuotaTotalMensual;
                resultado.InteresTotalAcumulado += cuota.Interes;
                resultado.Cronograma.Add(cuota);

                SICF = SFCF;
                SI = SF;
            }

            // FÓRMULAS EXCEL DE INDICADORES
            // Tasa de descuento = (1+COK)^(frec/NDxA)-1
            double cokMensual = Math.Pow(1 + tasaCokAnual, (double)frec / NDxA) - 1;
            
            resultado.Van = CalcularVAN(flujoCaja, N + 1, cokMensual);
            resultado.Tir = CalcularTIR(flujoCaja, N + 1);
            
            // TCEA de la operación = (1+TIR)^(NDxA/frec)-1
            resultado.Tcea = Math.Pow(1 + resultado.Tir, (double)NDxA / frec) - 1;

            return resultado;
        }
    }
}