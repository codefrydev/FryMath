using System;
using System.Collections.Generic;

namespace FryMath.Core.Celestial;

public sealed record CelestialBody(
    int Id,
    string Name,
    double Mass,
    double Radius,
    double X, double Y, double Z,
    double Vx, double Vy, double Vz,
    string ColorHex
)
{
    public double X { get; set; } = X;
    public double Y { get; set; } = Y;
    public double Z { get; set; } = Z;
    public double Vx { get; set; } = Vx;
    public double Vy { get; set; } = Vy;
    public double Vz { get; set; } = Vz;
    public double Ax { get; set; }
    public double Ay { get; set; }
    public double Az { get; set; }

    public List<(double X, double Y, double Z)> Trail { get; } = new();
}

public enum CelestialPreset
{
    FigureEight,
    LagrangeTrojans,
    PythagoreanChaos,
    SunEarthMoon,
    GalaxyCollision
}

public class CelestialNBodyEngine
{
    public double GravitationalConstant { get; set; } = 1.0;
    public double SofteningLength { get; set; } = 0.05; // Plummer softening: sqrt(r² + ε²)
    public double TimeStep { get; set; } = 0.005;
    public double SimulationTime { get; private set; }
    public CelestialPreset CurrentPreset { get; private set; } = CelestialPreset.FigureEight;

    public List<CelestialBody> Bodies { get; } = new();
    public int MaxTrailPoints { get; set; } = 300;

    // Conservation Invariants
    public double InitialEnergy { get; private set; }
    public double KineticEnergy { get; private set; }
    public double PotentialEnergy { get; private set; }
    public double TotalEnergy => KineticEnergy + PotentialEnergy;
    public double EnergyDrift => InitialEnergy != 0.0 ? Math.Abs((TotalEnergy - InitialEnergy) / InitialEnergy) : 0.0;
    public (double X, double Y, double Z) CenterOfMass { get; private set; }
    public (double X, double Y, double Z) TotalAngularMomentum { get; private set; }

    public CelestialNBodyEngine()
    {
        LoadPreset(CelestialPreset.FigureEight);
    }

    public void LoadPreset(CelestialPreset preset)
    {
        CurrentPreset = preset;
        Bodies.Clear();
        SimulationTime = 0.0;

        switch (preset)
        {
            case CelestialPreset.FigureEight:
                LoadFigureEight();
                break;
            case CelestialPreset.LagrangeTrojans:
                LoadLagrangeTrojans();
                break;
            case CelestialPreset.PythagoreanChaos:
                LoadPythagoreanChaos();
                break;
            case CelestialPreset.SunEarthMoon:
                LoadSunEarthMoon();
                break;
            case CelestialPreset.GalaxyCollision:
                LoadGalaxyCollision();
                break;
        }

        ComputeAccelerations();
        ComputeConservationMetrics();
        InitialEnergy = TotalEnergy;

        // Pre-warm trail points so initial render displays rich, glowing trajectories
        int warmSteps = preset switch
        {
            CelestialPreset.FigureEight => 140,
            CelestialPreset.LagrangeTrojans => 90,
            CelestialPreset.SunEarthMoon => 140,
            CelestialPreset.GalaxyCollision => 60,
            _ => 0
        };

        for (int i = 0; i < warmSteps; i++)
        {
            Step(TimeStep);
        }
    }

    private void LoadFigureEight()
    {
        GravitationalConstant = 1.0;
        SofteningLength = 0.001;
        TimeStep = 0.004;

        // Famous Chenciner-Montgomery Figure-8 choreography
        double x1 = 0.97000436;
        double y1 = -0.24308753;
        double vx1 = 0.46620531;
        double vy1 = 0.43236573;

        var b1 = new CelestialBody(1, "Body 1 (Alpha)", 1.0, 0.08, -x1, -y1, 0, vx1, vy1, 0, "#06b6d4"); // Cyan
        var b2 = new CelestialBody(2, "Body 2 (Beta)", 1.0, 0.08, x1, y1, 0, vx1, vy1, 0, "#f43f5e");  // Rose
        var b3 = new CelestialBody(3, "Body 3 (Gamma)", 1.0, 0.08, 0, 0, 0, -2 * vx1, -2 * vy1, 0, "#fbbf24"); // Amber

        Bodies.Add(b1);
        Bodies.Add(b2);
        Bodies.Add(b3);
    }

    private void LoadLagrangeTrojans()
    {
        GravitationalConstant = 1.0;
        SofteningLength = 0.02;
        TimeStep = 0.005;

        // Primary Star (M1 = 1.0) and Massive Planet (M2 = 0.04) in circular orbit
        double r12 = 2.0;
        double m1 = 1.0;
        double m2 = 0.04;
        double mTotal = m1 + m2;
        double omega = Math.Sqrt(GravitationalConstant * mTotal / (r12 * r12 * r12));

        double r1 = -r12 * (m2 / mTotal);
        double r2 = r12 * (m1 / mTotal);

        var sun = new CelestialBody(1, "Central Star", m1, 0.14, r1, 0, 0, 0, r1 * omega, 0, "#fbbf24"); // Sun
        var jupiter = new CelestialBody(2, "Giant Planet", m2, 0.09, r2, 0, 0, 0, r2 * omega, 0, "#f43f5e"); // Jupiter

        Bodies.Add(sun);
        Bodies.Add(jupiter);

        // L4 and L5 triangular points are at distance r12 forming equilateral triangles
        double l4Angle = Math.PI / 3.0; // 60 degrees ahead
        double l5Angle = -Math.PI / 3.0; // 60 degrees behind

        // Swarm of Trojan asteroids at L4
        var rng = new Random(42);
        for (int i = 0; i < 20; i++)
        {
            double dTheta = (rng.NextDouble() - 0.5) * 0.25;
            double dR = (rng.NextDouble() - 0.5) * 0.15;
            double angle = l4Angle + dTheta;
            double dist = r12 + dR;

            double x = dist * Math.Cos(angle) - r12 * (m2 / mTotal);
            double y = dist * Math.Sin(angle);
            double speed = dist * omega * (1.0 + (rng.NextDouble() - 0.5) * 0.04);

            double vx = -speed * Math.Sin(angle);
            double vy = speed * Math.Cos(angle);

            Bodies.Add(new CelestialBody(10 + i, $"Trojan L4-{i + 1}", 1e-6, 0.03, x, y, 0, vx, vy, 0, "#10b981")); // Emerald
        }

        // Swarm of Greek asteroids at L5
        for (int i = 0; i < 20; i++)
        {
            double dTheta = (rng.NextDouble() - 0.5) * 0.25;
            double dR = (rng.NextDouble() - 0.5) * 0.15;
            double angle = l5Angle + dTheta;
            double dist = r12 + dR;

            double x = dist * Math.Cos(angle) - r12 * (m2 / mTotal);
            double y = dist * Math.Sin(angle);
            double speed = dist * omega * (1.0 + (rng.NextDouble() - 0.5) * 0.04);

            double vx = -speed * Math.Sin(angle);
            double vy = speed * Math.Cos(angle);

            Bodies.Add(new CelestialBody(30 + i, $"Greek L5-{i + 1}", 1e-6, 0.03, x, y, 0, vx, vy, 0, "#a855f7")); // Purple
        }
    }

    private void LoadPythagoreanChaos()
    {
        GravitationalConstant = 1.0;
        SofteningLength = 0.01;
        TimeStep = 0.002;

        // Burrau's problem: 3-4-5 right triangle, initially at rest
        // m1 = 3 at (1, 3), m2 = 4 at (-2, -1), m3 = 5 at (1, -1)
        var b1 = new CelestialBody(1, "Mass 3 (Red)", 3.0, 0.11, 1.0, 3.0, 0, 0, 0, 0, "#f43f5e");
        var b2 = new CelestialBody(2, "Mass 4 (Green)", 4.0, 0.12, -2.0, -1.0, 0, 0, 0, 0, "#10b981");
        var b3 = new CelestialBody(3, "Mass 5 (Cyan)", 5.0, 0.13, 1.0, -1.0, 0, 0, 0, 0, "#06b6d4");

        Bodies.Add(b1);
        Bodies.Add(b2);
        Bodies.Add(b3);

        CenterSystem();
    }

    private void LoadSunEarthMoon()
    {
        GravitationalConstant = 1.0;
        SofteningLength = 0.005;
        TimeStep = 0.002;

        double mSun = 10.0;
        double mEarth = 0.01;
        double mMoon = 0.0001;

        double rEarth = 2.5;
        double vEarth = Math.Sqrt(GravitationalConstant * mSun / rEarth);

        double rMoon = 0.22;
        double vMoon = Math.Sqrt(GravitationalConstant * mEarth / rMoon);

        var sun = new CelestialBody(1, "Sun", mSun, 0.22, 0, 0, 0, 0, 0, 0, "#fbbf24");
        var earth = new CelestialBody(2, "Earth", mEarth, 0.08, rEarth, 0, 0, 0, vEarth, 0, "#06b6d4");
        var moon = new CelestialBody(3, "Moon", mMoon, 0.04, rEarth + rMoon, 0, 0, 0, vEarth + vMoon, 0, "#e2e8f0");

        Bodies.Add(sun);
        Bodies.Add(earth);
        Bodies.Add(moon);

        CenterSystem();
    }

    private void LoadGalaxyCollision()
    {
        GravitationalConstant = 1.0;
        SofteningLength = 0.05;
        TimeStep = 0.005;

        // Core 1: Approaching from left
        double mCore1 = 5.0;
        double mCore2 = 5.0;

        var core1 = new CelestialBody(1, "Galactic Core Alpha", mCore1, 0.16, -2.0, -0.6, 0.2, 0.45, 0.15, 0.0, "#06b6d4");
        var core2 = new CelestialBody(2, "Galactic Core Beta", mCore2, 0.16, 2.0, 0.6, -0.2, -0.45, -0.15, 0.0, "#f43f5e");

        Bodies.Add(core1);
        Bodies.Add(core2);

        // Ring stars orbiting Core 1
        var rng = new Random(77);
        int starsPerGalaxy = 45;
        for (int i = 0; i < starsPerGalaxy; i++)
        {
            double r = 0.4 + (rng.NextDouble() * 0.8);
            double theta = rng.NextDouble() * 2 * Math.PI;
            double vOrb = Math.Sqrt(GravitationalConstant * mCore1 / r);

            double x = core1.X + r * Math.Cos(theta);
            double y = core1.Y + r * Math.Sin(theta);
            double z = core1.Z + (rng.NextDouble() - 0.5) * 0.05;

            double vx = core1.Vx - vOrb * Math.Sin(theta);
            double vy = core1.Vy + vOrb * Math.Cos(theta);
            double vz = core1.Vz;

            Bodies.Add(new CelestialBody(100 + i, $"Star A-{i}", 1e-7, 0.02, x, y, z, vx, vy, vz, "#67e8f9"));
        }

        // Ring stars orbiting Core 2 (inclined plane)
        for (int i = 0; i < starsPerGalaxy; i++)
        {
            double r = 0.4 + (rng.NextDouble() * 0.8);
            double theta = rng.NextDouble() * 2 * Math.PI;
            double vOrb = Math.Sqrt(GravitationalConstant * mCore2 / r);

            double localX = r * Math.Cos(theta);
            double localY = r * Math.Sin(theta);

            // Incline disk by 45 degrees around X
            double x = core2.X + localX;
            double y = core2.Y + localY * 0.7071;
            double z = core2.Z + localY * 0.7071 + (rng.NextDouble() - 0.5) * 0.05;

            double localVx = -vOrb * Math.Sin(theta);
            double localVy = vOrb * Math.Cos(theta);

            double vx = core2.Vx + localVx;
            double vy = core2.Vy + localVy * 0.7071;
            double vz = core2.Vz + localVy * 0.7071;

            Bodies.Add(new CelestialBody(200 + i, $"Star B-{i}", 1e-7, 0.02, x, y, z, vx, vy, vz, "#fda4af"));
        }
    }

    private void CenterSystem()
    {
        double totalMass = 0;
        double cx = 0, cy = 0, cz = 0;
        double cvx = 0, cvy = 0, cvz = 0;

        foreach (var b in Bodies)
        {
            totalMass += b.Mass;
            cx += b.Mass * b.X;
            cy += b.Mass * b.Y;
            cz += b.Mass * b.Z;
            cvx += b.Mass * b.Vx;
            cvy += b.Mass * b.Vy;
            cvz += b.Mass * b.Vz;
        }

        if (totalMass > 0)
        {
            cx /= totalMass; cy /= totalMass; cz /= totalMass;
            cvx /= totalMass; cvy /= totalMass; cvz /= totalMass;

            foreach (var b in Bodies)
            {
                b.X -= cx; b.Y -= cy; b.Z -= cz;
                b.Vx -= cvx; b.Vy -= cvy; b.Vz -= cvz;
            }
        }
    }

    public void Step(double dt)
    {
        if (dt <= 0) return;

        // Symplectic Störmer-Verlet (Leapfrog) Integrator
        // 1. Kick: v(t + dt/2) = v(t) + a(t) * dt/2
        double halfDt = 0.5 * dt;
        for (int i = 0; i < Bodies.Count; i++)
        {
            var b = Bodies[i];
            b.Vx += b.Ax * halfDt;
            b.Vy += b.Ay * halfDt;
            b.Vz += b.Az * halfDt;
        }

        // 2. Drift: r(t + dt) = r(t) + v(t + dt/2) * dt
        for (int i = 0; i < Bodies.Count; i++)
        {
            var b = Bodies[i];
            b.X += b.Vx * dt;
            b.Y += b.Vy * dt;
            b.Z += b.Vz * dt;

            // Record trail
            b.Trail.Add((b.X, b.Y, b.Z));
            if (b.Trail.Count > MaxTrailPoints)
            {
                b.Trail.RemoveAt(0);
            }
        }

        // 3. Compute new accelerations: a(t + dt)
        ComputeAccelerations();

        // 4. Kick: v(t + dt) = v(t + dt/2) + a(t + dt) * dt/2
        for (int i = 0; i < Bodies.Count; i++)
        {
            var b = Bodies[i];
            b.Vx += b.Ax * halfDt;
            b.Vy += b.Ay * halfDt;
            b.Vz += b.Az * halfDt;
        }

        SimulationTime += dt;
        ComputeConservationMetrics();
    }

    private void ComputeAccelerations()
    {
        // Reset accelerations
        for (int i = 0; i < Bodies.Count; i++)
        {
            Bodies[i].Ax = 0;
            Bodies[i].Ay = 0;
            Bodies[i].Az = 0;
        }

        double epsSq = SofteningLength * SofteningLength;

        // Pairwise gravitational acceleration: a_i = sum_j G*m_j*(r_j - r_i)/(r_ij² + eps²)^(3/2)
        for (int i = 0; i < Bodies.Count; i++)
        {
            var b1 = Bodies[i];
            for (int j = i + 1; j < Bodies.Count; j++)
            {
                var b2 = Bodies[j];

                double dx = b2.X - b1.X;
                double dy = b2.Y - b1.Y;
                double dz = b2.Z - b1.Z;

                double rSq = dx * dx + dy * dy + dz * dz + epsSq;
                double invDist3 = 1.0 / (rSq * Math.Sqrt(rSq));

                double f12 = GravitationalConstant * invDist3;

                // Newton's 3rd Law: action and reaction
                if (b2.Mass > 0)
                {
                    double factor1 = f12 * b2.Mass;
                    b1.Ax += factor1 * dx;
                    b1.Ay += factor1 * dy;
                    b1.Az += factor1 * dz;
                }

                if (b1.Mass > 0)
                {
                    double factor2 = f12 * b1.Mass;
                    b2.Ax -= factor2 * dx;
                    b2.Ay -= factor2 * dy;
                    b2.Az -= factor2 * dz;
                }
            }
        }
    }

    private void ComputeConservationMetrics()
    {
        double ke = 0;
        double pe = 0;
        double totalMass = 0;
        double cmX = 0, cmY = 0, cmZ = 0;
        double lx = 0, ly = 0, lz = 0;

        double epsSq = SofteningLength * SofteningLength;

        for (int i = 0; i < Bodies.Count; i++)
        {
            var b = Bodies[i];
            double vSq = b.Vx * b.Vx + b.Vy * b.Vy + b.Vz * b.Vz;
            ke += 0.5 * b.Mass * vSq;

            totalMass += b.Mass;
            cmX += b.Mass * b.X;
            cmY += b.Mass * b.Y;
            cmZ += b.Mass * b.Z;

            // Angular momentum L = m * (r x v)
            lx += b.Mass * (b.Y * b.Vz - b.Z * b.Vy);
            ly += b.Mass * (b.Z * b.Vx - b.X * b.Vz);
            lz += b.Mass * (b.X * b.Vy - b.Y * b.Vx);

            for (int j = i + 1; j < Bodies.Count; j++)
            {
                var b2 = Bodies[j];
                double dx = b2.X - b.X;
                double dy = b2.Y - b.Y;
                double dz = b2.Z - b.Z;
                double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz + epsSq);

                if (dist > 1e-12)
                {
                    pe -= (GravitationalConstant * b.Mass * b2.Mass) / dist;
                }
            }
        }

        KineticEnergy = ke;
        PotentialEnergy = pe;
        if (totalMass > 0)
        {
            CenterOfMass = (cmX / totalMass, cmY / totalMass, cmZ / totalMass);
        }
        TotalAngularMomentum = (lx, ly, lz);
    }

    public double EvaluatePotentialAt(double x, double y, double z)
    {
        double phi = 0;
        double epsSq = SofteningLength * SofteningLength;

        for (int i = 0; i < Bodies.Count; i++)
        {
            var b = Bodies[i];
            if (b.Mass <= 1e-5) continue; // Skip test particles for potential grid

            double dx = x - b.X;
            double dy = y - b.Y;
            double dz = z - b.Z;
            double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz + epsSq);
            phi -= (GravitationalConstant * b.Mass) / dist;
        }

        return phi;
    }
}
