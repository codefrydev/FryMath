using System;
using System.Linq;
using FryMath.Core.Cryptography;
using Xunit;

namespace FryMath.Core.Tests;

public class EllipticCurveTests
{
    [Fact]
    public void Discriminant_NonSingularAndSingularCurves_CorrectlyIdentified()
    {
        var engine = new EllipticCurveEngine();

        // Standard smooth curve: y^2 = x^3 - 3x + 3
        engine.A = -3;
        engine.B = 3;
        // Delta = -16 * (4*(-27) + 27*9) = -16 * (-108 + 243) = -16 * 135 = -2160 != 0
        Assert.False(engine.IsSingular);
        Assert.True(engine.Discriminant < 0);

        // Nodal curve: y^2 = x^3 - 3x + 2 (4*(-27) + 27*4 = 0)
        engine.ApplyNodalSingularPreset();
        Assert.True(engine.IsSingular);

        // Cusp curve: y^2 = x^3 (a=0, b=0)
        engine.ApplyCuspSingularPreset();
        Assert.True(engine.IsSingular);
    }

    [Fact]
    public void RealPointAddition_DistinctPoints_SumLiesOnCurve()
    {
        var engine = new EllipticCurveEngine { A = -3.0, B = 3.0 };
        var p = engine.FindPointClosestToX(-1.5, positiveY: true);
        var q = engine.FindPointClosestToX(1.5, positiveY: true);

        Assert.True(engine.IsOnCurve(p));
        Assert.True(engine.IsOnCurve(q));

        var (sum, slope, intercept, rPrime) = engine.AddReal(p, q);

        Assert.False(sum.IsInfinity);
        Assert.True(engine.IsOnCurve(sum));
        Assert.Equal(-rPrime.Y, sum.Y, 4);
        Assert.Equal(rPrime.X, sum.X, 4);
    }

    [Fact]
    public void RealPointDoubling_TangentLine_SumLiesOnCurve()
    {
        var engine = new EllipticCurveEngine { A = -3.0, B = 3.0 };
        var p = engine.FindPointClosestToX(1.0, positiveY: true);

        Assert.True(engine.IsOnCurve(p));

        var (doubleP, slope, intercept, rPrime) = engine.AddReal(p, p);

        Assert.False(doubleP.IsInfinity);
        Assert.True(engine.IsOnCurve(doubleP));
    }

    [Fact]
    public void RealPointAddition_OppositePoints_YieldsInfinity()
    {
        var engine = new EllipticCurveEngine { A = -3.0, B = 3.0 };
        var p = engine.FindPointClosestToX(1.5, positiveY: true);
        var negP = new RealPoint(p.X, -p.Y);

        var (sum, _, _, _) = engine.AddReal(p, negP);
        Assert.True(sum.IsInfinity);
    }

    [Fact]
    public void FiniteFieldArithmetic_ModInverseAndModSqrt_Exact()
    {
        long p = 97;
        for (long a = 1; a < p; a++)
        {
            long inv = EllipticCurveEngine.ModInverse(a, p);
            Assert.Equal(1, EllipticCurveEngine.Mod(a * inv, p));
        }

        // Test quadratic residues and square roots on F_97
        for (long x = 1; x < p; x++)
        {
            long sq = EllipticCurveEngine.Mod(x * x, p);
            Assert.True(EllipticCurveEngine.IsQuadraticResidue(sq, p));
            long? root = EllipticCurveEngine.ModSqrt(sq, p);
            Assert.NotNull(root);
            Assert.True(root == x || root == p - x);
        }
    }

    [Fact]
    public void FiniteFieldPoints_SatisfiesHasseTheorem()
    {
        long p = 89;
        long a = -3;
        long b = 7;

        var points = EllipticCurveEngine.GetFinitePoints(a, b, p);
        int count = points.Count; // Includes point at infinity

        // Hasse's Theorem: |#E(F_p) - (p + 1)| <= 2 * sqrt(p)
        double bound = 2.0 * Math.Sqrt(p);
        double diff = Math.Abs(count - (p + 1));
        Assert.True(diff <= bound, $"Hasse bound violated: diff={diff}, bound={bound}");

        // Verify every point actually satisfies y^2 = x^3 + ax + b mod p
        foreach (var pt in points.Where(pt => !pt.IsInfinity))
        {
            long lhs = EllipticCurveEngine.Mod(pt.Y * pt.Y, p);
            long rhs = EllipticCurveEngine.Mod(pt.X * pt.X % p * pt.X + a * pt.X + b, p);
            Assert.Equal(lhs, rhs);
        }
    }

    [Fact]
    public void FinitePointAdditionAndMultiplication_AssociativeAndConsistent()
    {
        long p = 97;
        long a = 0;
        long b = 7; // secp256k1 curve mod 97

        var points = EllipticCurveEngine.GetFinitePoints(a, b, p);
        var g = points.First(pt => !pt.IsInfinity);

        var g2 = EllipticCurveEngine.AddFinite(g, g, a, p);
        var g2Mult = EllipticCurveEngine.MultiplyFinite(g, 2, a, p);
        Assert.Equal(g2, g2Mult);

        var g3 = EllipticCurveEngine.AddFinite(g2, g, a, p);
        var g3Mult = EllipticCurveEngine.MultiplyFinite(g, 3, a, p);
        Assert.Equal(g3, g3Mult);
    }

    [Fact]
    public void EcdhKeyExchange_AliceAndBobDeriveSameSharedSecret()
    {
        var engine = new EllipticCurveEngine();
        engine.ApplyEcdhPreset();

        var (alicePub, bobPub, sharedAlice, sharedBob, success) = engine.RunEcdh();

        Assert.True(success);
        Assert.Equal(sharedAlice, sharedBob);
        Assert.False(sharedAlice.IsInfinity);
    }
}
