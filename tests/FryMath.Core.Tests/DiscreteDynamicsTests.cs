using System;
using System.Linq;
using FryMath.Core.Dynamics;
using Xunit;

namespace FryMath.Core.Tests;

public class DiscreteDynamicsTests
{
    [Fact]
    public void ArnoldCatMap_PeriodCalculationAndReconstitution_Exact()
    {
        var engine = new DiscreteDynamicsEngine();
        engine.InitializeCatPattern();

        int period = DiscreteDynamicsEngine.ComputeCatPeriod(DiscreteDynamicsEngine.CatGridSize);
        Assert.Equal(48, period);
        Assert.Equal(48, engine.CatPeriod);

        // Store copy of initial pattern
        byte[,] initial = new byte[DiscreteDynamicsEngine.CatGridSize, DiscreteDynamicsEngine.CatGridSize];
        for (int y = 0; y < DiscreteDynamicsEngine.CatGridSize; y++)
            for (int x = 0; x < DiscreteDynamicsEngine.CatGridSize; x++)
                initial[x, y] = engine.CurrentCatPattern[x, y];

        // Step 1: should be scrambled (not identical to initial)
        engine.StepCatMap();
        Assert.Equal(1, engine.CatStep);
        bool identicalAfterOne = true;
        for (int y = 0; y < DiscreteDynamicsEngine.CatGridSize; y++)
            for (int x = 0; x < DiscreteDynamicsEngine.CatGridSize; x++)
                if (engine.CurrentCatPattern[x, y] != initial[x, y]) { identicalAfterOne = false; break; }
        Assert.False(identicalAfterOne);

        // Step remaining 47 steps: must reconstitute perfectly to step 0
        for (int i = 1; i < 48; i++)
        {
            engine.StepCatMap();
        }

        Assert.Equal(0, engine.CatStep);
        for (int y = 0; y < DiscreteDynamicsEngine.CatGridSize; y++)
        {
            for (int x = 0; x < DiscreteDynamicsEngine.CatGridSize; x++)
            {
                Assert.Equal(initial[x, y], engine.CurrentCatPattern[x, y]);
            }
        }
    }

    [Fact]
    public void LogisticMap_FixedPointsAndCobweb_Exact()
    {
        // For r = 2.5, fixed point x* = 1 - 1/2.5 = 0.6
        double r = 2.5;
        double fixedPt = DiscreteDynamicsEngine.EvaluateLogistic(0.6, r);
        Assert.Equal(0.6, fixedPt, 4);

        var engine = new DiscreteDynamicsEngine
        {
            LogisticR = 2.5,
            LogisticX0 = 0.2,
            CobwebSteps = 20
        };

        var orbit = engine.GenerateCobwebOrbit();
        Assert.True(orbit.Count > 20);

        // Orbit should converge monotonically to 0.6
        var last = orbit.Last();
        Assert.Equal(0.6, last.X, 3);
        Assert.Equal(0.6, last.Y, 3);
    }

    [Fact]
    public void LogisticMap_LyapunovExponent_DifferentiatesPeriodicAndChaotic()
    {
        var engine = new DiscreteDynamicsEngine();

        // Stable periodic regime r = 2.8: lambda < 0
        double lambdaStable = engine.ComputeLyapunovExponent(2.8);
        Assert.True(lambdaStable < 0, $"Expected negative Lyapunov for r=2.8, got {lambdaStable}");

        // Deterministic chaotic regime r = 3.9: lambda > 0
        double lambdaChaos = engine.ComputeLyapunovExponent(3.9);
        Assert.True(lambdaChaos > 0, $"Expected positive Lyapunov for r=3.9, got {lambdaChaos}");
    }

    [Fact]
    public void LogisticMap_BifurcationDiagram_GeneratesPointsInRange()
    {
        var engine = new DiscreteDynamicsEngine();
        var points = engine.GenerateBifurcationDiagram(rMin: 2.8, rMax: 3.8, rSteps: 50, warmup: 50, keep: 20);

        Assert.NotEmpty(points);
        foreach (var (r, x) in points)
        {
            Assert.True(r >= 2.8 && r <= 3.8);
            Assert.True(x >= 0.0 && x <= 1.0);
        }
    }

    [Fact]
    public void IteratedFunctionSystems_BarnsleyFernAndSierpinski_ValidPointClouds()
    {
        var engine = new DiscreteDynamicsEngine
        {
            IfsPreset = "barnsley",
            IfsPointsCount = 2000
        };

        var fernPoints = engine.GenerateIfsPoints();
        Assert.Equal(2000, fernPoints.Count);
        foreach (var (x, y, idx) in fernPoints)
        {
            Assert.True(x >= -3.5 && x <= 3.5, $"X out of bounds: {x}");
            Assert.True(y >= -0.5 && y <= 10.5, $"Y out of bounds: {y}");
            Assert.True(idx >= 0 && idx <= 3);
        }

        engine.IfsPreset = "sierpinski";
        engine.IfsPointsCount = 1500;
        var sierpinskiPoints = engine.GenerateIfsPoints();
        Assert.Equal(1500, sierpinskiPoints.Count);
        foreach (var (x, y, idx) in sierpinskiPoints)
        {
            Assert.True(x >= -0.1 && x <= 1.1, $"X out of bounds: {x}");
            Assert.True(y >= -0.1 && y <= 1.1, $"Y out of bounds: {y}");
            Assert.True(idx >= 0 && idx <= 2);
        }
    }
}
