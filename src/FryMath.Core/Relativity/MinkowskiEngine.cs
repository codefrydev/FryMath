using System;
using System.Collections.Generic;

namespace FryMath.Core.Relativity;

public enum SpacetimeCausality
{
    Timelike,
    Spacelike,
    Lightlike
}

public enum RelativityPresetType
{
    StandardBoost,
    TwinParadox,
    MuonDecay,
    BellSpaceships,
    DopplerShift
}

public record SpacetimeEvent(double X, double Ct, string Label = "")
{
    public double IntervalSquared => Ct * Ct - X * X;

    public SpacetimeCausality Causality
    {
        get
        {
            double s2 = IntervalSquared;
            if (Math.Abs(s2) < 1e-7) return SpacetimeCausality.Lightlike;
            return s2 > 0 ? SpacetimeCausality.Timelike : SpacetimeCausality.Spacelike;
        }
    }

    public (double XPrime, double CtPrime) Transform(double beta)
    {
        double gamma = 1.0 / Math.Sqrt(Math.Max(1e-6, 1.0 - beta * beta));
        double xPrime = gamma * (X - beta * Ct);
        double ctPrime = gamma * (Ct - beta * X);
        return (xPrime, ctPrime);
    }
}

public class Worldline
{
    public string Name { get; set; } = "";
    public string ColorHex { get; set; } = "#38bdf8";
    public bool IsDashed { get; set; }
    public List<SpacetimeEvent> Points { get; set; } = [];
    public double TotalProperTime { get; set; }
}

public class MinkowskiEngine
{
    private double _beta = 0.5;

    public double Beta
    {
        get => _beta;
        set => _beta = Math.Clamp(value, -0.99, 0.99);
    }

    public double Gamma => 1.0 / Math.Sqrt(Math.Max(1e-6, 1.0 - _beta * _beta));

    public RelativityPresetType CurrentPreset { get; private set; } = RelativityPresetType.StandardBoost;

    public List<Worldline> Worldlines { get; private set; } = [];
    public List<SpacetimeEvent> KeyEvents { get; private set; } = [];
    public List<double> SimultaneityValues { get; private set; } = [-4, -2, 0, 2, 4];
    public List<double> PositionValues { get; private set; } = [-4, -2, 0, 2, 4];

    public double SimTime { get; set; } = 0.0;
    public bool IsPlaying { get; set; } = true;

    public MinkowskiEngine()
    {
        LoadPreset(RelativityPresetType.StandardBoost);
    }

    public static (double XPrime, double CtPrime) LorentzTransform(double x, double ct, double beta)
    {
        double b = Math.Clamp(beta, -0.999, 0.999);
        double g = 1.0 / Math.Sqrt(Math.Max(1e-6, 1.0 - b * b));
        return (g * (x - b * ct), g * (ct - b * x));
    }

    public static (double X, double Ct) InverseLorentzTransform(double xPrime, double ctPrime, double beta)
    {
        double b = Math.Clamp(beta, -0.999, 0.999);
        double g = 1.0 / Math.Sqrt(Math.Max(1e-6, 1.0 - b * b));
        return (g * (xPrime + b * ctPrime), g * (ctPrime + b * xPrime));
    }

    public static double SpacetimeInterval(double x1, double ct1, double x2, double ct2)
    {
        double dx = x2 - x1;
        double dct = ct2 - ct1;
        return dct * dct - dx * dx;
    }

    public static double RelativisticVelocityAddition(double u, double v)
    {
        // (u + v) / (1 + uv/c^2) with c = 1
        double denom = 1.0 + u * v;
        if (Math.Abs(denom) < 1e-9) return 1.0;
        return (u + v) / denom;
    }

    public static double DopplerFactor(double beta)
    {
        // Receding (+) or approaching (-): f_obs = f_src * sqrt((1 - beta)/(1 + beta))
        double b = Math.Clamp(beta, -0.999, 0.999);
        return Math.Sqrt((1.0 - b) / (1.0 + b));
    }

    public void LoadPreset(RelativityPresetType preset)
    {
        CurrentPreset = preset;
        Worldlines.Clear();
        KeyEvents.Clear();

        switch (preset)
        {
            case RelativityPresetType.StandardBoost:
                ConfigureStandardBoost();
                break;
            case RelativityPresetType.TwinParadox:
                ConfigureTwinParadox();
                break;
            case RelativityPresetType.MuonDecay:
                ConfigureMuonDecay();
                break;
            case RelativityPresetType.BellSpaceships:
                ConfigureBellSpaceships();
                break;
            case RelativityPresetType.DopplerShift:
                ConfigureDopplerShift();
                break;
        }
    }

    private void ConfigureStandardBoost()
    {
        _beta = 0.6;
        SimultaneityValues = [-6, -4, -2, 0, 2, 4, 6];
        PositionValues = [-6, -4, -2, 0, 2, 4, 6];

        // Light cone worldlines
        var lightForward = new Worldline { Name = "Photon (+c)", ColorHex = "#fbbf24", IsDashed = true };
        for (double t = -7; t <= 7; t += 0.5)
            lightForward.Points.Add(new SpacetimeEvent(t, t));
        Worldlines.Add(lightForward);

        var lightBackward = new Worldline { Name = "Photon (-c)", ColorHex = "#fbbf24", IsDashed = true };
        for (double t = -7; t <= 7; t += 0.5)
            lightBackward.Points.Add(new SpacetimeEvent(-t, t));
        Worldlines.Add(lightBackward);

        // Rocket moving at v = 0.6c
        var rocket = new Worldline { Name = "Rocket (0.6c)", ColorHex = "#38bdf8" };
        for (double t = -6; t <= 6; t += 0.2)
            rocket.Points.Add(new SpacetimeEvent(_beta * t, t));
        rocket.TotalProperTime = 12.0 * Math.Sqrt(1.0 - _beta * _beta);
        Worldlines.Add(rocket);

        // Events
        KeyEvents.Add(new SpacetimeEvent(0, 0, "Origin (O)"));
        KeyEvents.Add(new SpacetimeEvent(3, 4, "Event A (Timelike)"));
        KeyEvents.Add(new SpacetimeEvent(4, 2, "Event B (Spacelike)"));
        KeyEvents.Add(new SpacetimeEvent(3, 3, "Event C (Lightlike)"));
    }

    private void ConfigureTwinParadox()
    {
        _beta = 0.8;
        double T = 5.0; // Turnaround coordinate time in Earth frame
        double gamma = 1.0 / Math.Sqrt(1.0 - _beta * _beta); // = 1.6667

        SimultaneityValues = [0, 2, 4, 6, 8, 10];
        PositionValues = [-2, 0, 2, 4, 6];

        // Earth Twin: stays at x = 0 from t = 0 to 2*T = 10
        var earthTwin = new Worldline { Name = "Earth Twin (Stay-at-Home)", ColorHex = "#34d399" };
        for (double t = 0; t <= 2 * T; t += 0.25)
            earthTwin.Points.Add(new SpacetimeEvent(0, t));
        earthTwin.TotalProperTime = 2 * T;
        Worldlines.Add(earthTwin);

        // Traveling Twin: journeys to (beta * T, T) then returns to (0, 2*T)
        var travelingTwin = new Worldline { Name = "Traveling Twin (Round-Trip)", ColorHex = "#f43f5e" };
        // Outbound
        for (double t = 0; t <= T; t += 0.2)
            travelingTwin.Points.Add(new SpacetimeEvent(_beta * t, t));
        // Inbound
        for (double t = T; t <= 2 * T; t += 0.2)
        {
            double fraction = (t - T) / T;
            double x = _beta * T * (1.0 - fraction);
            travelingTwin.Points.Add(new SpacetimeEvent(x, t));
        }
        // Total proper time tau = 2 * T / gamma = 10 / 1.6667 = 6.0
        travelingTwin.TotalProperTime = 2 * T / gamma;
        Worldlines.Add(travelingTwin);

        // Key Events
        KeyEvents.Add(new SpacetimeEvent(0, 0, "Departure (t=0, tau=0)"));
        KeyEvents.Add(new SpacetimeEvent(_beta * T, T, $"Turnaround (t={T:F1}, tau={T / gamma:F1})"));
        KeyEvents.Add(new SpacetimeEvent(0, 2 * T, $"Reunion (Earth=10, Traveler={travelingTwin.TotalProperTime:F1})"));
    }

    private void ConfigureMuonDecay()
    {
        _beta = 0.98;
        double gamma = 1.0 / Math.Sqrt(1.0 - _beta * _beta); // approx 5.025

        SimultaneityValues = [0, 2, 4, 6, 8, 10];
        PositionValues = [-2, 0, 2, 4, 6];

        // Earth surface at x = 0
        var earthSurface = new Worldline { Name = "Earth Surface (x = 0)", ColorHex = "#34d399" };
        for (double t = 0; t <= 10; t += 0.5)
            earthSurface.Points.Add(new SpacetimeEvent(0, t));
        Worldlines.Add(earthSurface);

        // Atmosphere top at x = 8
        var atmosphereTop = new Worldline { Name = "Upper Atmosphere (10 km)", ColorHex = "#64748b", IsDashed = true };
        for (double t = 0; t <= 10; t += 0.5)
            atmosphereTop.Points.Add(new SpacetimeEvent(8, t));
        Worldlines.Add(atmosphereTop);

        // Relativistic Muon descending toward Earth
        var muon = new Worldline { Name = "Relativistic Muon (0.98c)", ColorHex = "#38bdf8" };
        double arrivalTime = 8.0 / _beta; // approx 8.16
        for (double t = 0; t <= arrivalTime; t += 0.2)
        {
            double x = 8.0 - _beta * t;
            muon.Points.Add(new SpacetimeEvent(x, t));
        }
        muon.TotalProperTime = arrivalTime / gamma; // approx 1.62
        Worldlines.Add(muon);

        // Classical muon range limit (no dilation)
        double classicalLifetime = 1.6;
        double classicalDistance = 8.0 - _beta * classicalLifetime;
        KeyEvents.Add(new SpacetimeEvent(8, 0, "Muon Creation (Top of Atm)"));
        KeyEvents.Add(new SpacetimeEvent(classicalDistance, classicalLifetime, "Classical Decay Point (Altitude 6.4km)"));
        KeyEvents.Add(new SpacetimeEvent(0, arrivalTime, "Actual Sea-Level Detection!"));
    }

    private void ConfigureBellSpaceships()
    {
        _beta = 0.7;
        SimultaneityValues = [0, 2, 4, 6, 8];
        PositionValues = [0, 2, 4, 6, 8];

        // Spaceship A and B separated by constant initial distance L = 3.0 in ground frame
        // Both accelerate identically: x_A(t) = speed * t
        double L = 3.0;
        var shipA = new Worldline { Name = "Spaceship Lead (B)", ColorHex = "#38bdf8" };
        var shipB = new Worldline { Name = "Spaceship Trail (A)", ColorHex = "#a855f7" };

        for (double t = 0; t <= 8; t += 0.2)
        {
            double speed = _beta * Math.Min(1.0, t / 4.0);
            double xTrail = speed * t;
            double xLead = xTrail + L; // Distance remains L in ground frame!
            shipB.Points.Add(new SpacetimeEvent(xTrail, t));
            shipA.Points.Add(new SpacetimeEvent(xLead, t));
        }
        Worldlines.Add(shipB);
        Worldlines.Add(shipA);

        KeyEvents.Add(new SpacetimeEvent(0, 0, "Ship A Launch"));
        KeyEvents.Add(new SpacetimeEvent(L, 0, "Ship B Launch"));
        KeyEvents.Add(new SpacetimeEvent(L * 1.5, 4.0, "String Snaps! (Proper distance > L)"));
    }

    private void ConfigureDopplerShift()
    {
        _beta = 0.6;
        SimultaneityValues = [0, 2, 4, 6, 8, 10];
        PositionValues = [-6, -4, -2, 0, 2, 4, 6];

        // Source moving at +0.6c
        var source = new Worldline { Name = "Receding Source (0.6c)", ColorHex = "#f59e0b" };
        for (double t = 0; t <= 8; t += 0.2)
            source.Points.Add(new SpacetimeEvent(_beta * t, t));
        Worldlines.Add(source);

        // Flashes emitted at regular proper intervals
        double gamma = 1.0 / Math.Sqrt(1.0 - _beta * _beta);
        for (int n = 1; n <= 4; n++)
        {
            double tEmit = n * 1.5 * gamma;
            double xEmit = _beta * tEmit;

            // Flash toward left (receding observer: redshifted)
            var pulseLeft = new Worldline { Name = $"Redshift Flash {n}", ColorHex = "#ef4444", IsDashed = true };
            for (double s = 0; s <= 6; s += 0.5)
            {
                pulseLeft.Points.Add(new SpacetimeEvent(xEmit - s, tEmit + s));
            }
            Worldlines.Add(pulseLeft);

            // Flash toward right (approaching observer: blueshifted)
            var pulseRight = new Worldline { Name = $"Blueshift Flash {n}", ColorHex = "#06b6d4", IsDashed = true };
            for (double s = 0; s <= 6; s += 0.5)
            {
                pulseRight.Points.Add(new SpacetimeEvent(xEmit + s, tEmit + s));
            }
            Worldlines.Add(pulseRight);
        }
    }

    public List<(double X, double Ct)> GetHyperbolaPoints(double sSquared, double maxCoord = 8.0)
    {
        var pts = new List<(double X, double Ct)>();
        if (sSquared > 0)
        {
            // Timelike: ct^2 - x^2 = s^2  =>  ct = sqrt(s^2 + x^2)
            double s = Math.Sqrt(sSquared);
            for (double x = -maxCoord; x <= maxCoord; x += 0.2)
            {
                double ct = Math.Sqrt(s * s + x * x);
                pts.Add((x, ct));
            }
        }
        else if (sSquared < 0)
        {
            // Spacelike: x^2 - ct^2 = -s^2  =>  x = sqrt(-s^2 + ct^2)
            double sigma = Math.Sqrt(-sSquared);
            for (double ct = -maxCoord; ct <= maxCoord; ct += 0.2)
            {
                double x = Math.Sqrt(sigma * sigma + ct * ct);
                pts.Add((x, ct));
            }
        }
        return pts;
    }

    public void Step(double dt = 0.05)
    {
        if (!IsPlaying) return;
        SimTime += dt;
        if (SimTime > 10.0) SimTime = 0.0;
    }
}
