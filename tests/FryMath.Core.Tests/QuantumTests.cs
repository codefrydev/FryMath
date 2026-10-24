using System;
using FryMath.Core.Quantum;
using Xunit;

namespace FryMath.Core.Tests;

public class QuantumTests
{
    [Fact]
    public void QuantumEquation_CrankNicolson_ConservesProbabilityNorm()
    {
        var engine = new QuantumSchrodingerEngine(300, -15.0, 15.0);
        engine.LoadPreset(QuantumPreset.TunnelingBarrier);

        double initialNorm = engine.CalculateNorm();
        Assert.InRange(initialNorm, 0.999, 1.001);

        // Step forward 60 unitary time steps while wavepacket interacts with barrier
        for (int i = 0; i < 60; i++)
        {
            engine.Step();
        }

        double finalNorm = engine.CalculateNorm();
        // Crank-Nicolson Cayley transform must conserve total probability
        Assert.InRange(finalNorm, 0.999, 1.001);
    }

    [Fact]
    public void QuantumTunneling_PenetratesPotentialBarrier()
    {
        var engine = new QuantumSchrodingerEngine(300, -15.0, 15.0)
        {
            Dt = 0.04
        };
        engine.LoadPreset(QuantumPreset.TunnelingBarrier);

        // Initially at t=0, probability on the far right (x > 2.0) is effectively 0
        double initialTransmittedProb = 0.0;
        for (int i = 0; i < engine.Points; i++)
        {
            if (engine.GetX(i) > 2.0)
            {
                initialTransmittedProb += (engine.PsiR[i] * engine.PsiR[i] + engine.PsiI[i] * engine.PsiI[i]) * engine.Dx;
            }
        }
        Assert.True(initialTransmittedProb < 1e-4);

        // Propagate wavepacket into and across the barrier
        for (int step = 0; step < 75; step++)
        {
            engine.Step();
        }

        // Measure transmitted probability on the other side of barrier (x > 1.0)
        double transmittedProb = 0.0;
        for (int i = 0; i < engine.Points; i++)
        {
            if (engine.GetX(i) > 1.0)
            {
                transmittedProb += (engine.PsiR[i] * engine.PsiR[i] + engine.PsiI[i] * engine.PsiI[i]) * engine.Dx;
            }
        }

        // Quantum tunneling must allow non-zero transmission despite E < V0
        Assert.True(transmittedProb > 0.01, $"Expected non-zero quantum tunneling transmission, but got {transmittedProb}");
    }

    [Fact]
    public void HarmonicOscillator_CoherentState_OscillatesInWell()
    {
        var engine = new QuantumSchrodingerEngine(250, -10.0, 10.0)
        {
            Dt = 0.04
        };
        engine.LoadPreset(QuantumPreset.HarmonicOscillator);

        double initialX = engine.CalculateExpectedPosition();
        Assert.True(initialX < -2.0, $"Initial expected position should be near -3.5, got {initialX}");

        // Propagate half a harmonic cycle
        for (int step = 0; step < 80; step++)
        {
            engine.Step();
        }

        double laterX = engine.CalculateExpectedPosition();
        // The packet must have swung rightward into positive x
        Assert.True(laterX > -1.0, $"Coherent state should swing towards center/right, got {laterX}");
    }
}
