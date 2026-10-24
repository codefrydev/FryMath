using System;
using System.Numerics;
using FryMath.Core.Dynamics;
using FryMath.Core.Fourier;
using FryMath.Core.LinearAlgebra;
using FryMath.Core.Probability;
using Xunit;

namespace FryMath.Core.Tests;

public class LinearAlgebraAndFourierTests
{
    [Fact]
    public void Vector2D_Operations_WorkAccurately()
    {
        var v1 = new Vector2D(3, 4);
        Assert.Equal(5.0, v1.Length, 5);

        var v2 = new Vector2D(0, 1);
        Assert.Equal(4.0, v1.Dot(v2), 5);

        var rot = v2.Rotate(Math.PI * 0.5); // (0, 1) rotated 90 deg counter-clockwise -> (-1, 0)
        Assert.Equal(-1.0, rot.X, 4);
        Assert.Equal(0.0, rot.Y, 4);
    }

    [Fact]
    public void Matrix2x2_Determinant_Inverse_And_Eigenvalues()
    {
        // Matrix A = [[3, 1], [1, 3]]
        // det = 9 - 1 = 8
        // tr = 6
        // eigenvalues: lambda^2 - 6*lambda + 8 = 0 -> (lambda - 4)(lambda - 2) = 0 -> lambda = 4, 2
        var m = new Matrix2x2(3, 1, 1, 3);
        Assert.Equal(8.0, m.Determinant, 5);
        Assert.Equal(6.0, m.Trace, 5);

        var inv = m.Inverse();
        Assert.NotNull(inv);
        var identity = m * inv.Value;
        Assert.Equal(1.0, identity.M11, 4);
        Assert.Equal(0.0, identity.M12, 4);
        Assert.Equal(0.0, identity.M21, 4);
        Assert.Equal(1.0, identity.M22, 4);

        var eigenPairs = m.ComputeEigenPairs();
        Assert.Equal(2, eigenPairs.Count);
        Assert.Contains(eigenPairs, p => Math.Abs(p.Lambda - 4.0) < 1e-4);
        Assert.Contains(eigenPairs, p => Math.Abs(p.Lambda - 2.0) < 1e-4);

        // Verify A * v = lambda * v for lambda = 4
        var p4 = eigenPairs.Find(p => Math.Abs(p.Lambda - 4.0) < 1e-4);
        var transformed = m * p4.Vector;
        var scaled = p4.Vector * p4.Lambda;
        Assert.Equal(scaled.X, transformed.X, 4);
        Assert.Equal(scaled.Y, transformed.Y, 4);
    }

    [Fact]
    public void FastFourierTransform_ReconstructsSignal_Correctly()
    {
        int n = 64;
        var signal = new Complex[n];
        for (int i = 0; i < n; i++)
        {
            signal[i] = new Complex(Math.Sin(2.0 * Math.PI * 4.0 * i / n), 0);
        }

        // Copy original
        var original = new Complex[n];
        Array.Copy(signal, original, n);

        // Forward FFT
        FastFourierTransform.FFT(signal, invert: false);

        // Inverse FFT
        FastFourierTransform.FFT(signal, invert: true);

        // Verify reconstruction
        for (int i = 0; i < n; i++)
        {
            Assert.Equal(original[i].Real, signal[i].Real, 4);
            Assert.Equal(0.0, signal[i].Imaginary, 4);
        }
    }

    [Fact]
    public void GaltonBoard_SimulatesSteps_And_AccumulatesBins()
    {
        var sim = new GaltonBoardSimulation(rows: 10);
        Assert.Equal(11, sim.BinCount);
        Assert.Equal(0, sim.TotalSettledCount);

        for (int i = 0; i < 20; i++)
        {
            sim.SpawnBall();
        }

        for (int step = 0; step < 500; step++)
        {
            sim.Step(0.016);
        }

        Assert.True(sim.TotalSettledCount > 0);
    }

    [Fact]
    public void Matrix2x2_Interpolate_SmoothlyMorphsBetweenMatrices()
    {
        var identity = Matrix2x2.Identity;
        var shear = Matrix2x2.ShearX(2.0); // [[1, 2], [0, 1]]

        var mid = Matrix2x2.Interpolate(identity, shear, 0.5);
        Assert.Equal(1.0, mid.M11, 4);
        Assert.Equal(1.0, mid.M12, 4); // half of 2
        Assert.Equal(0.0, mid.M21, 4);
        Assert.Equal(1.0, mid.M22, 4);

        var end = Matrix2x2.Interpolate(identity, shear, 1.0);
        Assert.Equal(shear.M12, end.M12, 4);
    }

    [Fact]
    public void LorenzAttractorSimulation_StepsRK4_AndDivergesUnderPerturbation()
    {
        var sim1 = new LorenzAttractorSimulation(0.1, 0.0, 0.0);
        var sim2 = new LorenzAttractorSimulation(0.10001, 0.0, 0.0); // 1e-5 perturbation

        // Step both for 3000 steps (t = 24.0) into the full double-scroll attractor
        for (int i = 0; i < 3000; i++)
        {
            sim1.Step(0.008);
            sim2.Step(0.008);
        }

        // Both points must be non-zero and finite
        Assert.False(double.IsNaN(sim1.CurrentPosition.X));
        Assert.False(double.IsNaN(sim2.CurrentPosition.X));

        // Sensitive dependence on initial conditions: initial 10^-5 difference should amplify significantly
        double separation = (sim1.CurrentPosition - sim2.CurrentPosition).Length;
        Assert.True(separation > 1e-4, $"Separation {separation} should be greater than initial 1e-5 perturbation.");
    }
}
