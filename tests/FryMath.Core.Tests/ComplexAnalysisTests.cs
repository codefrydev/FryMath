using System;
using System.Numerics;
using FryMath.Core.ComplexAnalysis;
using Xunit;

namespace FryMath.Core.Tests;

public class ComplexAnalysisTests
{
    [Fact]
    public void ComplexExpression_ZSquared_DoublesArgumentAndSquaresModulus()
    {
        var expr = new ComplexExpression(ComplexPreset.ZSquared);
        var z = new Complex(1.0, 1.0); // magnitude sqrt(2), phase pi/4

        var w = expr.Evaluate(z); // should be (1+i)^2 = 1 + 2i - 1 = 2i

        Assert.True(Math.Abs(w.Real) < 1e-6, $"Real part expected 0, got {w.Real}");
        Assert.True(Math.Abs(w.Imaginary - 2.0) < 1e-6, $"Imaginary part expected 2, got {w.Imaginary}");
        Assert.True(Math.Abs(w.Magnitude - 2.0) < 1e-6, $"Modulus expected 2, got {w.Magnitude}");
    }

    [Fact]
    public void ComplexExpression_CubicRootsOfUnity_EvaluatesZeroAtAllThreeRoots()
    {
        var expr = new ComplexExpression(ComplexPreset.CubicRootsOfUnity);

        var r0 = new Complex(1.0, 0.0);
        var r1 = new Complex(-0.5, Math.Sqrt(3.0) / 2.0);
        var r2 = new Complex(-0.5, -Math.Sqrt(3.0) / 2.0);

        var w0 = expr.Evaluate(r0);
        var w1 = expr.Evaluate(r1);
        var w2 = expr.Evaluate(r2);

        Assert.True(w0.Magnitude < 1e-6, $"Root 0 magnitude {w0.Magnitude}");
        Assert.True(w1.Magnitude < 1e-5, $"Root 1 magnitude {w1.Magnitude}");
        Assert.True(w2.Magnitude < 1e-5, $"Root 2 magnitude {w2.Magnitude}");
    }

    [Fact]
    public void ComplexExpression_Exponential_MatchesEulersFormula()
    {
        var expr = new ComplexExpression(ComplexPreset.Exponential);

        // e^(i * pi) = -1
        var wPi = expr.Evaluate(new Complex(0, Math.PI));
        Assert.True(Math.Abs(wPi.Real - (-1.0)) < 1e-5, $"e^(i*pi) real expected -1, got {wPi.Real}");
        Assert.True(Math.Abs(wPi.Imaginary) < 1e-5, $"e^(i*pi) imag expected 0, got {wPi.Imaginary}");

        // e^(i * pi/2) = i
        var wHalfPi = expr.Evaluate(new Complex(0, Math.PI * 0.5));
        Assert.True(Math.Abs(wHalfPi.Real) < 1e-5, $"e^(i*pi/2) real expected 0, got {wHalfPi.Real}");
        Assert.True(Math.Abs(wHalfPi.Imaginary - 1.0) < 1e-5, $"e^(i*pi/2) imag expected 1, got {wHalfPi.Imaginary}");
    }

    [Fact]
    public void ComplexExpression_SimplePole_InvertsPointAndReversesAngle()
    {
        var expr = new ComplexExpression(ComplexPreset.SimplePole);

        // 1 / (2i) = -0.5i
        var w = expr.Evaluate(new Complex(0, 2.0));
        Assert.True(Math.Abs(w.Real) < 1e-6);
        Assert.True(Math.Abs(w.Imaginary - (-0.5)) < 1e-6);
    }

    [Fact]
    public void ComplexExpression_Derivative_CalculatesConformalScaleFactor()
    {
        var expr = new ComplexExpression(ComplexPreset.ZSquared);

        // For f(z) = z^2, f'(z) = 2z. At z = (1, 1), f'(z) = 2 + 2i, magnitude = sqrt(8) ~ 2.8284
        var z = new Complex(1.0, 1.0);
        var deriv = expr.Derivative(z);
        double expectedModulus = Math.Sqrt(8.0);

        Assert.True(Math.Abs(deriv.Magnitude - expectedModulus) < 1e-3,
            $"Expected derivative magnitude {expectedModulus}, got {deriv.Magnitude}");
    }

    [Fact]
    public void ComplexCompiler_ParsesAndEvaluatesArbitraryFormula()
    {
        var expr = new ComplexExpression();
        expr.CompileCustomFormula("z^4 - 1");

        // The four 4th roots of unity: 1, -1, i, -i
        var r1 = expr.Evaluate(new Complex(1, 0));
        var r2 = expr.Evaluate(new Complex(-1, 0));
        var r3 = expr.Evaluate(new Complex(0, 1));
        var r4 = expr.Evaluate(new Complex(0, -1));

        Assert.True(r1.Magnitude < 1e-6);
        Assert.True(r2.Magnitude < 1e-6);
        Assert.True(r3.Magnitude < 1e-6);
        Assert.True(r4.Magnitude < 1e-6);
    }

    [Fact]
    public void ComplexCompiler_EvaluatesTrigAndExpCombinations()
    {
        var expr = new ComplexExpression();
        expr.CompileCustomFormula("sin(z) + exp(z)");

        // At z = 0: sin(0) + exp(0) = 0 + 1 = 1
        var w = expr.Evaluate(Complex.Zero);
        Assert.True(Math.Abs(w.Real - 1.0) < 1e-6);
        Assert.True(Math.Abs(w.Imaginary) < 1e-6);
    }
}
