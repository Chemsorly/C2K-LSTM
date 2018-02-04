using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyser
{
    //https://msdn.microsoft.com/magazine/mt742873?f=255&MSPPError=-2147217396
    class Statistics
    {
        public static double CalculateP(List<double> pData1, List<double> pData2)
        {
            double[][] data = new double[2][]; // 3 groups
            data[0] = pData1.ToArray();
            data[1] = pData2.ToArray();

            int[] df = null; // degrees of freedom
            double F = Fstat(data, out df);
            if (F == 0d || double.IsNaN(F))
                return 0;
            double pValue = QF(df[0], df[1], F);

            return pValue;
        } // Main

        static double Fstat(double[][] data, out int[] df)
        {
            // calculate F statistic and degrees freedom num, denom
            // assumes data has specific structure:
            // data[0] -> 3, 4, etc (group 1)
            // data[1] -> 8, 12, etc. (group 2)
            // etc.

            int K = data.Length; // number groups
            int[] n = new int[K]; // number items each group
            int N = 0; // total number data points
            for (int i = 0; i < K; ++i)
            {
                n[i] = data[i].Length;
                N += data[i].Length;
            }

            // 1. group means and grand mean
            double[] means = new double[K];
            double gMean = 0.0;
            for (int i = 0; i < K; ++i)
            {
                for (int j = 0; j < data[i].Length; ++j)
                {
                    means[i] += data[i][j];
                    gMean += data[i][j];
                }
                means[i] /= n[i];
            }
            gMean /= N;

            // 2. SSb and MSb
            double SSb = 0.0;
            for (int i = 0; i < K; ++i)
                SSb += n[i] * (means[i] - gMean) * (means[i] - gMean);
            double MSb = SSb / (K - 1);

            // 3. SSw and MSw
            double SSw = 0.0;
            for (int i = 0; i < K; ++i)
                for (int j = 0; j < data[i].Length; ++j)
                    SSw += (data[i][j] - means[i]) * (data[i][j] - means[i]);
            double MSw = SSw / (N - K);

            df = new int[2]; // store df values
            df[0] = K - 1;
            df[1] = N - K;

            double F = MSb / MSw;
            return F;
        } // Fstat

        static double LogGamma(double z)
        {
            // Lanczos approximation g=5, n=7
            double[] coef = new double[7] { 1.000000000190015,
         76.18009172947146, -86.50532032941677,
        24.01409824083091, -1.231739572450155,
        0.1208650973866179e-2, -0.5395239384953e-5 };

            double LogSqrtTwoPi = 0.91893853320467274178;

            if (z < 0.5) // Gamma(z) = Pi / (Sin(Pi*z))* Gamma(1-z))
                return Math.Log(Math.PI / Math.Sin(Math.PI * z)) -
                  LogGamma(1.0 - z);

            double zz = z - 1.0;
            double b = zz + 5.5; // g + 0.5
            double sum = coef[0];

            for (int i = 1; i < coef.Length; ++i)
                sum += coef[i] / (zz + i);

            return (LogSqrtTwoPi + Math.Log(sum) - b) +
              (Math.Log(b) * (zz + 0.5));
        }

        static double BetaInc(double a, double b, double x)
        {
            // Incomplete Beta function
            // A & S 6.6.2 and 26.5.8
            double bt;
            if (x == 0.0 || x == 1.0)
                bt = 0.0;
            else
                bt = Math.Exp(LogGamma(a + b) - LogGamma(a) -
                  LogGamma(b) + a * Math.Log(x) +
                  b * Math.Log(1.0 - x));

            if (x < (a + 1.0) / (a + b + 2.0))
                return bt * BetaIncCf(a, b, x) / a;
            else
                return 1.0 - bt * BetaIncCf(b, a, 1.0 - x) / b;
        }

        static double BetaIncCf(double a, double b, double x)
        {
            // Approximate Incomplete Beta computed by
            // continued fraction
            // A & S 26.5.8 
            int max_it = 100;
            double epsilon = 3.0e-7;
            double small = 1.0e-30;

            int m2; // 2*m
            double aa, del;

            double qab = a + b;
            double qap = a + 1.0;
            double qam = a - 1.0;
            double c = 1.0;
            double d = 1.0 - qab * x / qap;
            if (Math.Abs(d) < small) d = small;
            d = 1.0 / d;
            double result = d;

            int m;
            for (m = 1; m <= max_it; ++m)
            {
                m2 = 2 * m;
                aa = m * (b - m) * x / ((qam + m2) * (a + m2));
                d = 1.0 + aa * d;
                if (Math.Abs(d) < small) d = small;
                c = 1.0 + aa / c;
                if (Math.Abs(c) < small) c = small;
                d = 1.0 / d;
                result *= d * c;
                aa = -(a + m) * (qab + m) * x / ((a + m2) *
                  (qap + m2));
                d = 1.0 + aa * d;
                if (Math.Abs(d) < small) d = small;
                c = 1.0 + aa / c;
                if (Math.Abs(c) < small) c = small;
                d = 1.0 / d;
                del = d * c;
                result *= del;
                if (Math.Abs(del - 1.0) < epsilon) break;
            } // for

            if (m > max_it)
                throw new Exception("BetaIncCf() failure ");
            return result;
        } // BetaCf

        static double PF(double a, double b, double x)
        {
            // approximate lower tail of F-dist
            // (area from 0.0 to x)
            // equivalent to the R pf() function
            // only accurate to about 8 decimals
            double z = (a * x) / (a * x + b);
            return BetaInc(a / 2, b / 2, z);
        }

        static double QF(double a, double b, double x)
        {
            // area from x to +infinity under F
            // for ANOVA = prob(all means equal)
            return 1.0 - PF(a, b, x);
        }

    }
}

