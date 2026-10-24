using System;
using System.Collections.Generic;
using FryMath.Core.CAS;
using FryMath.Core.LinearAlgebra;

namespace FryMath.Core.Calculus;

public enum SurfacePreset
{
    Paraboloid,
    HyperbolicSaddle,
    Sombrero,
    GaussianHill,
    MonkeySaddle,
    WaveRipple,
    EggCrate
}

public readonly record struct SurfacePoint3D(double X, double Y, double Z, double Depth, double ScreenX, double ScreenY);

public readonly record struct SurfaceQuad(
    SurfacePoint3D P0,
    SurfacePoint3D P1,
    SurfacePoint3D P2,
    SurfacePoint3D P3,
    double AvgZ,
    double AvgDepth
);

/// <summary>
/// Multivariable calculus engine for 3D surfaces z = f(x, y).
/// Computes partial derivatives, gradient vectors, tangent planes, and Hessian classification.
/// </summary>
public sealed class Surface3D
{
    public SurfacePreset Preset { get; private set; } = SurfacePreset.Paraboloid;
    public string Formula { get; private set; } = "x^2 + y^2";
    public Func<double, double, double> Evaluator { get; private set; } = (x, y) => x * x + y * y;

    public Surface3D(SurfacePreset preset = SurfacePreset.Paraboloid)
    {
        SetPreset(preset);
    }

    public void SetPreset(SurfacePreset preset)
    {
        Preset = preset;
        Evaluator = preset switch
        {
            SurfacePreset.Paraboloid => (x, y) => 0.5 * (x * x + y * y),
            SurfacePreset.HyperbolicSaddle => (x, y) => 0.5 * (x * x - y * y),
            SurfacePreset.Sombrero => (x, y) =>
            {
                double r = Math.Sqrt(x * x + y * y);
                return r < 1e-4 ? 2.5 : 2.5 * Math.Sin(r * 2.0) / (r * 2.0);
            },
            SurfacePreset.GaussianHill => (x, y) => 3.0 * Math.Exp(-0.4 * (x * x + y * y)),
            SurfacePreset.MonkeySaddle => (x, y) => 0.25 * (x * x * x - 3.0 * x * y * y),
            SurfacePreset.WaveRipple => (x, y) => 1.5 * Math.Sin(x) * Math.Cos(y),
            SurfacePreset.EggCrate => (x, y) => 1.2 * (Math.Sin(x) + Math.Sin(y)),
            _ => (x, y) => 0.5 * (x * x + y * y)
        };

        Formula = preset switch
        {
            SurfacePreset.Paraboloid => "0.5 * (x^2 + y^2)",
            SurfacePreset.HyperbolicSaddle => "0.5 * (x^2 - y^2)",
            SurfacePreset.Sombrero => "2.5 * sin(2*r) / (2*r)",
            SurfacePreset.GaussianHill => "3 * exp(-0.4*(x^2 + y^2))",
            SurfacePreset.MonkeySaddle => "0.25 * (x^3 - 3*x*y^2)",
            SurfacePreset.WaveRipple => "1.5 * sin(x) * cos(y)",
            SurfacePreset.EggCrate => "1.2 * (sin(x) + sin(y))",
            _ => "0.5 * (x^2 + y^2)"
        };
    }

    public void CompileCustomFormula(string formula)
    {
        Formula = formula;
        try
        {
            var ast = MathParser.Parse(formula);
            Evaluator = ExpressionCompiler.Compile2D(ast, "x", "y");
        }
        catch
        {
            Evaluator = (x, y) => 0.5 * (x * x + y * y);
        }
    }

    public double Evaluate(double x, double y)
    {
        try
        {
            double z = Evaluator(x, y);
            if (double.IsNaN(z) || double.IsInfinity(z)) return 0.0;
            return Math.Clamp(z, -10.0, 10.0);
        }
        catch
        {
            return 0.0;
        }
    }

    public (double Fx, double Fy) ComputeGradient(double x, double y, double h = 1e-4)
    {
        double fx = (Evaluate(x + h, y) - Evaluate(x - h, y)) / (2.0 * h);
        double fy = (Evaluate(x, y + h) - Evaluate(x, y - h)) / (2.0 * h);
        return (fx, fy);
    }

    public (double Fxx, double Fyy, double Fxy, double HessianD) ComputeHessian(double x, double y, double h = 1e-3)
    {
        double fCenter = Evaluate(x, y);
        double fxx = (Evaluate(x + h, y) - 2.0 * fCenter + Evaluate(x - h, y)) / (h * h);
        double fyy = (Evaluate(x, y + h) - 2.0 * fCenter + Evaluate(x, y - h)) / (h * h);
        double fxy = (Evaluate(x + h, y + h) - Evaluate(x + h, y - h) - Evaluate(x - h, y + h) + Evaluate(x - h, y - h)) / (4.0 * h * h);

        double d = fxx * fyy - (fxy * fxy);
        return (fxx, fyy, fxy, d);
    }

    public string ClassifyPoint(double x, double y)
    {
        var (fxx, _, _, d) = ComputeHessian(x, y);
        var (fx, fy) = ComputeGradient(x, y);
        double gradNorm = Math.Sqrt(fx * fx + fy * fy);

        if (gradNorm > 0.1)
        {
            return $"Sloping (||∇f|| = {gradNorm:F2})";
        }

        if (d > 0.01)
        {
            return fxx > 0 ? "Local Minimum (D > 0, f_xx > 0)" : "Local Maximum (D > 0, f_xx < 0)";
        }
        if (d < -0.01)
        {
            return "Saddle Point (D < 0)";
        }
        return "Critical Inconclusive (D ≈ 0)";
    }
}
