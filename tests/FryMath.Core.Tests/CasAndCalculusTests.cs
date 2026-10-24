using System;
using System.Collections.Generic;
using FryMath.Core.CAS;
using FryMath.Core.Calculus;
using Xunit;

namespace FryMath.Core.Tests;

public class CasAndCalculusTests
{
    [Fact]
    public void Parser_EvaluatesBasicArithmetic_Correctly()
    {
        var node = MathParser.Parse("2 + 3 * 4");
        Assert.Equal(14.0, node.Evaluate());
    }

    [Fact]
    public void Parser_HandlesImplicitMultiplication_Correctly()
    {
        var node = MathParser.Parse("2x + 3sin(x)");
        var vars = new Dictionary<string, double> { ["x"] = 0.0 };
        Assert.Equal(0.0, node.Evaluate(vars));

        vars["x"] = Math.PI * 0.5;
        // 2 * (pi/2) + 3 * sin(pi/2) = pi + 3
        Assert.Equal(Math.PI + 3.0, node.Evaluate(vars), 5);
    }

    [Fact]
    public void Differentiator_ComputesPolynomialDerivative_Correctly()
    {
        // f(x) = x^3 - 3x^2 + 5x - 7
        // f'(x) = 3x^2 - 6x + 5
        var node = MathParser.Parse("x^3 - 3*x^2 + 5*x - 7");
        var deriv = SymbolicDifferentiator.Differentiate(node, "x");

        var compiled = ExpressionCompiler.Compile1D(deriv, "x");

        // at x = 2: 3*(4) - 6*(2) + 5 = 12 - 12 + 5 = 5
        Assert.Equal(5.0, compiled(2.0), 3);
    }

    [Fact]
    public void Differentiator_ProductRule_And_ChainRule()
    {
        // f(x) = x * sin(x)
        // f'(x) = sin(x) + x * cos(x)
        var node = MathParser.Parse("x * sin(x)");
        var steps = new List<DerivationStep>();
        var deriv = SymbolicDifferentiator.Differentiate(node, "x", steps);

        Assert.NotEmpty(steps);

        var compiled = ExpressionCompiler.Compile1D(deriv, "x");
        double x = 1.0;
        double expected = Math.Sin(x) + x * Math.Cos(x);
        Assert.Equal(expected, compiled(x), 4);
    }

    [Fact]
    public void NumericalIntegration_ComputesDefiniteIntegral_Accurately()
    {
        // \int_0^\pi \sin(x) dx = 2.0
        Func<double, double> f = Math.Sin;
        double area = NumericalIntegration.IntegrateAdaptive(f, 0.0, Math.PI);
        Assert.Equal(2.0, area, 4);

        // Riemann sum should converge close to 2.0 with N=100
        var (partitions, riemannArea) = NumericalIntegration.ComputeRiemannSum(f, 0.0, Math.PI, 100, RiemannRule.Midpoint);
        Assert.Equal(100, partitions.Count);
        Assert.Equal(2.0, riemannArea, 2);
    }

    [Fact]
    public void FunctionAnalyzer_FindsRootsAndExtrema_OnCubic()
    {
        // f(x) = x^3 - 3x has:
        // roots at -sqrt(3), 0, sqrt(3)
        // local max at x = -1 (f(-1) = 2)
        // local min at x = 1 (f(1) = -2)
        Func<double, double> f = x => x * x * x - 3.0 * x;
        var crits = FunctionAnalyzer.FindCriticalPoints(f, -2.5, 2.5);

        Assert.Contains(crits, c => c.Kind == CriticalPointKind.LocalMaximum && Math.Abs(c.X - (-1.0)) < 0.05);
        Assert.Contains(crits, c => c.Kind == CriticalPointKind.LocalMinimum && Math.Abs(c.X - 1.0) < 0.05);
        Assert.Contains(crits, c => c.Kind == CriticalPointKind.Root && Math.Abs(c.X) < 0.05);
    }

    [Fact]
    public void TaylorSeries_ApproximatesExponential_Correctly()
    {
        // e^x around 0 up to degree 4: 1 + x + x^2/2 + x^3/6 + x^4/24
        var node = MathParser.Parse("exp(x)");
        var (poly, formula) = TaylorSeries.GenerateTaylorSeries(node, 0.0, 5);

        double x = 0.5;
        double exact = Math.Exp(x);
        double approx = poly(x);

        Assert.Equal(exact, approx, 3);
        Assert.NotEmpty(formula);
    }

    [Fact]
    public void FunctionAnalyzer_FindIntersections_FindsExactCrossings()
    {
        // f(x) = x^2, g(x) = x + 2
        // Intersections: x^2 - x - 2 = 0 -> (x - 2)(x + 1) = 0 -> x = -1, 2
        Func<double, double> f = x => x * x;
        Func<double, double> g = x => x + 2.0;

        var points = FunctionAnalyzer.FindIntersections(f, g, -3.0, 3.0);
        Assert.Equal(2, points.Count);
        Assert.Contains(points, p => Math.Abs(p.X - (-1.0)) < 0.05 && Math.Abs(p.Y - 1.0) < 0.05);
        Assert.Contains(points, p => Math.Abs(p.X - 2.0) < 0.05 && Math.Abs(p.Y - 4.0) < 0.05);
    }

    [Fact]
    public void FunctionAnalyzer_TangentAndNormalLines_CalculatesOrthogonalSlopes()
    {
        // f(x) = x^2 at x = 2
        // f'(2) = 4
        // Tangent: y = 4x - 4 (passes through (2, 4))
        // Normal: slope = -1/4 = -0.25
        Func<double, double> f = x => x * x;
        var (m, c) = FunctionAnalyzer.TangentLineEquation(f, 2.0);
        Assert.Equal(4.0, m, 2);
        Assert.Equal(-4.0, c, 2);

        var normal = FunctionAnalyzer.NormalLineEquation(f, 2.0);
        Assert.NotNull(normal);
        Assert.Equal(-0.25, normal.Value.Slope, 2);
        // Product of perpendicular slopes = -1
        Assert.Equal(-1.0, m * normal.Value.Slope, 3);
    }

    [Fact]
    public void FunctionAnalyzer_ArcLength_And_VolumeOfRevolution()
    {
        // Line y = x from x=0 to x=3 has arc length = sqrt(3^2 + 3^2) = 3*sqrt(2) approx 4.2426
        Func<double, double> line = x => x;
        double len = FunctionAnalyzer.ArcLength(line, 0.0, 3.0);
        Assert.Equal(3.0 * Math.Sqrt(2.0), len, 2);

        // Cylinder radius r=2, height h=3 has volume = pi * r^2 * h = 12 * pi approx 37.699
        Func<double, double> flat = _ => 2.0;
        double vol = FunctionAnalyzer.VolumeOfRevolution(flat, 0.0, 3.0);
        Assert.Equal(12.0 * Math.PI, vol, 2);
    }

    [Fact]
    public void NumericalIntegration_IntegrateRiemann_ConvergesToSimpson()
    {
        // \int_0^1 x^2 dx = 1/3 approx 0.3333
        Func<double, double> f = x => x * x;
        double exact = 1.0 / 3.0;

        double adaptive = NumericalIntegration.IntegrateAdaptive(f, 0.0, 1.0);
        Assert.Equal(exact, adaptive, 4);

        double riemannMid = NumericalIntegration.IntegrateRiemann(f, 0.0, 1.0, 100, RiemannRule.Midpoint);
        Assert.Equal(exact, riemannMid, 3);

        double riemannTrap = NumericalIntegration.IntegrateRiemann(f, 0.0, 1.0, 100, RiemannRule.Trapezoid);
        Assert.Equal(exact, riemannTrap, 3);
    }
}
