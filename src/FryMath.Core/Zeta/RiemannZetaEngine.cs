using System;
using System.Collections.Generic;
using System.Numerics;

namespace FryMath.Core.Zeta;

public sealed record RiemannZeroInfo(int Index, double ImaginaryPart, double ZValue, double ThetaValue);

public static class RiemannZetaEngine
{
    /// <summary>
    /// First 50 high-precision non-trivial zeros of the Riemann Zeta Function on the critical line Re(s) = 1/2.
    /// </summary>
    public static readonly double[] KnownZeros = new double[]
    {
        14.134725141734693,
        21.022039638771554,
        25.010857580145688,
        30.424876125859513,
        32.935061587739189,
        37.586178158825677,
        40.918719012147495,
        43.327073280914999,
        48.005150881167159,
        49.773832477672302,
        52.970321477714460,
        56.446247697063394,
        59.347044002602353,
        60.831778524609809,
        65.112544048081606,
        67.079810529494173,
        69.546401711173979,
        72.067157674481907,
        75.704691318596541,
        77.144840068874805,
        79.337375020249367,
        82.910380854086030,
        84.735492980517050,
        87.425274613125229,
        88.809111207634465,
        92.491899270558484,
        94.651344040519886,
        95.870633276632488,
        98.831194218193282,
        101.317851005731391,
        103.725538040478344,
        105.446623052326987,
        107.168611144212553,
        111.029535543169674,
        111.874659176992644,
        114.320220915452712,
        116.226680320857553,
        118.790782865976212,
        121.370125002420646,
        122.946829293552588,
        124.256818554345769,
        127.516683879596495,
        129.578704199956050,
        131.087688530932656,
        133.497737202997570,
        134.756509753373872,
        138.116042054533442,
        139.736208952121389,
        141.119719173427403,
        143.111845807620635
    };

    // Precomputed Borwein weights for n = 18
    private static readonly double[] BorweinD;
    private const int BorweinN = 18;

    static RiemannZetaEngine()
    {
        BorweinD = new double[BorweinN];
        for (int k = 0; k < BorweinN; k++)
        {
            double sum = 0.0;
            for (int i = 0; i <= k; i++)
            {
                double term = FactorialRatio(BorweinN + i - 1, BorweinN - i) * Math.Pow(4.0, i) / Factorial(2 * i);
                sum += term;
            }
            BorweinD[k] = BorweinN * sum;
        }
    }

    private static double Factorial(int n)
    {
        double f = 1.0;
        for (int i = 2; i <= n; i++) f *= i;
        return f;
    }

    private static double FactorialRatio(int num, int den)
    {
        double r = 1.0;
        for (int i = den + 1; i <= num; i++) r *= i;
        return r;
    }

    /// <summary>
    /// Computes Riemann Zeta ζ(s) everywhere in C using Borwein's algorithm.
    /// </summary>
    public static Complex EvaluateZeta(Complex s)
    {
        if (Complex.Abs(s - Complex.One) < 1e-12)
        {
            return new Complex(1e12, 0.0); // Simple pole
        }

        double dn = BorweinD[BorweinN - 1];
        Complex eta = Complex.Zero;

        for (int k = 0; k < BorweinN; k++)
        {
            double sign = (k % 2 == 0) ? 1.0 : -1.0;
            double coeff = sign * (BorweinD[k] - dn);
            Complex term = coeff / Complex.Pow(k + 1, s);
            eta += term;
        }

        eta /= -dn;

        Complex factor = Complex.One - Complex.Pow(2.0, Complex.One - s);
        return eta / factor;
    }

    /// <summary>
    /// Computes the Riemann-Siegel Theta function θ(t).
    /// </summary>
    public static double RiemannSiegelTheta(double t)
    {
        if (Math.Abs(t) < 1e-6) return 0.0;
        double absT = Math.Abs(t);

        // High-precision asymptotic expansion for θ(t)
        double theta = (absT * 0.5) * Math.Log(absT / (2.0 * Math.PI)) - (absT * 0.5) - (Math.PI / 8.0)
                     + (1.0 / (48.0 * absT)) + (7.0 / (5760.0 * Math.Pow(absT, 3)));

        return t >= 0 ? theta : -theta;
    }

    /// <summary>
    /// Computes the Riemann-Siegel Z(t) function on the critical line s = 1/2 + it.
    /// Z(t) is strictly real-valued, and its zero-crossings correspond exactly to the non-trivial zeros of ζ(s).
    /// </summary>
    public static double EvaluateSiegelZ(double t)
    {
        Complex s = new(0.5, t);
        Complex zeta = EvaluateZeta(s);
        double theta = RiemannSiegelTheta(t);
        Complex phase = new(Math.Cos(theta), Math.Sin(theta));
        Complex z = phase * zeta;
        return z.Real;
    }

    /// <summary>
    /// Returns the exact prime-counting step function π(x) for x in [2, 120].
    /// </summary>
    public static int ExactPrimeCount(double x)
    {
        if (x < 2.0) return 0;
        int count = 0;
        int max = (int)Math.Floor(x);
        for (int i = 2; i <= max; i++)
        {
            if (IsPrime(i)) count++;
        }
        return count;
    }

    public static bool IsPrime(int n)
    {
        if (n <= 1) return false;
        if (n <= 3) return true;
        if (n % 2 == 0 || n % 3 == 0) return false;
        for (int i = 5; i * i <= n; i += 6)
        {
            if (n % i == 0 || n % (i + 2) == 0) return false;
        }
        return true;
    }

    /// <summary>
    /// Logarithmic Integral Li(x) = ∫_2^x dt / ln(t).
    /// </summary>
    public static double LogarithmicIntegral(double x)
    {
        if (x <= 1.0) return 0.0;
        if (x < 2.0) return (x - 1.0) / Math.Log(x);

        // Numerical integration via Simpson's rule
        int steps = 100;
        double a = 2.0;
        double b = x;
        double h = (b - a) / steps;
        double sum = (1.0 / Math.Log(a)) + (1.0 / Math.Log(b));

        for (int i = 1; i < steps; i++)
        {
            double t = a + i * h;
            sum += (i % 2 == 1 ? 4.0 : 2.0) * (1.0 / Math.Log(t));
        }

        // Add offset Li(2) ≈ 1.04516378
        return (h / 3.0) * sum + 1.04516378;
    }

    /// <summary>
    /// Gram's series for the Riemann prime-counting R(x) function:
    /// R(x) = 1 + Σ_{n=1}^30 (ln x)^n / (n! * n * ζ(n+1))
    /// </summary>
    public static double RiemannR(double x)
    {
        if (x <= 1.0) return 0.0;
        double lnX = Math.Log(x);
        double sum = 1.0;
        double term = 1.0;

        for (int n = 1; n <= 30; n++)
        {
            term *= lnX / n;
            double zetaVal = EvaluateZeta(new Complex(n + 1, 0.0)).Real;
            sum += term / (n * zetaVal);
        }

        return sum;
    }

    /// <summary>
    /// Riemann's Explicit Formula prime count reconstruction using K Riemann zeros:
    /// π_0(x) ≈ R(x) - Σ_{k=1}^K 2 Re( x^(1/2 + i γ_k) / ((1/2 + i γ_k) ln x) )
    /// </summary>
    public static double ExplicitPrimeFormula(double x, int zeroCount)
    {
        if (x < 1.05) return 0.0;
        double rx = RiemannR(x);
        if (zeroCount <= 0) return rx;

        zeroCount = Math.Clamp(zeroCount, 1, KnownZeros.Length);
        double lnX = Math.Log(x);
        double sqrtX = Math.Sqrt(x);
        double correction = 0.0;

        for (int k = 0; k < zeroCount; k++)
        {
            double gamma = KnownZeros[k];
            double denom = 0.25 + gamma * gamma;
            double phase = gamma * lnX;

            // Re( x^(1/2 + i γ) / (1/2 + i γ) ) = sqrt(x) * (0.5 * cos(γ ln x) + γ * sin(γ ln x)) / (0.25 + γ²)
            double wave = sqrtX * (0.5 * Math.Cos(phase) + gamma * Math.Sin(phase)) / (denom * lnX);
            correction += 2.0 * wave;
        }

        return Math.Max(0.0, rx - correction);
    }
}
