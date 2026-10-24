using System;
using FryMath.Core.Fractals;
using Xunit;

namespace FryMath.Core.Tests;

public class FractalTests
{
    [Fact]
    public void Mandelbrot_OriginAndPeriod2Bulb_AreInside()
    {
        // c = 0 should be inside (returns max iterations)
        double iter0 = FractalEngine.EvaluateMandelbrot(0, 0, 100);
        Assert.Equal(100, iter0);

        // c = -1 should be inside period-2 bulb
        double iterBulb = FractalEngine.EvaluateMandelbrot(-1.0, 0, 100);
        Assert.Equal(100, iterBulb);
    }

    [Fact]
    public void Mandelbrot_FarPoints_EscapeQuickly()
    {
        // c = 2.5 is clearly outside
        double iterOutside = FractalEngine.EvaluateMandelbrot(2.5, 0, 100);
        Assert.True(iterOutside < 10);
    }

    [Fact]
    public void Julia_OriginForSmallC_IsInside()
    {
        // For c = 0, Julia set is unit disk, z = 0 is inside
        double iter = FractalEngine.EvaluateJulia(0, 0, 0, 0, 100);
        Assert.Equal(100, iter);

        // z = 2 for c = 0 escapes
        double iterEscape = FractalEngine.EvaluateJulia(2.0, 0, 0, 0, 100);
        Assert.True(iterEscape < 10);
    }

    [Fact]
    public void NewtonBasin_Degree3_ConvergesToRootsOfUnity()
    {
        // Close to root 1 + 0i (root index 0)
        var (iter1, root1) = FractalEngine.EvaluateNewton(0.9, 0.05, 3, 50);
        Assert.Equal(0, root1);
        Assert.True(iter1 < 20);

        // Close to root e^(i 2pi/3) = -0.5 + i sqrt(3)/2 ~= -0.5 + 0.866i (root index 1)
        var (iter2, root2) = FractalEngine.EvaluateNewton(-0.45, 0.8, 3, 50);
        Assert.Equal(1, root2);
        Assert.True(iter2 < 20);
    }

    [Fact]
    public void RenderToBuffer_FillsFullBuffer()
    {
        int w = 64;
        int h = 48;
        uint[] buffer = new uint[w * h];
        var p = new FractalParams { Type = FractalType.Mandelbrot, MaxIterations = 30 };

        FractalEngine.RenderToBuffer(buffer, w, h, -0.5, 0.0, 1.0, p);

        // Verify non-zero pixels
        int nonZero = 0;
        foreach (var px in buffer)
        {
            if (px != 0) nonZero++;
        }

        Assert.Equal(w * h, nonZero);
    }
}
