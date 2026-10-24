using System;
using System.Collections.Generic;
using FryMath.Core.CAS;
using FryMath.Core.Calculus;
using Xunit;

namespace FryMath.Core.Tests;

public class SymbolicIntegratorTests
{
    [Fact]
    public void Integrator_IntegratesConstant_Correctly()
    {
        var node = MathParser.Parse("5");
        var anti = SymbolicIntegrator.Integrate(node, "x");
        var compiled = ExpressionCompiler.Compile1D(anti, "x");

        // F(x) = 5x -> F(3) = 15
        Assert.Equal(15.0, compiled(3.0), 4);
    }

    [Fact]
    public void Integrator_IntegratesLinearVariable_Correctly()
    {
        var node = MathParser.Parse("x");
        var anti = SymbolicIntegrator.Integrate(node, "x");
        var compiled = ExpressionCompiler.Compile1D(anti, "x");

        // F(x) = 0.5 * x^2 -> F(4) = 8
        Assert.Equal(8.0, compiled(4.0), 4);
    }

    [Fact]
    public void Integrator_IntegratesPolynomial_Correctly()
    {
        // f(x) = 3*x^2 + 4*x - 5
        // F(x) = x^3 + 2*x^2 - 5*x
        var node = MathParser.Parse("3*x^2 + 4*x - 5");
        var steps = new List<DerivationStep>();
        var anti = SymbolicIntegrator.Integrate(node, "x", steps);

        Assert.NotEmpty(steps);

        var compiled = ExpressionCompiler.Compile1D(anti, "x");
        // at x = 2: 2^3 + 2*(4) - 5*(2) = 8 + 8 - 10 = 6
        Assert.Equal(6.0, compiled(2.0), 3);
    }

    [Fact]
    public void Integrator_IntegratesTrigFunctions_Correctly()
    {
        var sinNode = MathParser.Parse("sin(x)");
        var sinAnti = SymbolicIntegrator.Integrate(sinNode, "x");
        var compiledSin = ExpressionCompiler.Compile1D(sinAnti, "x");

        // ∫ sin(x) dx = -cos(x). at x = 0: -cos(0) = -1.0
        Assert.Equal(-1.0, compiledSin(0.0), 4);

        var cosNode = MathParser.Parse("cos(x)");
        var cosAnti = SymbolicIntegrator.Integrate(cosNode, "x");
        var compiledCos = ExpressionCompiler.Compile1D(cosAnti, "x");

        // ∫ cos(x) dx = sin(x). at x = pi/2: sin(pi/2) = 1.0
        Assert.Equal(1.0, compiledCos(Math.PI * 0.5), 4);
    }

    [Fact]
    public void Integrator_IntegratesExponential_Correctly()
    {
        var node = MathParser.Parse("exp(x)");
        var anti = SymbolicIntegrator.Integrate(node, "x");
        var compiled = ExpressionCompiler.Compile1D(anti, "x");

        // ∫ exp(x) dx = exp(x). at x = 2: exp(2)
        Assert.Equal(Math.Exp(2.0), compiled(2.0), 4);
    }

    [Fact]
    public void Integrator_IntegratesRationalInverse_Correctly()
    {
        var node = MathParser.Parse("1 / x");
        var anti = SymbolicIntegrator.Integrate(node, "x");
        var compiled = ExpressionCompiler.Compile1D(anti, "x");

        // ∫ 1/x dx = ln(|x|). at x = Math.E: ln(e) = 1.0
        Assert.Equal(1.0, compiled(Math.E), 4);
    }

    [Fact]
    public void Integrator_FundamentalTheoremOfCalculus_DerivativeOfIntegralMatchesOriginal()
    {
        // For f(x) = 2*x^3 - 5*x + 3
        // d/dx [ ∫ f(x) dx ] == f(x)
        var origNode = MathParser.Parse("2*x^3 - 5*x + 3");
        var antiNode = SymbolicIntegrator.Integrate(origNode, "x");
        var derivOfAnti = SymbolicDifferentiator.Differentiate(antiNode, "x");

        var origFunc = ExpressionCompiler.Compile1D(origNode, "x");
        var roundTripFunc = ExpressionCompiler.Compile1D(derivOfAnti, "x");

        foreach (double testX in new[] { -2.5, 0.0, 1.3, 3.7 })
        {
            double expected = origFunc(testX);
            double actual = roundTripFunc(testX);
            Assert.Equal(expected, actual, 3);
        }
    }
}
