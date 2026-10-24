using System;
using FryMath.Core.Relativity;
using Xunit;

namespace FryMath.Core.Tests;

public class RelativityTests
{
    [Fact]
    public void SpacetimeInterval_IsStrictlyInvariant_UnderLorentzBoost()
    {
        double x = 3.5;
        double ct = 7.2;
        double originalS2 = ct * ct - x * x;

        double[] betas = [-0.85, -0.6, -0.2, 0.0, 0.3, 0.75, 0.95];

        foreach (double beta in betas)
        {
            var (xPrime, ctPrime) = MinkowskiEngine.LorentzTransform(x, ct, beta);
            double boostedS2 = ctPrime * ctPrime - xPrime * xPrime;

            Assert.True(Math.Abs(boostedS2 - originalS2) < 1e-9,
                $"Interval failed invariance under beta={beta}: expected {originalS2}, got {boostedS2}");
        }
    }

    [Fact]
    public void LorentzTransform_RoundtripInversion_RestoresCoordinates()
    {
        double x = -2.8;
        double ct = 5.4;
        double beta = 0.72;

        var (xPrime, ctPrime) = MinkowskiEngine.LorentzTransform(x, ct, beta);
        var (xRestored, ctRestored) = MinkowskiEngine.InverseLorentzTransform(xPrime, ctPrime, beta);

        Assert.True(Math.Abs(xRestored - x) < 1e-9);
        Assert.True(Math.Abs(ctRestored - ct) < 1e-9);
    }

    [Fact]
    public void RelativisticVelocityAddition_PreventsSuperluminalSpeeds()
    {
        // 0.6c + 0.6c in Newtonian is 1.2c, in Einstein is (1.2)/(1 + 0.36) = 0.88235c
        double vCombined = MinkowskiEngine.RelativisticVelocityAddition(0.6, 0.6);
        Assert.True(Math.Abs(vCombined - (1.2 / 1.36)) < 1e-5);
        Assert.True(vCombined < 1.0);

        // Speed of light + anything = speed of light
        double lightCombined = MinkowskiEngine.RelativisticVelocityAddition(1.0, 0.9);
        Assert.True(Math.Abs(lightCombined - 1.0) < 1e-9);
    }

    [Fact]
    public void TwinParadox_DemonstratesAsymmetricAging()
    {
        var engine = new MinkowskiEngine();
        engine.LoadPreset(RelativityPresetType.TwinParadox);

        Assert.Equal(2, engine.Worldlines.Count);
        var earth = engine.Worldlines[0];
        var traveler = engine.Worldlines[1];

        // Earth twin stays at rest for 10 coordinate units
        Assert.Equal(10.0, earth.TotalProperTime, 2);

        // Traveler journeys at 0.8c, so gamma = 1.6667, proper time = 10 / 1.6667 = 6.0
        Assert.True(traveler.TotalProperTime < earth.TotalProperTime);
        Assert.Equal(6.0, traveler.TotalProperTime, 1);
    }

    [Fact]
    public void DopplerFactor_CorrectlyCalculatesRedshiftAndBlueshift()
    {
        // Receding at 0.6c: sqrt((1 - 0.6) / (1 + 0.6)) = sqrt(0.4 / 1.6) = sqrt(0.25) = 0.5 (redshift)
        double redFactor = MinkowskiEngine.DopplerFactor(0.6);
        Assert.Equal(0.5, redFactor, 3);

        // Approaching at -0.6c: sqrt((1 - (-0.6)) / (1 + (-0.6))) = sqrt(1.6 / 0.4) = sqrt(4) = 2.0 (blueshift)
        double blueFactor = MinkowskiEngine.DopplerFactor(-0.6);
        Assert.Equal(2.0, blueFactor, 3);
    }

    [Fact]
    public void AllRelativityPresets_LoadValidWorldlinesAndEvents()
    {
        var engine = new MinkowskiEngine();
        foreach (RelativityPresetType preset in Enum.GetValues<RelativityPresetType>())
        {
            engine.LoadPreset(preset);
            Assert.NotEmpty(engine.Worldlines);
            Assert.NotEmpty(engine.SimultaneityValues);
            Assert.NotEmpty(engine.PositionValues);
        }
    }
}
