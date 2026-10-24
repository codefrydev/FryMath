using System;
using System.Linq;
using FryMath.Core.Catastrophe;
using Xunit;

namespace FryMath.Core.Tests;

public class CatastropheTests
{
    private readonly CatastropheEngine _engine = new();

    [Fact]
    public void CuspPotentialAndDerivatives_MatchAnalyticalFormulas()
    {
        double x = 2.0;
        double a = -3.0;
        double b = 1.0;

        // V(2) = 2⁴/4 + (-3)(2²)/2 + 1(2) = 4 - 6 + 2 = 0
        double v = _engine.ComputeCuspPotential(x, a, b);
        Assert.Equal(0.0, v, precision: 5);

        // V'(2) = 2³ + (-3)(2) + 1 = 8 - 6 + 1 = 3
        double vp = _engine.ComputeCuspDerivative(x, a, b);
        Assert.Equal(3.0, vp, precision: 5);

        // V''(2) = 3(2²) + (-3) = 12 - 3 = 9
        double vpp = _engine.ComputeCuspCurvature(x, a);
        Assert.Equal(9.0, vpp, precision: 5);
    }

    [Fact]
    public void CuspEquilibria_BistableThreeRoots_ClassifiesStabilityCorrectly()
    {
        // a = -3, b = 0 => x³ - 3x = 0 => x = -√3, 0, √3
        var eq = _engine.SolveCuspEquilibria(-3.0, 0.0);
        Assert.Equal(3, eq.Count);

        double sqrt3 = Math.Sqrt(3.0);
        Assert.Equal(-sqrt3, eq[0].X, precision: 4);
        Assert.True(eq[0].IsStable); // V''(-√3) = 3(3) - 3 = 6 > 0

        Assert.Equal(0.0, eq[1].X, precision: 4);
        Assert.False(eq[1].IsStable); // V''(0) = -3 < 0

        Assert.Equal(sqrt3, eq[2].X, precision: 4);
        Assert.True(eq[2].IsStable); // V''(√3) = 6 > 0
    }

    [Fact]
    public void CuspEquilibria_MonostableRegion_ReturnsSingleStableRoot()
    {
        // a = 2, b = 0 => x³ + 2x = 0 => x(x²+2) = 0 => only x = 0
        var eq = _engine.SolveCuspEquilibria(2.0, 0.0);
        Assert.Single(eq);
        Assert.Equal(0.0, eq[0].X, precision: 4);
        Assert.True(eq[0].IsStable); // V''(0) = 2 > 0
    }

    [Fact]
    public void CuspBifurcationDiscriminant_IdentifiesCuspBoundaries()
    {
        // At a = -3, bifurcation occurs when 4(-3)³ + 27b² = 0 => -108 + 27b² = 0 => b = ±2
        double discPlus2 = _engine.ComputeCuspDiscriminant(-3.0, 2.0);
        Assert.Equal(0.0, discPlus2, precision: 5);

        double discMinus2 = _engine.ComputeCuspDiscriminant(-3.0, -2.0);
        Assert.Equal(0.0, discMinus2, precision: 5);

        // Inside cusp: b = 0 => 4(-27) = -108 < 0
        double inside = _engine.ComputeCuspDiscriminant(-3.0, 0.0);
        Assert.True(inside < 0);

        // Outside cusp: b = 3 => -108 + 27(9) = -108 + 243 = +135 > 0
        double outside = _engine.ComputeCuspDiscriminant(-3.0, 3.0);
        Assert.True(outside > 0);
    }

    [Fact]
    public void CuspBifurcationCurve_GeneratesParabolicBranches()
    {
        var curve = _engine.ComputeBifurcationCurve(aMin: -4.0, aMax: 0.0, steps: 20);
        Assert.NotEmpty(curve);

        // Origin cusp point at a = 0
        var origin = curve.Last();
        Assert.Equal(0.0, origin.A, precision: 4);
        Assert.Equal(0.0, origin.BPlus, precision: 4);
        Assert.Equal(0.0, origin.BMinus, precision: 4);
    }

    [Fact]
    public void RelaxState_UnderHysteresis_CatastrophicJumpOccurs()
    {
        // Start on upper stable branch at a = -3, b = -1 => x ≈ +1.879
        double xInit = 1.88;
        double a = -3.0;

        // When b is pushed past the fold threshold b = 2.0 (e.g. b = 2.2),
        // the positive minimum vanishes and state relaxes to negative root (x ≈ -2.08)
        double xRelaxed = _engine.RelaxState(xInit, a, b: 2.2, dt: 0.05, iterations: 150);
        Assert.True(xRelaxed < 0.0, $"Expected jump to lower sheet (x < 0), got {xRelaxed}");
    }

    [Fact]
    public void FoldCatastrophe_EvaluatesAnnihilationOfSaddleNode()
    {
        // V(x) = x³/3 + ax. For a = -1, V' = x² - 1 = 0 => x = ±1.
        var (_, vp1, vpp1) = _engine.EvaluateFold(1.0, -1.0);
        Assert.Equal(0.0, vp1, precision: 5);
        Assert.True(vpp1 > 0); // stable node

        var (_, vp2, vpp2) = _engine.EvaluateFold(-1.0, -1.0);
        Assert.Equal(0.0, vp2, precision: 5);
        Assert.True(vpp2 < 0); // unstable saddle
    }

    [Fact]
    public void ButterflyCatastrophe_EvaluatesPotentialAndDerivatives()
    {
        var (v, vp, vpp) = _engine.EvaluateButterfly(x: 1.0, a: -4.0, b: 0.0, c: 3.0, d: 0.0);
        // V(1) = 1/6 - 4/4 + 0 + 3/2 + 0 = 0.1667 - 1 + 1.5 = 0.6667
        Assert.Equal(2.0 / 3.0, v, precision: 4);
        // V'(1) = 1 - 4 + 0 + 3 + 0 = 0
        Assert.Equal(0.0, vp, precision: 5);
        // V''(1) = 5 - 12 + 0 + 3 = -4
        Assert.Equal(-4.0, vpp, precision: 5);
    }

    [Fact]
    public void ZeemanCatastropheMachine_ComputesNonNegativeEnergy()
    {
        double energy = _engine.ComputeZeemanPotential(theta: Math.PI / 4.0, a: 1.5, b: 0.5);
        Assert.True(energy >= 0.0);
    }
}
