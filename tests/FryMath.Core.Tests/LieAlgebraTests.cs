using System;
using System.Collections.Generic;
using System.Linq;
using FryMath.Core.Lie;
using Xunit;

namespace FryMath.Core.Tests;

public class LieAlgebraTests
{
    [Theory]
    [InlineData(LieType.A2, 6, 2, 8, 3)]
    [InlineData(LieType.B2, 8, 2, 10, 4)]
    [InlineData(LieType.G2, 12, 2, 14, 6)]
    [InlineData(LieType.A3, 12, 3, 15, 4)]
    [InlineData(LieType.B3, 18, 3, 21, 6)]
    [InlineData(LieType.E8, 240, 8, 248, 30)]
    public void LieAlgebraEngine_GeneratesCorrectInvariants(LieType type, int expectedRoots, int expectedRank, int expectedDim, int expectedCoxeter)
    {
        var rootSystem = LieAlgebraEngine.BuildLieAlgebra(type);

        Assert.NotNull(rootSystem);
        Assert.Equal(type, rootSystem.Type);
        Assert.Equal(expectedRoots, rootSystem.Roots.Count);
        Assert.Equal(expectedRank, rootSystem.Rank);
        Assert.Equal(expectedDim, rootSystem.Dimension);
        Assert.Equal(expectedCoxeter, rootSystem.CoxeterNumber);
        Assert.Equal(expectedRank, rootSystem.SimpleRoots.Count);
    }

    [Theory]
    [InlineData(LieType.A2)]
    [InlineData(LieType.B2)]
    [InlineData(LieType.G2)]
    [InlineData(LieType.A3)]
    [InlineData(LieType.B3)]
    [InlineData(LieType.E8)]
    public void LieAlgebraEngine_CartanMatrixProperties(LieType type)
    {
        var rootSystem = LieAlgebraEngine.BuildLieAlgebra(type);
        var A = rootSystem.CartanMatrix;
        int rank = rootSystem.Rank;

        Assert.Equal(rank, A.GetLength(0));
        Assert.Equal(rank, A.GetLength(1));

        // Diagonal elements must be 2: A_ii = 2
        for (int i = 0; i < rank; i++)
        {
            Assert.Equal(2, A[i, i]);
        }

        // Off-diagonal elements must be non-positive integers: A_ij in {0, -1, -2, -3}
        for (int i = 0; i < rank; i++)
        {
            for (int j = 0; j < rank; j++)
            {
                if (i != j)
                {
                    Assert.True(A[i, j] <= 0, $"Cartan matrix entry A[{i},{j}] should be non-positive, got {A[i,j]}");
                    Assert.True(A[i, j] >= -3, $"Cartan matrix entry A[{i},{j}] should be >= -3, got {A[i,j]}");
                }
            }
        }
    }

    [Theory]
    [InlineData(LieType.A2)]
    [InlineData(LieType.B2)]
    [InlineData(LieType.G2)]
    public void LieAlgebraEngine_WeylReflectionInvariance2D(LieType type)
    {
        var rootSystem = LieAlgebraEngine.BuildLieAlgebra(type);

        foreach (var simpleRoot in rootSystem.SimpleRoots)
        {
            foreach (var root in rootSystem.Roots)
            {
                var reflected = LieAlgebraEngine.Reflect(root.Coordinates, simpleRoot);

                // Check that reflected vector is close to one of the roots in the root system
                bool foundMatch = false;
                foreach (var other in rootSystem.Roots)
                {
                    double distSq = 0;
                    for (int k = 0; k < rootSystem.Rank; k++)
                    {
                        double diff = reflected[k] - other.Coordinates[k];
                        distSq += diff * diff;
                    }
                    if (distSq < 1e-6)
                    {
                        foundMatch = true;
                        break;
                    }
                }

                Assert.True(foundMatch, $"Weyl reflection of root by simple root did not land in root system for {type}");
            }
        }
    }

    [Fact]
    public void LieAlgebraEngine_DynkinDiagram_Topology()
    {
        var a2 = LieAlgebraEngine.BuildLieAlgebra(LieType.A2);
        Assert.Equal(2, a2.DynkinNodes.Count);
        Assert.Single(a2.DynkinEdges);
        Assert.Equal(1, a2.DynkinEdges[0].Multiplicity);

        var g2 = LieAlgebraEngine.BuildLieAlgebra(LieType.G2);
        Assert.Equal(2, g2.DynkinNodes.Count);
        Assert.Single(g2.DynkinEdges);
        Assert.Equal(3, g2.DynkinEdges[0].Multiplicity); // G2 has triple bond!

        var b2 = LieAlgebraEngine.BuildLieAlgebra(LieType.B2);
        Assert.Equal(2, b2.DynkinNodes.Count);
        Assert.Single(b2.DynkinEdges);
        Assert.Equal(2, b2.DynkinEdges[0].Multiplicity); // B2 has double bond!

        var a3 = LieAlgebraEngine.BuildLieAlgebra(LieType.A3);
        Assert.Equal(3, a3.DynkinNodes.Count);
        Assert.Equal(2, a3.DynkinEdges.Count);

        var b3 = LieAlgebraEngine.BuildLieAlgebra(LieType.B3);
        Assert.Equal(3, b3.DynkinNodes.Count);
        Assert.Equal(2, b3.DynkinEdges.Count);

        var e8 = LieAlgebraEngine.BuildLieAlgebra(LieType.E8);
        Assert.Equal(8, e8.DynkinNodes.Count);
        Assert.Equal(7, e8.DynkinEdges.Count);
    }

    [Fact]
    public void LieAlgebraEngine_E8_CoxeterProjectionRings()
    {
        var e8 = LieAlgebraEngine.BuildLieAlgebra(LieType.E8);
        Assert.Equal(240, e8.Roots.Count);

        // Projected onto 2D Coxeter plane, 240 roots group into concentric rings
        var radii = e8.Roots.Select(r => Math.Round(Math.Sqrt(r.X2D * r.X2D + r.Y2D * r.Y2D), 2))
                            .Distinct()
                            .OrderBy(r => r)
                            .ToList();

        // There are 8 concentric Coxeter 30-gon rings
        Assert.Equal(8, radii.Count);
    }
}
