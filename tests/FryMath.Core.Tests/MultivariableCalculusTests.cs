using System;
using FryMath.Core.Calculus;
using Xunit;

namespace FryMath.Core.Tests;

public class MultivariableCalculusTests
{
    [Fact]
    public void Surface3D_Paraboloid_HasZeroGradientAndPositiveHessianAtOrigin()
    {
        var surface = new Surface3D(SurfacePreset.Paraboloid);

        var (fx, fy) = surface.ComputeGradient(0.0, 0.0);
        Assert.True(Math.Abs(fx) < 1e-4, $"fx should be 0, got {fx}");
        Assert.True(Math.Abs(fy) < 1e-4, $"fy should be 0, got {fy}");

        var (fxx, fyy, fxy, d) = surface.ComputeHessian(0.0, 0.0);
        Assert.True(Math.Abs(fxx - 1.0) < 0.05, $"fxx should be 1.0, got {fxx}");
        Assert.True(Math.Abs(fyy - 1.0) < 0.05, $"fyy should be 1.0, got {fyy}");
        Assert.True(Math.Abs(fxy) < 0.05, $"fxy should be 0, got {fxy}");
        Assert.True(d > 0.8, $"Hessian determinant D should be > 0.8, got {d}");

        string classification = surface.ClassifyPoint(0.0, 0.0);
        Assert.Contains("Local Minimum", classification);
    }

    [Fact]
    public void Surface3D_HyperbolicSaddle_HasNegativeHessianAtOrigin()
    {
        var surface = new Surface3D(SurfacePreset.HyperbolicSaddle);

        var (fx, fy) = surface.ComputeGradient(0.0, 0.0);
        Assert.True(Math.Abs(fx) < 1e-4);
        Assert.True(Math.Abs(fy) < 1e-4);

        var (fxx, fyy, _, d) = surface.ComputeHessian(0.0, 0.0);
        Assert.True(Math.Abs(fxx - 1.0) < 0.05);
        Assert.True(Math.Abs(fyy - (-1.0)) < 0.05);
        Assert.True(d < -0.8, $"Hessian determinant D should be negative, got {d}");

        string classification = surface.ClassifyPoint(0.0, 0.0);
        Assert.Contains("Saddle Point", classification);
    }

    [Fact]
    public void Surface3D_GradientVector_PointsInSteepestAscentDirection()
    {
        var surface = new Surface3D();
        surface.CompileCustomFormula("x^2 + y^2");

        // Gradient of x^2 + y^2 at (1, 2) is <2x, 2y> = <2, 4>
        var (fx, fy) = surface.ComputeGradient(1.0, 2.0);
        Assert.True(Math.Abs(fx - 2.0) < 0.01, $"fx expected 2, got {fx}");
        Assert.True(Math.Abs(fy - 4.0) < 0.01, $"fy expected 4, got {fy}");
    }

    [Fact]
    public void Surface3D_CustomFormula_CompilesAndEvaluates()
    {
        var surface = new Surface3D();
        surface.CompileCustomFormula("sin(x) + cos(y)");

        double z0 = surface.Evaluate(0.0, 0.0); // sin(0) + cos(0) = 1.0
        Assert.True(Math.Abs(z0 - 1.0) < 1e-5);

        var (fx, fy) = surface.ComputeGradient(0.0, 0.0);
        Assert.True(Math.Abs(fx - 1.0) < 0.01); // cos(0) = 1
        Assert.True(Math.Abs(fy) < 0.01);       // -sin(0) = 0
    }
}
