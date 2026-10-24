using System;
using FryMath.Core.Celestial;
using Xunit;

namespace FryMath.Core.Tests;

public class CelestialNBodyTests
{
    [Fact]
    public void FigureEight_ConservesTotalEnergy_UnderSymplecticLeapfrog()
    {
        var engine = new CelestialNBodyEngine();
        engine.LoadPreset(CelestialPreset.FigureEight);

        double e0 = engine.TotalEnergy;
        Assert.True(e0 < 0, "Figure-8 gravitational energy should be bound (negative)");

        // Step 500 times using symplectic leapfrog
        for (int i = 0; i < 500; i++)
        {
            engine.Step(0.004);
        }

        double eEnd = engine.TotalEnergy;
        double drift = Math.Abs((eEnd - e0) / e0);

        Assert.True(drift < 0.002, $"Symplectic leapfrog must conserve energy without secular drift! Drift was {drift:E3}");
    }

    [Fact]
    public void FigureEight_PreservesZeroTotalLinearMomentum()
    {
        var engine = new CelestialNBodyEngine();
        engine.LoadPreset(CelestialPreset.FigureEight);

        // Center of mass should remain near origin
        for (int i = 0; i < 200; i++)
        {
            engine.Step(0.004);
        }

        var cm = engine.CenterOfMass;
        double rCM = Math.Sqrt(cm.X * cm.X + cm.Y * cm.Y + cm.Z * cm.Z);
        Assert.True(rCM < 1e-4, $"Center of mass should remain stationary at origin! Was {rCM:E3}");
    }

    [Fact]
    public void LagrangeTrojans_InitializesPrimarySecondaryAndSwarms()
    {
        var engine = new CelestialNBodyEngine();
        engine.LoadPreset(CelestialPreset.LagrangeTrojans);

        // Sun + Jupiter + 20 L4 + 20 L5 = 42 bodies
        Assert.Equal(42, engine.Bodies.Count);
        Assert.Equal("Central Star", engine.Bodies[0].Name);
        Assert.Equal("Giant Planet", engine.Bodies[1].Name);
        Assert.True(engine.Bodies[0].Mass > engine.Bodies[1].Mass);
    }

    [Fact]
    public void PythagoreanChaos_CentersTotalMomentumAtRest()
    {
        var engine = new CelestialNBodyEngine();
        engine.LoadPreset(CelestialPreset.PythagoreanChaos);

        Assert.Equal(3, engine.Bodies.Count);
        double totalPx = 0, totalPy = 0, totalPz = 0;
        foreach (var b in engine.Bodies)
        {
            totalPx += b.Mass * b.Vx;
            totalPy += b.Mass * b.Vy;
            totalPz += b.Mass * b.Vz;
        }

        Assert.True(Math.Abs(totalPx) < 1e-9);
        Assert.True(Math.Abs(totalPy) < 1e-9);
        Assert.True(Math.Abs(totalPz) < 1e-9);
    }

    [Fact]
    public void EvaluatePotentialAt_ApproachesZeroAtInfinity()
    {
        var engine = new CelestialNBodyEngine();
        engine.LoadPreset(CelestialPreset.FigureEight);

        double phiNear = engine.EvaluatePotentialAt(0, 0, 0);
        double phiFar = engine.EvaluatePotentialAt(100, 100, 100);

        Assert.True(phiNear < 0);
        Assert.True(Math.Abs(phiFar) < Math.Abs(phiNear));
        Assert.True(Math.Abs(phiFar) < 0.05);
    }
}
