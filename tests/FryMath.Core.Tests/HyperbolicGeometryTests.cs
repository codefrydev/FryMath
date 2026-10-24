using System;
using FryMath.Core.Hyperbolic;
using Xunit;

namespace FryMath.Core.Tests;

public class HyperbolicGeometryTests
{
    [Fact]
    public void HyperbolicDistance_BetweenOriginAndRadius_MatchesAnalyticalFormula()
    {
        var p0 = new HyperbolicPoint(0, 0);
        var p1 = new HyperbolicPoint(0.5, 0);

        double dist = HyperbolicGeometryEngine.HyperbolicDistance(p0, p1);

        // Exact formula: 2 * artanh(0.5) = ln((1 + 0.5)/(1 - 0.5)) = ln(3) ≈ 1.098612
        double expected = Math.Log(3.0);
        Assert.True(Math.Abs(dist - expected) < 1e-5, $"Hyperbolic distance should match ln(3)! Was {dist:F6}");
    }

    [Fact]
    public void ConstructGeodesicArc_OriginAndRadialPoint_ReturnsDiameter()
    {
        var p0 = new HyperbolicPoint(0, 0);
        var p1 = new HyperbolicPoint(0.4, 0.4);

        var arc = HyperbolicGeometryEngine.ConstructGeodesicArc(p0, p1);
        Assert.True(arc.IsDiameter, "Geodesic through origin must be a Euclidean diameter line!");
    }

    [Fact]
    public void ConstructGeodesicArc_GeneralPoints_ProducesCircleOrthogonalToBoundary()
    {
        var p1 = new HyperbolicPoint(0.5, 0.2);
        var p2 = new HyperbolicPoint(0.2, 0.6);

        var arc = HyperbolicGeometryEngine.ConstructGeodesicArc(p1, p2);
        Assert.False(arc.IsDiameter);

        // Orthogonality condition to unit circle: c_x² + c_y² = R² + 1²
        double centerNormSq = arc.CenterX * arc.CenterX + arc.CenterY * arc.CenterY;
        double expectedCenterNormSq = arc.Radius * arc.Radius + 1.0;

        Assert.True(Math.Abs(centerNormSq - expectedCenterNormSq) < 1e-4,
            $"Geodesic circle must be strictly orthogonal to the boundary circle |z|=1! Left={centerNormSq}, Right={expectedCenterNormSq}");
    }

    [Fact]
    public void SetInteractiveTriangle_GaussBonnet_VerifiesAngleSumDefect()
    {
        var engine = new HyperbolicGeometryEngine();
        var a = new HyperbolicPoint(0.0, 0.0);
        var b = new HyperbolicPoint(0.5, 0.0);
        var c = new HyperbolicPoint(0.0, 0.5);

        engine.SetInteractiveTriangle(a, b, c);
        var tri = engine.ActiveTriangle;
        Assert.NotNull(tri);

        // Hyperbolic angle sum MUST be strictly less than π
        double angleSum = tri.AngleA + tri.AngleB + tri.AngleC;
        Assert.True(angleSum < Math.PI, $"Hyperbolic triangle angle sum must be strictly less than π! Was {angleSum:F4}");

        // Gauss-Bonnet: Area = π - (A + B + C)
        double expectedArea = Math.PI - angleSum;
        Assert.True(Math.Abs(tri.HyperbolicArea - expectedArea) < 1e-5);
    }

    [Fact]
    public void IdealTriangle_HasMaximumAreaPi()
    {
        var engine = new HyperbolicGeometryEngine();
        engine.LoadPreset(HyperbolicTilingPreset.IdealTriangle);

        var tri = engine.ActiveTriangle;
        Assert.NotNull(tri);
        Assert.True(Math.Abs(tri.HyperbolicArea - Math.PI) < 1e-4, $"Ideal triangle must achieve maximal area π! Was {tri.HyperbolicArea:F5}");
    }
}
