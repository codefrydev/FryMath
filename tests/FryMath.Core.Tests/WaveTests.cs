using System;
using FryMath.Core.Waves;
using Xunit;

namespace FryMath.Core.Tests;

public class WaveTests
{
    [Fact]
    public void ChladniMode_OnDiagonal_HasZeroNodalLine()
    {
        var engine = new Wave2DEngine();
        engine.ModeN = 2;
        engine.ModeM = 4;

        // When x == y, cos(n pi x)cos(m pi y) - cos(m pi x)cos(n pi y) == 0 identically!
        double valDiagonal1 = engine.EvaluateChladni(0.3, 0.3);
        double valDiagonal2 = engine.EvaluateChladni(-0.6, -0.6);

        Assert.True(Math.Abs(valDiagonal1) < 1e-10);
        Assert.True(Math.Abs(valDiagonal2) < 1e-10);
    }

    [Fact]
    public void FiniteDifference_DropImpulse_PropagatesDisturbance()
    {
        var engine = new Wave2DEngine();
        engine.Mode = WaveSimulationMode.InteractiveRippleTank;

        // Drop in center (40, 40)
        engine.DropImpulse(40, 40, amplitude: 5.0, radius: 4);

        // Center should have initial displacement
        double uCenterInitial = engine.GetDisplacement(0.0, 0.0);
        Assert.True(Math.Abs(uCenterInitial) > 1.0);

        // Step simulation forward
        for (int i = 0; i < 20; i++)
        {
            engine.StepSimulation(0.016);
        }

        // Wave should propagate outward away from center
        double uCenterLater = engine.GetDisplacement(0.0, 0.0);
        // Disturbance should have evolved
        Assert.NotEqual(uCenterInitial, uCenterLater);
    }

    [Fact]
    public void ChladniSandParticles_RemainBounded()
    {
        var engine = new Wave2DEngine(particleCount: 100);
        engine.Mode = WaveSimulationMode.ChladniPlate;

        for (int step = 0; step < 50; step++)
        {
            engine.StepSimulation(0.016);
        }

        foreach (var p in engine.SandParticles)
        {
            Assert.True(p.X >= -1.0 && p.X <= 1.0);
            Assert.True(p.Y >= -1.0 && p.Y <= 1.0);
        }
    }
}
