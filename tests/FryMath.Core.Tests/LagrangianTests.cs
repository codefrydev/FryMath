using System;
using FryMath.Core.Lagrangian;
using Xunit;

namespace FryMath.Core.Tests;

public class LagrangianTests
{
    [Fact]
    public void DoublePendulum_ConservesTotalEnergyInConservativeRegime()
    {
        var engine = new LagrangianMechanicsEngine();
        engine.LoadPreset("Double Pendulum Chaos");
        engine.Damping = 0.0;

        double initialE = engine.TotalEnergy;

        for (int i = 0; i < 200; i++)
        {
            engine.Step(0.005);
        }

        double finalE = engine.TotalEnergy;
        double relativeError = Math.Abs(finalE - initialE) / Math.Abs(initialE);

        // RK4 symplectic behavior should maintain relative error < 1e-3
        Assert.True(relativeError < 1e-3, $"Energy drift too large: {relativeError:E3}");
    }

    [Fact]
    public void DoublePendulum_ExhibitsLyapunovDivergence()
    {
        var engine = new LagrangianMechanicsEngine();
        engine.ResetToState(Math.PI * 0.7, Math.PI * 0.8, 0.0, 0.0);

        double initialSep = engine.LyapunovSeparation;
        Assert.Equal(1e-4, initialSep, 4);

        // Advance simulation through chaotic phase space
        for (int i = 0; i < 400; i++)
        {
            engine.Step(0.01);
        }

        double finalSep = engine.LyapunovSeparation;
        // Due to chaotic divergence (positive Lyapunov exponent), separation should grow by orders of magnitude
        Assert.True(finalSep > initialSep * 10.0, $"Expected chaotic divergence, but initial={initialSep}, final={finalSep}");
    }

    [Fact]
    public void DoublePendulum_CoordinatesMatchLinkLengths()
    {
        var engine = new LagrangianMechanicsEngine();
        engine.L1 = 1.2;
        engine.L2 = 0.8;
        engine.ResetToState(0.5, -0.3, 0.0, 0.0);

        (double x1, double y1, double x2, double y2) = engine.GetDoublePendulumCoordinates(engine.State);

        double len1 = Math.Sqrt(x1 * x1 + y1 * y1);
        double len2 = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

        Assert.Equal(1.2, len1, 4);
        Assert.Equal(0.8, len2, 4);
    }

    [Fact]
    public void SpringPendulum_ExecutesBoundedMotion()
    {
        var engine = new LagrangianMechanicsEngine();
        engine.LoadPreset("Spring Pendulum (Fermi Resonance)");

        for (int i = 0; i < 150; i++)
        {
            engine.Step(0.01);
            Assert.True(engine.SpringR > 0.1 && engine.SpringR < 3.0);
            Assert.True(!double.IsNaN(engine.TotalEnergy));
        }
    }
}
