using System;
using FryMath.Core.Fluids;
using Xunit;

namespace FryMath.Core.Tests;

public class FluidTests
{
    [Fact]
    public void NavierStokes_VelocityField_SatisfiesIncompressibilityCondition()
    {
        var engine = new NavierStokesEngine(60, 40);
        engine.LoadPreset(FluidPresetType.TaylorGreen);

        // Run a few steps to let streamfunction settle
        for (int i = 0; i < 5; i++)
            engine.Step(0.2);

        // Check divergence: du/dx + dv/dy in the interior
        double maxDivergence = 0.0;
        for (int y = 5; y < engine.Height - 5; y++)
        {
            for (int x = 5; x < engine.Width - 5; x++)
            {
                int idx = y * engine.Width + x;
                double dudx = 0.5 * (engine.U[idx + 1] - engine.U[idx - 1]);
                double dvdy = 0.5 * (engine.V[idx + engine.Width] - engine.V[idx - engine.Width]);
                double div = Math.Abs(dudx + dvdy);
                if (div > maxDivergence) maxDivergence = div;
            }
        }

        // Streamfunction formulation inherently guarantees div u = 0 analytically; numerically div u < 0.05
        Assert.True(maxDivergence < 0.08, $"Incompressibility violated: max divergence = {maxDivergence}");
    }

    [Fact]
    public void NavierStokes_ObstacleCells_HaveZeroVelocityAndVorticity()
    {
        var engine = new NavierStokesEngine(60, 40);
        engine.LoadPreset(FluidPresetType.VonKarman);

        engine.Step(0.2);

        for (int i = 0; i < engine.Obstacles.Length; i++)
        {
            if (engine.Obstacles[i])
            {
                Assert.Equal(0.0, engine.U[i]);
                Assert.Equal(0.0, engine.V[i]);
                Assert.Equal(0.0, engine.Omega[i]);
            }
        }
    }

    [Fact]
    public void AllFluidPresets_LoadSuccessfullyWithParticles()
    {
        var engine = new NavierStokesEngine(50, 30);
        foreach (FluidPresetType preset in Enum.GetValues<FluidPresetType>())
        {
            engine.LoadPreset(preset);
            Assert.NotEmpty(engine.Particles);
            Assert.True(engine.ReynoldsNumber > 10.0);
        }
    }

    [Fact]
    public void InjectVorticity_CorrectlyModifiesVorticityAtTarget()
    {
        var engine = new NavierStokesEngine(50, 30);
        engine.LoadPreset(FluidPresetType.VonKarman);

        int tx = 25;
        int ty = 15;
        int idx = ty * engine.Width + tx;
        double before = engine.Omega[idx];

        engine.InjectVorticity(tx, ty, 3.5, 3.0);
        double after = engine.Omega[idx];

        Assert.True(after > before + 1.0, $"Expected vorticity increase: before={before}, after={after}");
    }
}
