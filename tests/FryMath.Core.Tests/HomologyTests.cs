using System.Linq;
using FryMath.Core.Topology;
using Xunit;

namespace FryMath.Core.Tests;

public class HomologyTests
{
    [Fact]
    public void SimplicialHomologyEngine_BoundarySquaredIsIdenticallyZero()
    {
        var torus = SimplicialHomologyEngine.CreateTorus();
        var resTorus = torus.ComputeHomology();
        Assert.True(resTorus.D1D2IsZero, "Fundamental Homological Axiom ∂₁ ∘ ∂₂ = 0 must hold on Torus!");

        var sphere = SimplicialHomologyEngine.CreateSphere();
        var resSphere = sphere.ComputeHomology();
        Assert.True(resSphere.D1D2IsZero, "Fundamental Homological Axiom ∂₁ ∘ ∂₂ = 0 must hold on Sphere!");

        var mobius = SimplicialHomologyEngine.CreateMobiusStrip();
        var resMobius = mobius.ComputeHomology();
        Assert.True(resMobius.D1D2IsZero, "Fundamental Homological Axiom ∂₁ ∘ ∂₂ = 0 must hold on Möbius Strip!");

        var rp2 = SimplicialHomologyEngine.CreateProjectivePlane();
        var resRp2 = rp2.ComputeHomology();
        Assert.True(resRp2.D1D2IsZero, "Fundamental Homological Axiom ∂₁ ∘ ∂₂ = 0 must hold on ℝP²!");
    }

    [Fact]
    public void SimplicialHomologyEngine_Torus_HasCorrectBettiNumbersAndEulerCharacteristic()
    {
        var torus = SimplicialHomologyEngine.CreateTorus();
        var res = torus.ComputeHomology();

        Assert.Equal(9, res.VertexCount);
        Assert.Equal(27, res.EdgeCount);
        Assert.Equal(18, res.TriangleCount);

        Assert.Equal(1, res.Betti0); // 1 connected component
        Assert.Equal(2, res.Betti1); // 2 independent non-bounding 1-cycles: meridian & longitudinal
        Assert.Equal(1, res.Betti2); // 1 enclosed 2D void

        Assert.Equal(0, res.EulerSimplicial); // V - E + F = 9 - 27 + 18 = 0
        Assert.Equal(0, res.EulerHomological); // β₀ - β₁ + β₂ = 1 - 2 + 1 = 0
        Assert.Equal(res.EulerSimplicial, res.EulerHomological);
    }

    [Fact]
    public void SimplicialHomologyEngine_Sphere_HasCorrectBettiNumbers()
    {
        var sphere = SimplicialHomologyEngine.CreateSphere();
        var res = sphere.ComputeHomology();

        Assert.Equal(6, res.VertexCount);
        Assert.Equal(12, res.EdgeCount);
        Assert.Equal(8, res.TriangleCount);

        Assert.Equal(1, res.Betti0); // 1 connected component
        Assert.Equal(0, res.Betti1); // Simply connected! Every 1-cycle is the boundary of a 2-chain
        Assert.Equal(1, res.Betti2); // 1 enclosed hollow spherical cavity

        Assert.Equal(2, res.EulerSimplicial); // V - E + F = 6 - 12 + 8 = 2
        Assert.Equal(2, res.EulerHomological); // β₀ - β₁ + β₂ = 1 - 0 + 1 = 2
        Assert.Equal(res.EulerSimplicial, res.EulerHomological);
    }

    [Fact]
    public void SimplicialHomologyEngine_MobiusStrip_HasSingleHole()
    {
        var mobius = SimplicialHomologyEngine.CreateMobiusStrip();
        var res = mobius.ComputeHomology();

        Assert.Equal(1, res.Betti0); // 1 connected component
        Assert.Equal(1, res.Betti1); // 1 generator cycle around center loop
        Assert.Equal(0, res.Betti2); // Surface has non-empty boundary, no closed 2-cycle

        Assert.Equal(res.EulerSimplicial, res.EulerHomological);
    }

    [Fact]
    public void SimplicialHomologyEngine_ProjectivePlane_EulerCharacteristicIsOne()
    {
        var rp2 = SimplicialHomologyEngine.CreateProjectivePlane();
        var res = rp2.ComputeHomology();

        Assert.Equal(6, res.VertexCount);
        Assert.Equal(15, res.EdgeCount);
        Assert.Equal(10, res.TriangleCount);

        Assert.Equal(1, res.EulerSimplicial); // V - E + F = 6 - 15 + 10 = 1
        Assert.Equal(1, res.Betti0);
        Assert.Equal(0, res.Betti2); // Over Q, non-orientable closed surface has H_2 = 0
    }

    [Fact]
    public void SimplicialHomologyEngine_PersistenceBarcodes_DetectAnnulusHole()
    {
        var barcodes = SimplicialHomologyEngine.ComputePersistenceBarcodes("annulus", 2.0);

        Assert.NotEmpty(barcodes);
        var h1Hole = barcodes.FirstOrDefault(b => b.Dimension == 1 && b.FeatureLabel.Contains("Annulus Hole"));
        Assert.NotNull(h1Hole);
        Assert.True(h1Hole.Death > h1Hole.Birth);
        Assert.True(h1Hole.Death - h1Hole.Birth > 0.5, "Persistent topological feature must have significant lifespan!");
    }
}
