using System;
using System.Collections.Generic;
using FryMath.Core.Calculus;
using FryMath.Core.CAS;
using FryMath.Core.Dynamics;
using FryMath.Core.LinearAlgebra;
using Xunit;

namespace FryMath.Core.Tests;

public class VectorFieldAndTaylorTests
{
    [Fact]
    public void TaylorSeries_ExpX_YieldsCorrectMaclaurinApproximation()
    {
        var ast = MathParser.Parse("exp(x)");
        var (polyFunc, formula) = TaylorSeries.GenerateTaylorSeries(ast, centerA: 0.0, degree: 4);

        Assert.NotNull(polyFunc);
        Assert.NotEmpty(formula);

        // At x = 1.0: 1 + 1 + 1/2 + 1/6 + 1/24 = 2.708333...
        double approx = polyFunc(1.0);
        double exact = Math.Exp(1.0); // 2.71828...

        Assert.True(Math.Abs(approx - exact) < 0.02, $"Approximation {approx} differs from {exact}");
    }

    [Fact]
    public void TaylorSeries_CosX_ApproximatesCosineAroundZero()
    {
        var ast = MathParser.Parse("cos(x)");
        var (polyFunc, _) = TaylorSeries.GenerateTaylorSeries(ast, centerA: 0.0, degree: 4);

        Assert.NotNull(polyFunc);

        double x = 0.5;
        double approx = polyFunc(x);
        double exact = Math.Cos(x);

        Assert.True(Math.Abs(approx - exact) < 1e-4, $"Approximation {approx} differs from {exact}");
    }

    [Fact]
    public void VectorField2D_StandardVortex_ZeroDivergenceAndConstantCurl()
    {
        var field = new VectorField2D("-y", "x");

        // Divergence: d(-y)/dx + d(x)/dy = 0 + 0 = 0
        // Curl: d(x)/dx - d(-y)/dy = 1 - (-1) = 2
        double divOrigin = field.ComputeDivergence(0, 0);
        double curlOrigin = field.ComputeCurl(0, 0);

        Assert.True(Math.Abs(divOrigin) < 1e-3, $"Divergence should be 0, got {divOrigin}");
        Assert.True(Math.Abs(curlOrigin - 2.0) < 1e-3, $"Curl should be 2, got {curlOrigin}");

        double divPoint = field.ComputeDivergence(2.5, -3.2);
        double curlPoint = field.ComputeCurl(2.5, -3.2);

        Assert.True(Math.Abs(divPoint) < 1e-3, $"Divergence at (2.5, -3.2) should be 0, got {divPoint}");
        Assert.True(Math.Abs(curlPoint - 2.0) < 1e-3, $"Curl at (2.5, -3.2) should be 2, got {curlPoint}");
    }

    [Fact]
    public void VectorField2D_RadialSource_PositiveDivergenceAndZeroCurl()
    {
        var field = new VectorField2D("x", "y");

        // Divergence: dx/dx + dy/dy = 1 + 1 = 2
        // Curl: dy/dx - dx/dy = 0 - 0 = 0
        double div = field.ComputeDivergence(1.0, 1.0);
        double curl = field.ComputeCurl(1.0, 1.0);

        Assert.True(Math.Abs(div - 2.0) < 1e-3, $"Divergence should be 2, got {div}");
        Assert.True(Math.Abs(curl) < 1e-3, $"Curl should be 0, got {curl}");
    }

    [Fact]
    public void VectorField2D_TraceStreamline_ProducesCircularOrbitForVortex()
    {
        var field = new VectorField2D("-y", "x");
        var pts = field.TraceTrajectory(new Vector2D(2.0, 0.0), dt: 0.05, steps: 80);

        Assert.NotEmpty(pts);
        Assert.True(pts.Count > 40);

        // All points along the streamline should be close to radius 2.0
        foreach (var v in pts)
        {
            double r = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            Assert.True(Math.Abs(r - 2.0) < 0.1, $"Radius {r} drifted too far from 2.0");
        }
    }

    [Fact]
    public void ParticleAdvectionEngine_StepsParticlesWithinBounds()
    {
        var field = new VectorField2D("-y", "x");
        var engine = new ParticleAdvectionEngine(150);

        Assert.Equal(150, engine.ParticleCount);

        // Step simulation for 60 frames
        for (int i = 0; i < 60; i++)
        {
            engine.Step(field, dt: 0.016, -5.0, 5.0, -5.0, 5.0, speedMultiplier: 1.0);
        }

        // All particles should remain within domain bounds
        foreach (var p in engine.Particles)
        {
            Assert.True(p.X >= -5.0 && p.X <= 5.0);
            Assert.True(p.Y >= -5.0 && p.Y <= 5.0);
        }
    }
}
