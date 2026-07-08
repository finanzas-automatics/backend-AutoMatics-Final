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
            decimal portesMensuales, double tasaCokAnual,
            decimal costesNotariales, bool financiarNotariales,
            decimal costesRegistrales, bool financiarRegistrales,
            decimal tasacion, bool financiarTasacion,
            decimal comisionEstudio, bool financiarEstudio,
            decimal comisionActivacion, bool financiarActivacion,
            decimal gpsMensual, decimal gastosAdmMensuales)
        {
            var resultado = new SimulacionResultado();

            double PV = (double)precioVenta;
            double pCI = (double)porcentajeCuotaInicial;
            double pCF = (double)porcentajeCuotaFinal;
            int N = plazoMeses;
            double Tasa = (double)tasaInteresAnual;

            int NDxA = 360;
            int frec = 30;

            double periodosCapitalizacion = (double)NDxA / diasCapitalizacion;

            double TEA = esTasaEfectiva ? Tasa : Math.Pow(1 + (Tasa / periodosCapitalizacion), periodosCapitalizacion) - 1;

            double TEM = Math.Pow(1 + TEA, (double)frec / NDxA) - 1;

            double NCxA = (double)NDxA / frec;
            double pSegDesPer = (double)tasaDesgravamenMensual;

            double SegRiePer = (double)seguroVehicularMensual * PV / NCxA;

            double PortesPer = (double)portesMensuales;
            double GPSPer = (double)gpsMensual;
            double GasAdmPer = (double)gastosAdmMensuales;

            double CI = pCI * PV;
            double CF = pCF * PV;

            double Prestamo = PV - CI;

            if (financiarNotariales) Prestamo += (double)costesNotariales;
            if (financiarRegistrales) Prestamo += (double)costesRegistrales;
            if (financiarTasacion) Prestamo += (double)tasacion;
            if (financiarEstudio) Prestamo += (double)comisionEstudio;
            if (financiarActivacion) Prestamo += (double)comisionActivacion;

            resultado.MontoPrestamo = (decimal)Prestamo;
            resultado.CuotaFinalVal = (decimal)CF;

            double SICF_mes1 = CF / Math.Pow(1 + TEM + pSegDesPer, N + 1);

            double SaldoRegular_mes1 = Prestamo - SICF_mes1;

            double SICF = SICF_mes1;
            double SI = SaldoRegular_mes1;

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
                else cuota.TipoPeriodo = TipoPeriodo.NORMAL;

                double ICF = SICF * TEM;

                double SegDesCF = SICF * pSegDesPer;

                double ACF = (mes == N + 1) ? (SICF + ICF + SegDesCF) : 0;

                double SFCF = SICF + ICF + SegDesCF - ACF;

                double I = 0, SegDes = 0, CuotaMensualReg = 0, A = 0, SF = 0;

                double SegRie = (mes <= N + 1) ? SegRiePer : 0;
                double GPS = (mes <= N + 1) ? GPSPer : 0;
                double Portes = (mes <= N + 1) ? PortesPer : 0;
                double GasAdm = (mes <= N + 1) ? GasAdmPer : 0;

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
                    else
                    {

                        double rate = TEM + pSegDesPer;
                        int nper = N - mes + 1;
                        CuotaMensualReg = (SI * rate) / (1 - Math.Pow(1 + rate, -nper));

                        A = CuotaMensualReg - I - SegDes;

                        SF = SI - A;
                    }
                }

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

            double cokMensual = Math.Pow(1 + tasaCokAnual, (double)frec / NDxA) - 1;

            resultado.Van = CalcularVAN(flujoCaja, N + 1, cokMensual);
            resultado.Tir = CalcularTIR(flujoCaja, N + 1);

            resultado.Tcea = Math.Pow(1 + resultado.Tir, (double)NDxA / frec) - 1;

            return resultado;
        }
    }
}