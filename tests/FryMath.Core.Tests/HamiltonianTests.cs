using System;
using System.Collections.Generic;
using FryMath.Core.Mechanics;
using Xunit;

namespace FryMath.Core.Tests;

public class HamiltonianTests
{
    [Fact]
    public void HamiltonianEngine_HenonHeiles_SaddlePointsAndCriticalEnergy()
    {
        // Stable equilibrium at origin (0, 0)
        var (gx0, gy0) = HamiltonianEngine.HenonHeilesGradV(0, 0);
        Assert.Equal(0.0, gx0);
        Assert.Equal(0.0, gy0);
        Assert.Equal(0.0, HamiltonianEngine.EvaluateHenonHeilesH(0, 0, 0, 0));

        // Saddle point at (0, 1): escape energy Ec = 1/6 ≈ 0.166667
        var (gx1, gy1) = HamiltonianEngine.HenonHeilesGradV(0, 1);
        Assert.Equal(0.0, gx1);
        Assert.Equal(0.0, gy1);
        double hSaddle = HamiltonianEngine.EvaluateHenonHeilesH(0, 1, 0, 0);
        Assert.True(Math.Abs(hSaddle - (1.0 / 6.0)) < 1e-6, $"Expected Ec = 1/6, got {hSaddle}");

        // Triangular symmetry saddle points at (±sqrt(3)/2, -1/2)
        double sqrt3_2 = Math.Sqrt(3.0) / 2.0;
        var (gx2, gy2) = HamiltonianEngine.HenonHeilesGradV(sqrt3_2, -0.5);
        Assert.True(Math.Abs(gx2) < 1e-6);
        Assert.True(Math.Abs(gy2) < 1e-6);
        double hSaddle2 = HamiltonianEngine.EvaluateHenonHeilesH(sqrt3_2, -0.5, 0, 0);
        Assert.True(Math.Abs(hSaddle2 - (1.0 / 6.0)) < 1e-6);
    }

    [Fact]
    public void HamiltonianEngine_YoshidaSymplectic_BoundedEnergyOscillations_Vs_RK4Drift()
    {
        // Simulate Hénon-Heiles at bound energy E = 0.10 for 2000 steps with dt = 0.05
        double dt = 0.05;
        int steps = 2000;

        var stateSymplectic = new HamiltonianEngine.PhasePoint4D(0.0, 0.2, 0.35, 0.0);
        var stateRK4 = new HamiltonianEngine.PhasePoint4D(0.0, 0.2, 0.35, 0.0);

        double h0 = HamiltonianEngine.EvaluateHenonHeilesH(stateSymplectic.X, stateSymplectic.Y, stateSymplectic.Px, stateSymplectic.Py);

        double maxErrorYoshida = 0.0;
        for (int i = 0; i < steps; i++)
        {
            stateSymplectic = HamiltonianEngine.StepYoshida4D(stateSymplectic, dt, HamiltonianEngine.HenonHeilesGradV);
            double hCurr = HamiltonianEngine.EvaluateHenonHeilesH(stateSymplectic.X, stateSymplectic.Y, stateSymplectic.Px, stateSymplectic.Py);
            maxErrorYoshida = Math.Max(maxErrorYoshida, Math.Abs(hCurr - h0));
        }

        // Yoshida 4th-order energy error must remain strictly bounded without secular growth
        Assert.True(maxErrorYoshida < 1e-4, $"Yoshida max energy error should be < 1e-4, got {maxErrorYoshida}");
    }

    [Fact]
    public void HamiltonianEngine_LiouvillePhaseArea_PreservedUnderSymplecticFlow()
    {
        // Initial phase space circle in nonlinear pendulum (q, p)
        var initialLoop = HamiltonianEngine.GeneratePhaseLoop(0.0, 1.0, 0.3, count: 64);
        double area0 = HamiltonianEngine.ComputePhaseEnclosedArea(initialLoop);
        Assert.True(area0 > 0.25 && area0 < 0.35, $"Initial area should be ~pi*0.3² ≈ 0.283, got {area0}");

        // Evolve loop under pendulum Hamiltonian for 500 steps
        var currentLoop = initialLoop;
        double dt = 0.02;

        for (int i = 0; i < 500; i++)
        {
            currentLoop = HamiltonianEngine.StepPhaseLoop(currentLoop, dt, HamiltonianEngine.PendulumGradV, useSymplectic: true);
        }

        double areaT = HamiltonianEngine.ComputePhaseEnclosedArea(currentLoop);
        double areaRelError = Math.Abs(areaT - area0) / area0;

        // Liouville's theorem: area must be conserved within symplectic discretization tolerance (< 0.5%)
        Assert.True(areaRelError < 0.005, $"Liouville phase area relative error was {areaRelError:P3}, expected < 0.5%");
    }

    [Fact]
    public void HamiltonianEngine_DuffingPotential_EquilibriaAndSeparatrix()
    {
        // V(q) = -1/2 q² + 1/4 q⁴
        // dV/dq = -q + q³ = q(q² - 1) = 0 at q = 0 (unstable hill) and q = ±1 (stable wells)
        Assert.Equal(0.0, HamiltonianEngine.DuffingGradV(0.0));
        Assert.Equal(0.0, HamiltonianEngine.DuffingGradV(1.0));
        Assert.Equal(0.0, HamiltonianEngine.DuffingGradV(-1.0));

        // Potential at wells: V(±1) = -0.5 + 0.25 = -0.25
        Assert.Equal(-0.25, HamiltonianEngine.EvaluateDuffingH(1.0, 0.0));
        Assert.Equal(-0.25, HamiltonianEngine.EvaluateDuffingH(-1.0, 0.0));

        // Separatrix at origin: V(0) = 0.0
        Assert.Equal(0.0, HamiltonianEngine.EvaluateDuffingH(0.0, 0.0));
    }

    [Fact]
    public void HamiltonianEngine_PoissonBracket_CanonicalAxioms()
    {
        // Canonical bracket {q, p} = 1
        double pb_qp = HamiltonianEngine.EvaluatePoissonBracket2D(
            df_dq: 1.0, df_dp: 0.0, // f = q
            dg_dq: 0.0, dg_dp: 1.0  // g = p
        );
        Assert.Equal(1.0, pb_qp);

        // Anticommutativity {p, q} = -{q, p} = -1
        double pb_pq = HamiltonianEngine.EvaluatePoissonBracket2D(
            df_dq: 0.0, df_dp: 1.0, // f = p
            dg_dq: 1.0, dg_dp: 0.0  // g = q
        );
        Assert.Equal(-1.0, pb_pq);

        // Self-bracket {q, q} = 0
        double pb_qq = HamiltonianEngine.EvaluatePoissonBracket2D(
            df_dq: 1.0, df_dp: 0.0,
            dg_dq: 1.0, dg_dp: 0.0
        );
        Assert.Equal(0.0, pb_qq);
    }

    [Fact]
    public void HamiltonianEngine_PoincareCrossing_LinearInterpolation()
    {
        var sOld = new HamiltonianEngine.PhasePoint4D(-0.1, 0.5, 1.2, 0.3);
        var sNew = new HamiltonianEngine.PhasePoint4D(0.1, 0.7, 1.2, 0.4);

        var cross = HamiltonianEngine.DetectPoincareCrossing(sOld, sNew);
        Assert.NotNull(cross);

        // x crosses 0 at fraction = 0.5
        // y should be 0.5 + 0.5*(0.7 - 0.5) = 0.6
        Assert.True(Math.Abs(cross.Y - 0.6) < 1e-6);
        Assert.True(Math.Abs(cross.Py - 0.35) < 1e-6);
    }
}
