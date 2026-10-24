using System;
using FryMath.Core.Heat;
using Xunit;

namespace FryMath.Core.Tests;

public class HeatTests
{
    [Fact]
    public void HeatEquation_NeumannBoundary_ConservesThermalEnergy()
    {
        var engine = new HeatDiffusionEngine(40, 40)
        {
            Boundary = BoundaryType.NeumannInsulated,
            Diffusivity = 0.20
        };

        // Seed with a localized heat blob
        engine.Temperature[20, 20] = 100.0;
        engine.Temperature[21, 20] = 50.0;
        engine.Temperature[20, 21] = 50.0;

        double initialEnergy = 0.0;
        for (int x = 0; x < engine.Width; x++)
            for (int y = 0; y < engine.Height; y++)
                initialEnergy += engine.Temperature[x, y];

        // Step forward 50 diffusion time steps
        for (int s = 0; s < 50; s++)
        {
            engine.Step(0.5);
        }

        double finalEnergy = 0.0;
        for (int x = 0; x < engine.Width; x++)
            for (int y = 0; y < engine.Height; y++)
                finalEnergy += engine.Temperature[x, y];

        // Energy must be conserved on insulated adiabatic boundary
        Assert.Equal(initialEnergy, finalEnergy, 3);
    }

    [Fact]
    public void HeatFlux_FourierLaw_DirectsFromHotToCold()
    {
        var engine = new HeatDiffusionEngine(40, 40);
        engine.LoadPreset("dipole"); // Hot source at x=0.32, Cold sink at x=0.68

        // Sample flux midway between hot and cold points (x=0.5, y=0.5)
        var flux = engine.SampleFlux(0.5, 0.5);

        // Heat flows from left (hot) to right (cold), so Qx should be positive!
        Assert.True(flux.Qx > 0.0, $"Expected positive heat flux Qx from hot to cold, but got {flux.Qx}");
    }

    [Fact]
    public void RelaxToSteadyState_SatisfiesLaplaceEquation()
    {
        var engine = new HeatDiffusionEngine(30, 30);
        engine.LoadPreset("laplace");

        // Sample internal cell (x=15, y=15)
        int x = 15;
        int y = 15;
        double left = engine.Temperature[x - 1, y];
        double right = engine.Temperature[x + 1, y];
        double up = engine.Temperature[x, y - 1];
        double down = engine.Temperature[x, y + 1];
        double laplacian = left + right + up + down - 4.0 * engine.Temperature[x, y];

        // Laplacian should be nearly 0 in steady state equilibrium
        Assert.True(Math.Abs(laplacian) < 0.05, $"Expected Laplacian ≈ 0, but got {laplacian}");
    }
}
