using System;
using System.Collections.Generic;

namespace FryMath.Core.Fluids;

public enum FluidPresetType
{
    VonKarman,
    VortexDipole,
    KelvinHelmholtz,
    TaylorGreen,
    LidDrivenCavity
}

public class FluidParticle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Age { get; set; }
    public double MaxAge { get; set; }
    public string ColorHex { get; set; } = "#38bdf8";

    public FluidParticle(double x, double y, double maxAge = 8.0)
    {
        X = x;
        Y = y;
        Age = 0;
        MaxAge = maxAge;
    }
}

public class NavierStokesEngine
{
    public int Width { get; }
    public int Height { get; }

    // Physical fields
    public double[] Omega { get; private set; } // Vorticity
    public double[] Omega0 { get; private set; } // Temporary vorticity
    public double[] Psi { get; private set; }   // Streamfunction
    public double[] U { get; private set; }     // Horizontal velocity
    public double[] V { get; private set; }     // Vertical velocity
    public bool[] Obstacles { get; private set; } // Solid boundary mask

    public double Viscosity { get; set; } = 0.0008; // Kinematic viscosity nu
    public double ReynoldsNumber => 1.0 / Math.Max(1e-5, Viscosity);

    public FluidPresetType CurrentPreset { get; private set; } = FluidPresetType.VonKarman;
    public List<FluidParticle> Particles { get; } = [];

    private readonly Random _rand = new(42);

    public NavierStokesEngine(int width = 110, int height = 70)
    {
        Width = width;
        Height = height;

        int size = width * height;
        Omega = new double[size];
        Omega0 = new double[size];
        Psi = new double[size];
        U = new double[size];
        V = new double[size];
        Obstacles = new bool[size];

        LoadPreset(FluidPresetType.VonKarman);
    }

    public void LoadPreset(FluidPresetType preset)
    {
        CurrentPreset = preset;
        Array.Clear(Omega, 0, Omega.Length);
        Array.Clear(Omega0, 0, Omega0.Length);
        Array.Clear(Psi, 0, Psi.Length);
        Array.Clear(U, 0, U.Length);
        Array.Clear(V, 0, V.Length);
        Array.Clear(Obstacles, 0, Obstacles.Length);
        Particles.Clear();

        switch (preset)
        {
            case FluidPresetType.VonKarman:
                ConfigureVonKarman();
                break;
            case FluidPresetType.VortexDipole:
                ConfigureVortexDipole();
                break;
            case FluidPresetType.KelvinHelmholtz:
                ConfigureKelvinHelmholtz();
                break;
            case FluidPresetType.TaylorGreen:
                ConfigureTaylorGreen();
                break;
            case FluidPresetType.LidDrivenCavity:
                ConfigureLidDrivenCavity();
                break;
        }

        SeedParticles(350);
    }

    private void ConfigureVonKarman()
    {
        Viscosity = 0.0006;
        // Circular cylinder obstacle at (Width * 0.22, Height * 0.5) with radius 7
        double cx = Width * 0.22;
        double cy = Height * 0.5;
        double radius = 7.0;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                double dx = x - cx;
                double dy = y - cy;
                if (dx * dx + dy * dy <= radius * radius)
                {
                    Obstacles[y * Width + x] = true;
                }
                // Ambient inflow from left to right
                U[y * Width + x] = 1.0;
            }
        }

        // Pre-seed alternating vortex street downstream
        for (int k = 0; k < 4; k++)
        {
            double vx = cx + radius + 10.0 + k * 16.0;
            double vy = cy + (k % 2 == 0 ? 6.0 : -6.0);
            double strength = (k % 2 == 0 ? 2.2 : -2.2);
            double r0 = 5.0;

            for (int y = 0; y < Height; y++)
            {
                for (int x = (int)(cx + radius + 1); x < Width; x++)
                {
                    double d2 = (x - vx) * (x - vx) + (y - vy) * (y - vy);
                    Omega[y * Width + x] += strength * Math.Exp(-d2 / (2 * r0 * r0));
                }
            }
        }

        // Pre-relax streamfunction
        SolvePoissonSOR(25, 1.4);
        ComputeVelocityFromStreamfunction();
    }

    private void ConfigureVortexDipole()
    {
        Viscosity = 0.0004;
        // Two counter-rotating Gaussian vortex cores propagating forward
        double cx = Width * 0.35;
        double cy1 = Height * 0.42;
        double cy2 = Height * 0.58;
        double r0 = 4.5;
        double strength = 2.5;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                double d1 = (x - cx) * (x - cx) + (y - cy1) * (y - cy1);
                double d2 = (x - cx) * (x - cx) + (y - cy2) * (y - cy2);
                Omega[y * Width + x] = strength * Math.Exp(-d1 / (2 * r0 * r0)) -
                                      strength * Math.Exp(-d2 / (2 * r0 * r0));
            }
        }

        SolvePoissonSOR(25, 1.4);
        ComputeVelocityFromStreamfunction();
    }

    private void ConfigureKelvinHelmholtz()
    {
        Viscosity = 0.0003;
        // Shear layer with opposite velocities above and below midline
        double midY = Height * 0.5;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                double dy = y - midY;
                double shearU = Math.Tanh(dy / 3.0);
                U[y * Width + x] = shearU;

                // Sinusoidal vertical displacement perturbation
                double pert = 0.15 * Math.Sin(2.0 * Math.PI * x / (Width * 0.5)) * Math.Exp(-dy * dy / 16.0);
                V[y * Width + x] = pert;
                Omega[y * Width + x] = -(1.0 - shearU * shearU) / 3.0; // curl of tanh profile
            }
        }
    }

    private void ConfigureTaylorGreen()
    {
        Viscosity = 0.0008;
        // 2x2 vortex lattice
        double kx = 2.0 * Math.PI / Width;
        double ky = 2.0 * Math.PI / Height;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                U[y * Width + x] = Math.Cos(kx * x) * Math.Sin(ky * y);
                V[y * Width + x] = -Math.Sin(kx * x) * Math.Cos(ky * y);
                Omega[y * Width + x] = 2.0 * kx * Math.Cos(kx * x) * Math.Cos(ky * y);
            }
        }
    }

    private void ConfigureLidDrivenCavity()
    {
        Viscosity = 0.001;
        // Solid wall boundaries on bottom, left, and right
        for (int x = 0; x < Width; x++)
        {
            Obstacles[x] = true; // Bottom wall
            U[(Height - 1) * Width + x] = 1.2; // Moving lid top
        }
        for (int y = 0; y < Height; y++)
        {
            Obstacles[y * Width] = true; // Left wall
            Obstacles[y * Width + (Width - 1)] = true; // Right wall
        }
    }

    public void SeedParticles(int count)
    {
        for (int i = 0; i < count; i++)
        {
            double px = _rand.NextDouble() * (Width - 2) + 1;
            double py = _rand.NextDouble() * (Height - 2) + 1;
            Particles.Add(new FluidParticle(px, py, 6.0 + _rand.NextDouble() * 4.0));
        }
    }

    public void Step(double dt = 0.4)
    {
        // 1. Solve Poisson Equation: nabla^2 psi = -omega via Successive Over-Relaxation (SOR)
        SolvePoissonSOR(20, 1.4);

        // 2. Compute Velocity from Streamfunction: u = d_psi/dy, v = -d_psi/dx
        ComputeVelocityFromStreamfunction();

        // 3. Advect Vorticity: d_omega/dt + (u . nabla)omega = 0 (Semi-Lagrangian)
        AdvectVorticity(dt);

        // 4. Diffuse Vorticity: d_omega/dt = nu nabla^2 omega
        DiffuseVorticity(dt);

        // 5. Inflow / Outflow & Obstacle boundary conditions
        EnforceBoundaryConditions();

        // 6. Advect dye tracer particles
        AdvectParticles(dt);
    }

    private void SolvePoissonSOR(int iterations, double omegaSOR)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            for (int y = 1; y < Height - 1; y++)
            {
                int row = y * Width;
                for (int x = 1; x < Width - 1; x++)
                {
                    int idx = row + x;
                    if (Obstacles[idx]) continue;

                    double residual = 0.25 * (Psi[idx - 1] + Psi[idx + 1] +
                                              Psi[idx - Width] + Psi[idx + Width] +
                                              Omega[idx]) - Psi[idx];
                    Psi[idx] += omegaSOR * residual;
                }
            }
        }
    }

    private void ComputeVelocityFromStreamfunction()
    {
        for (int y = 1; y < Height - 1; y++)
        {
            int row = y * Width;
            for (int x = 1; x < Width - 1; x++)
            {
                int idx = row + x;
                if (Obstacles[idx])
                {
                    U[idx] = 0;
                    V[idx] = 0;
                    continue;
                }

                // u = d_psi/dy, v = -d_psi/dx (central differences)
                U[idx] = 0.5 * (Psi[idx + Width] - Psi[idx - Width]);
                V[idx] = -0.5 * (Psi[idx + 1] - Psi[idx - 1]);
            }
        }
    }

    private void AdvectVorticity(double dt)
    {
        Array.Copy(Omega, Omega0, Omega.Length);

        for (int y = 1; y < Height - 1; y++)
        {
            int row = y * Width;
            for (int x = 1; x < Width - 1; x++)
            {
                int idx = row + x;
                if (Obstacles[idx]) continue;

                // Backtrace position
                double backX = x - U[idx] * dt;
                double backY = y - V[idx] * dt;

                // Bilinear sample from Omega0
                Omega[idx] = SampleBilinear(Omega0, backX, backY);
            }
        }
    }

    private void DiffuseVorticity(double dt)
    {
        double alpha = Math.Clamp(Viscosity * dt, 0.0, 0.2);
        if (alpha < 1e-6) return;

        Array.Copy(Omega, Omega0, Omega.Length);

        for (int y = 1; y < Height - 1; y++)
        {
            int row = y * Width;
            for (int x = 1; x < Width - 1; x++)
            {
                int idx = row + x;
                if (Obstacles[idx]) continue;

                double laplacian = Omega0[idx - 1] + Omega0[idx + 1] +
                                   Omega0[idx - Width] + Omega0[idx + Width] -
                                   4.0 * Omega0[idx];
                Omega[idx] += alpha * laplacian;
            }
        }
    }

    private void EnforceBoundaryConditions()
    {
        if (CurrentPreset == FluidPresetType.VonKarman)
        {
            // Maintain continuous left inflow
            for (int y = 0; y < Height; y++)
            {
                U[y * Width] = 1.0;
                V[y * Width] = 0.0;
                Omega[y * Width] = 0.0;
            }
            // Zero-gradient outflow on right
            for (int y = 0; y < Height; y++)
            {
                int rIdx = y * Width + (Width - 1);
                Omega[rIdx] = Omega[rIdx - 1];
            }
        }

        // Zero out vorticity & velocity inside obstacles
        for (int i = 0; i < Obstacles.Length; i++)
        {
            if (Obstacles[i])
            {
                Omega[i] = 0;
                U[i] = 0;
                V[i] = 0;
            }
        }
    }

    private void AdvectParticles(double dt)
    {
        for (int i = 0; i < Particles.Count; i++)
        {
            var p = Particles[i];
            p.Age += 0.04;

            // Sample local velocity
            double u = SampleBilinear(U, p.X, p.Y);
            double v = SampleBilinear(V, p.X, p.Y);

            p.X += u * dt;
            p.Y += v * dt;

            // Respawn if expired, left the domain, or entered an obstacle
            int gx = (int)Math.Clamp(p.X, 0, Width - 1);
            int gy = (int)Math.Clamp(p.Y, 0, Height - 1);

            if (p.Age >= p.MaxAge || p.X < 1 || p.X >= Width - 1 ||
                p.Y < 1 || p.Y >= Height - 1 || Obstacles[gy * Width + gx])
            {
                if (CurrentPreset == FluidPresetType.VonKarman)
                {
                    // Inflow seeding on left edge
                    p.X = 1.5;
                    p.Y = 2.0 + _rand.NextDouble() * (Height - 4.0);
                }
                else
                {
                    p.X = 2.0 + _rand.NextDouble() * (Width - 4.0);
                    p.Y = 2.0 + _rand.NextDouble() * (Height - 4.0);
                }
                p.Age = 0;
            }
        }
    }

    private double SampleBilinear(double[] field, double x, double y)
    {
        x = Math.Clamp(x, 0.0, Width - 1.001);
        y = Math.Clamp(y, 0.0, Height - 1.001);

        int x0 = (int)x;
        int y0 = (int)y;
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        double fx = x - x0;
        double fy = y - y0;

        double v00 = field[y0 * Width + x0];
        double v10 = field[y0 * Width + x1];
        double v01 = field[y1 * Width + x0];
        double v11 = field[y1 * Width + x1];

        return (1.0 - fx) * (1.0 - fy) * v00 +
               fx * (1.0 - fy) * v10 +
               (1.0 - fx) * fy * v01 +
               fx * fy * v11;
    }

    public void InjectVorticity(double x, double y, double amount, double radius = 4.0)
    {
        int minX = (int)Math.Max(1, x - radius);
        int maxX = (int)Math.Min(Width - 2, x + radius);
        int minY = (int)Math.Max(1, y - radius);
        int maxY = (int)Math.Min(Height - 2, y + radius);

        for (int py = minY; py <= maxY; py++)
        {
            for (int px = minX; px <= maxX; px++)
            {
                double d2 = (px - x) * (px - x) + (py - y) * (py - y);
                if (d2 <= radius * radius)
                {
                    double w = Math.Exp(-d2 / (radius * radius * 0.5));
                    Omega[py * Width + px] += amount * w;
                }
            }
        }
    }

    public void AddObstacle(double x, double y, double radius = 3.5)
    {
        int minX = (int)Math.Max(1, x - radius);
        int maxX = (int)Math.Min(Width - 2, x + radius);
        int minY = (int)Math.Max(1, y - radius);
        int maxY = (int)Math.Min(Height - 2, y + radius);

        for (int py = minY; py <= maxY; py++)
        {
            for (int px = minX; px <= maxX; px++)
            {
                double d2 = (px - x) * (px - x) + (py - y) * (py - y);
                if (d2 <= radius * radius)
                {
                    Obstacles[py * Width + px] = true;
                    Omega[py * Width + px] = 0;
                    U[py * Width + px] = 0;
                    V[py * Width + px] = 0;
                }
            }
        }
    }
}
