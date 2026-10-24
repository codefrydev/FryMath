using System;

namespace FryMath.Core.Probability;

public static class Distributions
{
    public static double NormalPdf(double x, double mu = 0.0, double sigma = 1.0)
    {
        if (sigma <= 0.0) return 0.0;
        double z = (x - mu) / sigma;
        return (1.0 / (sigma * Math.Sqrt(2.0 * Math.PI))) * Math.Exp(-0.5 * z * z);
    }

    public static double NormalCdf(double x, double mu = 0.0, double sigma = 1.0)
    {
        if (sigma <= 0.0) return x >= mu ? 1.0 : 0.0;
        double z = (x - mu) / (sigma * Math.Sqrt(2.0));
        return 0.5 * (1.0 + Erf(z));
    }

    public static double BinomialPmf(int k, int n, double p)
    {
        if (k < 0 || k > n || p < 0.0 || p > 1.0) return 0.0;
        double logCoeff = LogFactorial(n) - LogFactorial(k) - LogFactorial(n - k);
        double logProb = logCoeff + k * Math.Log(p) + (n - k) * Math.Log(1.0 - p);
        return Math.Exp(logProb);
    }

    public static double PoissonPmf(int k, double lambda)
    {
        if (k < 0 || lambda <= 0.0) return 0.0;
        double logProb = k * Math.Log(lambda) - lambda - LogFactorial(k);
        return Math.Exp(logProb);
    }

    public static double ExponentialPdf(double x, double lambda)
    {
        if (x < 0.0 || lambda <= 0.0) return 0.0;
        return lambda * Math.Exp(-lambda * x);
    }

    // Chebyshev rational approximation of the error function erf(x)
    public static double Erf(double x)
    {
        double t = 1.0 / (1.0 + 0.5 * Math.Abs(x));
        double tau = t * Math.Exp(-x * x - 1.26551223 +
                     t * (1.00002368 +
                     t * (0.37409196 +
                     t * (0.09678418 +
                     t * (-0.18628806 +
                     t * (0.27886807 +
                     t * (-1.13520398 +
                     t * (1.48851587 +
                     t * (-0.82215223 +
                     t * 0.17087277)))))))));
        return x >= 0 ? 1.0 - tau : tau - 1.0;
    }

    private static double LogFactorial(int n)
    {
        if (n <= 1) return 0.0;
        // Ramanujan approximation for log(n!)
        return n * Math.Log(n) - n + (Math.Log(n * (1.0 + 4.0 * n * (1.0 + 2.0 * n))) / 6.0) + (Math.Log(Math.PI) / 2.0);
    }
}
