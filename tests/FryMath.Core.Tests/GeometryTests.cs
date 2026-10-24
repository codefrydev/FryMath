using System;
using System.Numerics;
using FryMath.Core.Geometry;
using Xunit;

namespace FryMath.Core.Tests;

public class GeometryTests
{
    [Fact]
    public void CircularHelix_FrenetFrame_IsOrthonormal()
    {
        var frame = SpaceCurveEngine.ComputeFrenetFrame(SpaceCurvePreset.CircularHelix, t: 1.5);

        // Check unit lengths
        Assert.Equal(1.0, frame.Tangent.Length(), 3);
        Assert.Equal(1.0, frame.Normal.Length(), 3);
        Assert.Equal(1.0, frame.Binormal.Length(), 3);

        // Check mutual orthogonality
        float dotTN = Vector3.Dot(frame.Tangent, frame.Normal);
        float dotTB = Vector3.Dot(frame.Tangent, frame.Binormal);
        float dotNB = Vector3.Dot(frame.Normal, frame.Binormal);

        Assert.True(Math.Abs(dotTN) < 1e-3, $"T.N should be 0, was {dotTN}");
        Assert.True(Math.Abs(dotTB) < 1e-3, $"T.B should be 0, was {dotTB}");
        Assert.True(Math.Abs(dotNB) < 1e-3, $"N.B should be 0, was {dotNB}");
    }

    [Fact]
    public void CircularHelix_CurvatureAndTorsion_MatchAnalyticalFormulas()
    {
        // For helix r(t) = <a cos t, a sin t, b t> with a=2.0, b=0.6:
        // kappa = a / (a^2 + b^2) = 2.0 / (4.0 + 0.36) = 2.0 / 4.36 = 0.4587
        // tau = b / (a^2 + b^2) = 0.6 / 4.36 = 0.1376
        double expectedKappa = 2.0 / (4.0 + 0.36);
        double expectedTau = 0.6 / (4.0 + 0.36);

        var frame = SpaceCurveEngine.ComputeFrenetFrame(SpaceCurvePreset.CircularHelix, t: 2.0);

        Assert.Equal(expectedKappa, frame.Curvature, 2);
        Assert.Equal(expectedTau, frame.Torsion, 2);
        Assert.True(frame.RadiusOfCurvature > 0);
    }

    [Fact]
    public void TrefoilKnot_IsClosedCurve()
    {
        Vector3 r0 = SpaceCurveEngine.EvaluateCurve(SpaceCurvePreset.TrefoilKnot, 0.0);
        Vector3 r2Pi = SpaceCurveEngine.EvaluateCurve(SpaceCurvePreset.TrefoilKnot, 2.0 * Math.PI);

        Assert.Equal(r0.X, r2Pi.X, 2);
        Assert.Equal(r0.Y, r2Pi.Y, 2);
        Assert.Equal(r0.Z, r2Pi.Z, 2);
    }
}
