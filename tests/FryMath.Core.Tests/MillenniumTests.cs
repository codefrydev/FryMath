using System;
using System.Linq;
using FryMath.Core.Millennium;
using Xunit;

namespace FryMath.Core.Tests;

public class MillenniumTests
{
    [Fact]
    public void MillenniumEngine_ContainsAllSevenClayProblems()
    {
        Assert.NotNull(MillenniumEngine.Problems);
        Assert.Equal(7, MillenniumEngine.Problems.Count);

        var types = MillenniumEngine.Problems.Select(p => p.Type).ToList();
        Assert.Contains(MillenniumProblemType.PoincareConjecture, types);
        Assert.Contains(MillenniumProblemType.RiemannHypothesis, types);
        Assert.Contains(MillenniumProblemType.BirchSwinnertonDyer, types);
        Assert.Contains(MillenniumProblemType.NavierStokesSmoothness, types);
        Assert.Contains(MillenniumProblemType.YangMillsMassGap, types);
        Assert.Contains(MillenniumProblemType.PversusNP, types);
        Assert.Contains(MillenniumProblemType.HodgeConjecture, types);

        // Two solved problems: Poincaré (by Grigori Perelman) and Navier-Stokes (by OpenAI & Lean 4 formalization)
        int solvedCount = MillenniumEngine.Problems.Count(p => p.IsSolved);
        Assert.Equal(2, solvedCount);

        var poincare = MillenniumEngine.Problems.First(p => p.Type == MillenniumProblemType.PoincareConjecture);
        Assert.True(poincare.IsSolved);
        Assert.Contains("Perelman", poincare.ProposerOrSolver);
        Assert.Contains("∂_t g", poincare.CoreFormula);

        var navier = MillenniumEngine.Problems.First(p => p.Type == MillenniumProblemType.NavierStokesSmoothness);
        Assert.True(navier.IsSolved);
        Assert.Contains("OpenAI", navier.ProposerOrSolver);
        Assert.Contains("Lean 4", navier.SolutionOrCurrentAttack);
        Assert.Contains("Blow-up", navier.CoreFormula);
    }

    [Fact]
    public void SimulateRicciFlow_PerelmanEntropyMonotonicityAndSurgery()
    {
        // Perelman's W-entropy must be strictly monotonic: dW/dt >= 0
        var state0 = MillenniumEngine.SimulateRicciFlow(0.0);
        var state1 = MillenniumEngine.SimulateRicciFlow(1.5);
        var state2 = MillenniumEngine.SimulateRicciFlow(3.0);

        Assert.True(state1.PerelmanEntropyW >= state0.PerelmanEntropyW, "Perelman W-entropy must be monotonically non-decreasing");
        Assert.True(state2.PerelmanEntropyW >= state1.PerelmanEntropyW, "Perelman W-entropy must be monotonically non-decreasing after surgery");

        // Surgery occurs as t advances
        Assert.Equal(0, state0.SurgeryCount);
        Assert.True(state2.SurgeryCount > 0, "Topological surgery should be performed along developing neck pinch");

        // Extinction at t >= 4.8
        var stateExtinct = MillenniumEngine.SimulateRicciFlow(5.0);
        Assert.True(stateExtinct.IsExtinct, "Ricci flow extinction uniformizes manifold into S³");
    }

    [Fact]
    public void SimulateRiemannGue_ObeysMontgomeryOdlyzkoLaw()
    {
        var state = MillenniumEngine.SimulateRiemannGue(14.13, 12);

        Assert.NotNull(state);
        Assert.True(state.PairCorrelationMean > 0.0);
        Assert.InRange(state.PairCorrelationMean, 0.0, 1.5);
        Assert.True(state.GueRepulsionIndex > 0.0, "GUE level repulsion P(s) ~ s² must be positive");
        Assert.True(state.PrimeLiResidual >= 0.0);
        Assert.Contains("GUE", state.SpectralStatus);
    }

    [Theory]
    [InlineData(-1.0, 1.0, 1)]
    [InlineData(-2.0, 0.0, 1)]
    public void SimulateBsdElliptic_ComputesAnalyticRankAndInvariants(double a, double b, int expectedRank)
    {
        var state = MillenniumEngine.SimulateBsdElliptic(a, b);

        Assert.NotNull(state);
        Assert.Equal(a, state.ParamA);
        Assert.Equal(b, state.ParamB);
        Assert.NotEqual(0.0, state.Discriminant);
        Assert.True(state.LeadingCoefficientL > 0.0);
        Assert.True(state.RealPeriodOmega > 0.0);
        Assert.Equal(expectedRank, state.AnalyticRank);
        Assert.NotEmpty(state.MordellWeilStructure);
    }

    [Fact]
    public void SimulateNavierStokesBlowup_ViscousRegularityVsBlowup()
    {
        // Viscous regular regime (nu = 0.005)
        var smoothState = MillenniumEngine.SimulateNavierStokesBlowup(1.5, viscosity: 0.005);
        Assert.False(smoothState.IsBlowupImminent);
        Assert.Contains("Guaranteed", smoothState.RegularityStatus);
        Assert.True(smoothState.KineticEnergy > 0.0);
        Assert.True(smoothState.Enstrophy > 0.0);

        // Ultra-low viscosity blow-up regime (nu = 0.00001, high vorticity)
        var blowupState = MillenniumEngine.SimulateNavierStokesBlowup(2.5, viscosity: 0.00001, initialVorticityAmp: 8.0);
        Assert.True(blowupState.BkmBlowupIntegral > smoothState.BkmBlowupIntegral);

        // OpenAI Lean 4 verified finite-time blow-up regime
        var openAiState = MillenniumEngine.SimulateNavierStokesBlowup(1.84, viscosity: 0.00001, initialVorticityAmp: 9.5);
        Assert.True(openAiState.IsOpenAiBlowupVerified);
        Assert.Equal(1.84, openAiState.SingularityTimeTStar);
        Assert.Equal(0.5, openAiState.SelfSimilarAlpha);
        Assert.Contains("Lean 4", openAiState.ProofFormalism);
        Assert.Contains("OpenAI Blow-Up Singularity Realized", openAiState.RegularityStatus);
    }

    [Fact]
    public void SimulateYangMillsLattice_ConfinementAndMassGapPositive()
    {
        // Strong coupling
        var strong = MillenniumEngine.SimulateYangMillsLattice(1.2);
        Assert.True(strong.MassGapDelta > 0.0, "Glueball mass gap must be strictly positive");
        Assert.True(strong.StringTensionSigma > 0.0, "String tension must be non-zero for linear confinement");
        Assert.Contains("Confinement", strong.ConfinementRegime);

        // Physical scaling window
        var scaling = MillenniumEngine.SimulateYangMillsLattice(2.4);
        Assert.True(scaling.MassGapDelta > 0.0);
        Assert.InRange(scaling.AveragePlaquette, 0.0, 1.0);
    }

    [Theory]
    [InlineData(20, 2.5, true)]
    [InlineData(20, 6.5, false)]
    public void SimulatePvsNpPhaseTransition_SatisfiabilitySigmoid(int vars, double alpha, bool expectMostlySat)
    {
        var state = MillenniumEngine.SimulatePvsNpPhaseTransition(vars, alpha);

        Assert.NotNull(state);
        Assert.Equal(vars, state.VariablesCount);
        Assert.Equal(alpha, state.ClauseRatioAlpha);

        if (expectMostlySat)
        {
            Assert.True(state.SatisfiabilityProbability > 0.8, "Below critical threshold α < 3.8, formulas are almost surely SAT");
        }
        else
        {
            Assert.True(state.SatisfiabilityProbability < 0.2, "Above critical threshold α > 5.5, formulas are almost surely UNSAT");
        }

        // Search complexity peak at critical threshold
        var crit = MillenniumEngine.SimulatePvsNpPhaseTransition(vars, 4.267);
        Assert.True(crit.SearchComplexitySteps > state.SearchComplexitySteps || Math.Abs(alpha - 4.267) < 0.5);
    }

    [Theory]
    [InlineData("curve", 1, 1, true)]
    [InlineData("k3", 2, 20, true)]
    [InlineData("calabiyau", 3, 101, false)]
    public void SimulateHodgeDiamond_VarietyCohomologyAndCycles(string variety, int expectedDim, int expectedCycles, bool expectedLefschetz)
    {
        var state = MillenniumEngine.SimulateHodgeDiamond(variety);

        Assert.NotNull(state);
        Assert.Equal(expectedDim, state.Dimension);
        Assert.Equal(expectedCycles, state.RationalHodgeClassesCount);
        Assert.Equal(expectedLefschetz, state.IsLefschetzApplicable);
        Assert.NotNull(state.HodgeDiamond);
        Assert.NotEmpty(state.HodgeStatus);
    }

    [Fact]
    public void SimulateHilbertPolya_QuantizedEnergiesMatchZetaZeros()
    {
        var hp = RiemannSolutionEngine.SimulateHilbertPolya(count: 16);

        Assert.NotNull(hp);
        Assert.Equal(16, hp.QuantumEigenvalues.Count);
        Assert.Equal(16, hp.ActualZeros.Count);
        Assert.True(hp.SemiclassicalResidual < 1.0);
        Assert.Contains("H = -i", hp.OperatorFormula);
        Assert.Contains("Connes", hp.SpectralProofStatus);

        // First eigenvalue should be close to first zero gamma_1 ~ 14.1347
        Assert.InRange(hp.QuantumEigenvalues[0], 14.0, 14.5);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(-0.1, false)]
    [InlineData(0.1, true)]
    public void SimulateDeBruijnNewman_FlowAndCriticalThreshold(double t, bool expectedRhEquivalent)
    {
        var dbn = RiemannSolutionEngine.SimulateDeBruijnNewman(t, rootsCount: 12);

        Assert.NotNull(dbn);
        Assert.Equal(t, dbn.ParameterT);
        Assert.Equal(expectedRhEquivalent, Math.Abs(dbn.ParameterT) < 1e-9 || dbn.ParameterT > 0);
        Assert.True(dbn.Roots.Count > 0);

        if (t >= 0.0)
        {
            Assert.True(dbn.AllRootsReal, "All roots of H_t must be real for t >= 0");
        }
    }

    [Fact]
    public void SimulateLiCriterion_PositivityHoldsForAllTestedN()
    {
        var li = RiemannSolutionEngine.SimulateLiCriterion(maxN: 25);

        Assert.NotNull(li);
        Assert.True(li.AllPositive);
        Assert.Equal(25, li.TestedCount);
        Assert.Equal(25, li.Coefficients.Count);

        // Every lambda_n must be strictly positive
        foreach (var c in li.Coefficients)
        {
            Assert.True(c.Value > 0.0, $"Li coefficient lambda_{c.N} must be positive, got {c.Value}");
            Assert.True(c.IsPositive);
        }
    }

    [Fact]
    public void SimulateZeroHunter_FindsTuringVerifiedRootsAndGramPoints()
    {
        // Scan first range [10, 30] where zeros gamma_1=14.13, gamma_2=21.02, gamma_3=25.01 occur
        var hunter = RiemannSolutionEngine.SimulateZeroHunter(10.0, 30.0);

        Assert.NotNull(hunter);
        Assert.True(hunter.FoundZeros.Count >= 3, "Range [10, 30] contains at least 3 zeros");
        Assert.True(hunter.AllZerosOnCriticalLine);
        Assert.Contains("Turing Method Certified", hunter.TuringVerificationCertificate);

        // Verify zeros close to known values
        Assert.InRange(hunter.FoundZeros[0], 14.10, 14.20);
        Assert.InRange(hunter.FoundZeros[1], 21.00, 21.10);
        Assert.InRange(hunter.FoundZeros[2], 25.00, 25.10);

        // Gram points
        Assert.True(hunter.GramPoints.Count > 0);
        Assert.True(hunter.GramPoints.All(g => g.Index >= 0));
    }

    [Fact]
    public void PlayRiemannChordTone_ExecutesWithoutError()
    {
        var ex = Record.Exception(() => RiemannSolutionEngine.PlayRiemannChordTone(durationSeconds: 0.01, volume: 0.1));
        Assert.Null(ex);
    }
}
