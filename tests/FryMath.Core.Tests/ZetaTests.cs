using System;
using System.Numerics;
using FryMath.Core.Zeta;
using Xunit;

namespace FryMath.Core.Tests;

public class ZetaTests
{
    [Fact]
    public void EvaluateZeta_KnownRealValues_MatchesAnalyticalResults()
    {
        // ζ(2) = π² / 6 ≈ 1.6449340668
        var z2 = RiemannZetaEngine.EvaluateZeta(new Complex(2.0, 0.0));
        Assert.Equal(Math.PI * Math.PI / 6.0, z2.Real, 4);
        Assert.True(Math.Abs(z2.Imaginary) < 1e-6);

        // ζ(4) = π⁴ / 90 ≈ 1.0823232337
        var z4 = RiemannZetaEngine.EvaluateZeta(new Complex(4.0, 0.0));
        Assert.Equal(Math.Pow(Math.PI, 4) / 90.0, z4.Real, 4);
    }

    [Fact]
    public void EvaluateZeta_FirstRiemannZero_VanishesOnCriticalLine()
    {
        double gamma1 = RiemannZetaEngine.KnownZeros[0]; // 14.134725...
        var z = RiemannZetaEngine.EvaluateZeta(new Complex(0.5, gamma1));

        // Magnitude should be near 0 at the zero
        Assert.True(z.Magnitude < 0.015, $"Expected |ζ(1/2 + i*14.134)| ≈ 0, but got {z.Magnitude}");

        // Riemann-Siegel Z(t) function must cross zero at gamma1
        double siegelZ = RiemannZetaEngine.EvaluateSiegelZ(gamma1);
        Assert.True(Math.Abs(siegelZ) < 0.015, $"Expected Z(14.134) ≈ 0, but got {siegelZ}");
    }

    [Fact]
    public void ExactPrimeCount_MatchesSmallPrimes()
    {
        Assert.Equal(0, RiemannZetaEngine.ExactPrimeCount(1.5));
        Assert.Equal(1, RiemannZetaEngine.ExactPrimeCount(2.0));
        Assert.Equal(2, RiemannZetaEngine.ExactPrimeCount(3.0));
        Assert.Equal(4, RiemannZetaEngine.ExactPrimeCount(10.0)); // 2, 3, 5, 7
        Assert.Equal(8, RiemannZetaEngine.ExactPrimeCount(20.0)); // 2, 3, 5, 7, 11, 13, 17, 19
        Assert.Equal(25, RiemannZetaEngine.ExactPrimeCount(100.0));
    }

    [Fact]
    public void ExplicitPrimeFormula_ApproximatesPrimeStaircase()
    {
        // At x = 20, π(20) = 8.
        double r20 = RiemannZetaEngine.RiemannR(20.0);
        Assert.InRange(r20, 7.0, 9.5);

        // With zeros, explicit formula oscillates around the true step count
        double approx = RiemannZetaEngine.ExplicitPrimeFormula(20.0, 5);
        Assert.InRange(approx, 6.5, 9.5);
    }
}
