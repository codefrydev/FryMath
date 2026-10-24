using System;
using System.Collections.Generic;
using System.Linq;
using FryMath.Core.Topology;
using Xunit;

namespace FryMath.Core.Tests;

public class KnotTheoryTests
{
    [Fact]
    public void KnotEngine_SampleCurveWithBishopFrame_ProducesOrthonormalTriads()
    {
        var samples = KnotEngine.SampleCurveWithBishopFrame("trefoil", 100);
        Assert.Equal(100, samples.Count);

        foreach (var pt in samples)
        {
            // Unit vector lengths
            Assert.True(Math.Abs(pt.Tangent.Length - 1.0) < 1e-4, "Tangent must be unit length");
            Assert.True(Math.Abs(pt.Normal.Length - 1.0) < 1e-4, "Normal must be unit length");
            Assert.True(Math.Abs(pt.Binormal.Length - 1.0) < 1e-4, "Binormal must be unit length");

            // Mutual orthogonality
            Assert.True(Math.Abs(pt.Tangent.Dot(pt.Normal)) < 1e-3, "Tangent and Normal must be orthogonal");
            Assert.True(Math.Abs(pt.Tangent.Dot(pt.Binormal)) < 1e-3, "Tangent and Binormal must be orthogonal");
            Assert.True(Math.Abs(pt.Normal.Dot(pt.Binormal)) < 1e-3, "Normal and Binormal must be orthogonal");
        }
    }

    [Fact]
    public void KnotEngine_TrefoilKnot_CrossingNumberAndJonesPolynomial()
    {
        int crossings = KnotEngine.GetCrossingNumber("trefoil");
        Assert.Equal(3, crossings);

        int writhe = KnotEngine.ComputeWrithe("trefoil");
        Assert.Equal(-3, writhe);

        var jones = KnotEngine.GetJonesPolynomial("trefoil");
        Assert.Equal(3, jones.Terms.Count);

        // V(t) = -t^(-4) + t^(-3) + t^(-1)
        var tNeg4 = jones.Terms.FirstOrDefault(t => t.Exponent == -4);
        var tNeg3 = jones.Terms.FirstOrDefault(t => t.Exponent == -3);
        var tNeg1 = jones.Terms.FirstOrDefault(t => t.Exponent == -1);

        Assert.NotNull(tNeg4);
        Assert.Equal(-1, tNeg4.Coefficient);
        Assert.NotNull(tNeg3);
        Assert.Equal(1, tNeg3.Coefficient);
        Assert.NotNull(tNeg1);
        Assert.Equal(1, tNeg1.Coefficient);
    }

    [Fact]
    public void KnotEngine_FigureEight_IsAmphichiralPalindromeJonesPolynomial()
    {
        // 4_1 knot is amphichiral (equivalent to its own mirror image)
        // Therefore its Jones polynomial is palindromic: V(t) = V(t⁻¹)
        var jones = KnotEngine.GetJonesPolynomial("figure8");

        var posTerms = jones.Terms.Where(t => t.Exponent > 0).ToDictionary(t => t.Exponent, t => t.Coefficient);
        var negTerms = jones.Terms.Where(t => t.Exponent < 0).ToDictionary(t => -t.Exponent, t => t.Coefficient);

        foreach (var (exp, coeff) in posTerms)
        {
            Assert.True(negTerms.ContainsKey(exp), $"Expected negative exponent -{exp} in figure-8 Jones polynomial");
            Assert.Equal(coeff, negTerms[exp]);
        }

        // Writhe must be 0 for amphichiral knot
        int writhe = KnotEngine.ComputeWrithe("figure8");
        Assert.Equal(0, writhe);
    }

    [Fact]
    public void KnotEngine_Unknot_HasTrivialPolynomials()
    {
        var jones = KnotEngine.GetJonesPolynomial("unknot");
        Assert.Single(jones.Terms);
        Assert.Equal(0, jones.Terms[0].Exponent);
        Assert.Equal(1, jones.Terms[0].Coefficient);
        Assert.Equal("1", jones.ToString());

        var alexander = KnotEngine.GetAlexanderPolynomial("unknot");
        Assert.Single(alexander.Terms);
        Assert.Equal(0, alexander.Terms[0].Exponent);
        Assert.Equal(1, alexander.Terms[0].Coefficient);
    }

    [Fact]
    public void KnotEngine_HopfLink_GaussLinkingIntegral_EqualsOne()
    {
        // Sample two linked circles
        var c1 = new List<FryMath.Core.LinearAlgebra.Vector3D>();
        var c2 = new List<FryMath.Core.LinearAlgebra.Vector3D>();
        int n = 120;
        double dt = 2.0 * Math.PI / n;

        for (int i = 0; i < n; i++)
        {
            c1.Add(KnotEngine.EvaluateCurve("hopf", i * dt, componentIndex: 0));
            c2.Add(KnotEngine.EvaluateCurve("hopf", i * dt, componentIndex: 1));
        }

        double lk = KnotEngine.ComputeLinkingNumber(c1, c2);
        // Discrete Gauss integral on 120 samples matches 1.0 within numerical discretization tolerance
        Assert.True(Math.Abs(Math.Abs(lk) - 1.0) < 0.15, $"Expected linking number ~1.0, got {lk}");
    }

    [Fact]
    public void KnotEngine_BorromeanRings_ComponentLinkingNumbersAreZero()
    {
        // Borromean rings: every pair of rings is unlinked (Lk = 0), yet all 3 together are inextricably linked
        var ring0 = new List<FryMath.Core.LinearAlgebra.Vector3D>();
        var ring1 = new List<FryMath.Core.LinearAlgebra.Vector3D>();
        var ring2 = new List<FryMath.Core.LinearAlgebra.Vector3D>();
        int n = 100;
        double dt = 2.0 * Math.PI / n;

        for (int i = 0; i < n; i++)
        {
            ring0.Add(KnotEngine.EvaluateBorromeanRing(0, i * dt));
            ring1.Add(KnotEngine.EvaluateBorromeanRing(1, i * dt));
            ring2.Add(KnotEngine.EvaluateBorromeanRing(2, i * dt));
        }

        double lk01 = KnotEngine.ComputeLinkingNumber(ring0, ring1);
        double lk12 = KnotEngine.ComputeLinkingNumber(ring1, ring2);
        double lk20 = KnotEngine.ComputeLinkingNumber(ring2, ring0);

        Assert.True(Math.Abs(lk01) < 0.05, $"Borromean Lk(0,1) must be 0, got {lk01}");
        Assert.True(Math.Abs(lk12) < 0.05, $"Borromean Lk(1,2) must be 0, got {lk12}");
        Assert.True(Math.Abs(lk20) < 0.05, $"Borromean Lk(2,0) must be 0, got {lk20}");
    }

    [Fact]
    public void KnotEngine_ArtinBraidWord_ParsesAndSatisfiesYangBaxterRelation()
    {
        // Standard braid word for trefoil: (σ₁ σ₂⁻¹)³ or σ₁³ on 2 strands
        var braid = KnotEngine.ParseBraidWord("s1 s2^-1 s1 s2^-1 s1 s2^-1", 3);
        Assert.Equal(3, braid.StrandCount);
        Assert.Equal(6, braid.Crossings.Count);

        // Verify Yang-Baxter Artin relation σ₁ σ₂ σ₁ = σ₂ σ₁ σ₂
        bool yb = KnotEngine.VerifyArtinBraidRelation();
        Assert.True(yb, "Artin braid relation σ₁ σ₂ σ₁ = σ₂ σ₁ σ₂ must hold identically");
    }

    [Fact]
    public void KnotEngine_AlexanderPolynomial_Symmetry()
    {
        // For any knot, Alexander polynomial satisfies Δ(t) = Δ(t⁻¹)
        var delta = KnotEngine.GetAlexanderPolynomial("trefoil");
        var posTerms = delta.Terms.Where(t => t.Exponent > 0).ToDictionary(t => t.Exponent, t => t.Coefficient);
        var negTerms = delta.Terms.Where(t => t.Exponent < 0).ToDictionary(t => -t.Exponent, t => t.Coefficient);

        foreach (var (exp, coeff) in posTerms)
        {
            Assert.True(negTerms.ContainsKey(exp));
            Assert.Equal(coeff, negTerms[exp]);
        }
    }
}
