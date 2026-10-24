using System;
using FryMath.Core.Topology4D;
using Xunit;

namespace FryMath.Core.Tests;

public class Topology4DTests
{
    [Fact]
    public void Vector4D_Rotations_Preserve4DEuclideanNorm()
    {
        var v = new Vector4D(1.2, -0.8, 2.1, 0.9);
        double originalNorm = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z + v.W * v.W);

        var r = v.RotateXW(0.7)
                 .RotateYW(-1.2)
                 .RotateZW(0.4)
                 .RotateXY(1.8);

        double rotatedNorm = Math.Sqrt(r.X * r.X + r.Y * r.Y + r.Z * r.Z + r.W * r.W);

        Assert.True(Math.Abs(rotatedNorm - originalNorm) < 1e-9,
            $"4D isoclinic rotation must be unitary/orthogonal: original={originalNorm}, rotated={rotatedNorm}");
    }

    [Fact]
    public void Tesseract_HasExactly16VerticesAnd32Edges()
    {
        var engine = new Manifold4DEngine();
        engine.LoadPreset(ManifoldPresetType.Tesseract);

        Assert.Equal(16, engine.BaseVertices.Count);
        Assert.Equal(32, engine.Edges.Count);
    }

    [Fact]
    public void Cell24_HasExactly24VerticesAnd96Edges()
    {
        var engine = new Manifold4DEngine();
        engine.LoadPreset(ManifoldPresetType.Cell24);

        Assert.Equal(24, engine.BaseVertices.Count);
        Assert.Equal(96, engine.Edges.Count);
    }

    [Fact]
    public void CliffordTorus_LiesStrictlyOnUnit3Sphere()
    {
        var engine = new Manifold4DEngine();
        engine.LoadPreset(ManifoldPresetType.CliffordTorus);

        foreach (var v in engine.BaseVertices)
        {
            double r2 = v.X * v.X + v.Y * v.Y + v.Z * v.Z + v.W * v.W;
            Assert.True(Math.Abs(r2 - 1.0) < 1e-6, $"Clifford torus must lie on S^3: r^2={r2}");
        }
    }

    [Fact]
    public void All4DPresets_ProjectToValidFiniteScreenPoints()
    {
        var engine = new Manifold4DEngine();
        foreach (ManifoldPresetType preset in Enum.GetValues<ManifoldPresetType>())
        {
            engine.LoadPreset(preset);
            var pts = engine.GetProjectedVertices(1200, 800, 180);

            Assert.Equal(engine.BaseVertices.Count, pts.Count);
            foreach (var pt in pts)
            {
                Assert.False(double.IsNaN(pt.ScreenX));
                Assert.False(double.IsNaN(pt.ScreenY));
                Assert.False(double.IsInfinity(pt.ScreenX));
                Assert.False(double.IsInfinity(pt.ScreenY));
            }
        }
    }
}
