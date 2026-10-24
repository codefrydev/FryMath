using System;
using System.Linq;
using FryMath.Core.Information;
using Xunit;

namespace FryMath.Core.Tests;

public class InformationTheoryTests
{
    [Fact]
    public void ShannonEntropy_CalculatesWithinTheoreticalBounds()
    {
        var engine = new InformationTheoryEngine();
        engine.SetSourceText("AAAAABBBCC");

        Assert.True(engine.ShannonEntropy > 0);
        Assert.True(engine.ShannonEntropy <= engine.MaxEntropy);
        Assert.True(engine.Redundancy >= 0.0 && engine.Redundancy <= 1.0);
    }

    [Fact]
    public void HuffmanCoding_SatisfiesSourceCodingTheorem()
    {
        var engine = new InformationTheoryEngine();
        engine.SetSourceText("SHANNON ENTROPY AND INFORMATION THEORY");

        Assert.NotNull(engine.HuffmanRoot);
        Assert.True(engine.Alphabet.Count > 5);

        // All codes must be non-empty prefix-free
        var codes = engine.Alphabet.Select(s => s.HuffmanCode).ToList();
        for (int i = 0; i < codes.Count; i++)
        {
            for (int j = 0; j < codes.Count; j++)
            {
                if (i != j)
                {
                    Assert.False(codes[i].StartsWith(codes[j]), $"Code {codes[j]} is a prefix of {codes[i]}");
                }
            }
        }

        // Shannon Source Coding Theorem: H(X) <= L < H(X) + 1
        Assert.True(engine.AverageCodeLength >= engine.ShannonEntropy - 1e-6);
        Assert.True(engine.AverageCodeLength < engine.ShannonEntropy + 1.0 + 1e-6);
        Assert.True(engine.CompressionRatio > 0.0);
    }

    [Fact]
    public void Constellation_GeneratesExpectedPointCounts()
    {
        var engine = new InformationTheoryEngine();

        engine.SetModulation(ModulationScheme.Bpsk);
        Assert.Equal(2, engine.Constellation.Count);

        engine.SetModulation(ModulationScheme.Qpsk);
        Assert.Equal(4, engine.Constellation.Count);

        engine.SetModulation(ModulationScheme.Psk8);
        Assert.Equal(8, engine.Constellation.Count);

        engine.SetModulation(ModulationScheme.Qam16);
        Assert.Equal(16, engine.Constellation.Count);
    }

    [Fact]
    public void ChannelCapacity_IncreasesWithSnr()
    {
        var engine = new InformationTheoryEngine();

        engine.SetSnrDb(0.0);
        double cLow = engine.ChannelCapacity;

        engine.SetSnrDb(20.0);
        double cHigh = engine.ChannelCapacity;

        Assert.True(cHigh > cLow);
    }

    [Fact]
    public void BinarySymmetricChannel_CorrectExtrema()
    {
        var engine = new InformationTheoryEngine();

        engine.SetCrossoverProbability(0.0);
        Assert.Equal(0.0, engine.BinaryEntropy, 3);
        Assert.Equal(1.0, engine.BscCapacity, 3);

        engine.SetCrossoverProbability(0.5);
        Assert.Equal(1.0, engine.BinaryEntropy, 3);
        Assert.Equal(0.0, engine.BscCapacity, 3);
    }

    [Fact]
    public void KlDivergence_IsNonNegativeAndAsymmetric()
    {
        var engine = new InformationTheoryEngine();
        engine.SetDistributions([0.6, 0.4], [0.2, 0.8]);

        Assert.True(engine.KlDivergencePQ >= 0);
        Assert.True(engine.KlDivergenceQP >= 0);
        Assert.NotEqual(engine.KlDivergencePQ, engine.KlDivergenceQP);
    }
}
