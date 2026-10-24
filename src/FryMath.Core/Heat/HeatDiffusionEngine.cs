using System;
using System.Collections.Generic;

namespace FryMath.Core.Heat;

public enum BoundaryType
{
    DirichletFixed,
    NeumannInsulated,
    Periodic
}

public sealed record HeatSource(double X, double Y, double Temperature, double Radius, bool IsActive = true);

public sealed class HeatDiffusionEngine
{
    public int Width { get; }
    public int Height { get; }

    public double[,] Temperature { get; private set; }
    private double[,] _tempBuffer;

    public double Diffusivity { get; set; } = 0.22; // α (thermal diffusivity)
    public BoundaryType Boundary { get; set; } = BoundaryType.DirichletFixed;
    public double AmbientTemperature { get; set; } = 0.0;

    public double TopBoundary { get; set; } = 0.0;
    public double BottomBoundary { get; set; } = 0.0;
    public double LeftBoundary { get; set; } = 0.0;
    public double RightBoundary { get; set; } = 0.0;

    public List<HeatSource> Sources { get; } = new();

    public HeatDiffusionEngine(int width = 72, int height = 72)
    {
        Width = Math.Max(16, width);
        Height = Math.Max(16, height);
        Temperature = new double[Width, Height];
        _tempBuffer = new double[Width, Height];
        Reset(AmbientTemperature);
    }

    public void Reset(double baseTemp = 0.0)
    {
        AmbientTemperature = baseTemp;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Temperature[x, y] = baseTemp;
                _tempBuffer[x, y] = baseTemp;
            }
        }
    }

    public double GetDirichletBoundaryValue(int x, int y)
    {
        if (y <= 0) return TopBoundary;
        if (y >= Height - 1) return BottomBoundary;
        if (x <= 0) return LeftBoundary;
        if (x >= Width - 1) return RightBoundary;
        return AmbientTemperature;
    }

    /// <summary>
    /// Performs one forward finite-difference step of the 2D Heat Equation:
    /// ∂T/∂t = α ∇²T + Q(x, y)
    /// </summary>
    public void Step(double dt = 0.5)
    {
        // Enforce von Neumann stability limit: α dt / dx² <= 0.25 (with dx=1)
        double r = Math.Min(Diffusivity * dt, 0.24);

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (Boundary == BoundaryType.DirichletFixed && (x == 0 || x == Width - 1 || y == 0 || y == Height - 1))
                {
                    _tempBuffer[x, y] = GetDirichletBoundaryValue(x, y);
                    continue;
                }

                // Neighbor lookups with boundary conditions
                double left = GetNeighbor(x - 1, y, x, y);
                double right = GetNeighbor(x + 1, y, x, y);
                double up = GetNeighbor(x, y - 1, x, y);
                double down = GetNeighbor(x, y + 1, x, y);

                double laplacian = left + right + up + down - 4.0 * Temperature[x, y];
                _tempBuffer[x, y] = Temperature[x, y] + r * laplacian;
            }
        }

        // Apply persistent heat sources and sinks
        ApplyHeatSources(_tempBuffer);

        // Swap buffers
        var temp = Temperature;
        Temperature = _tempBuffer;
        _tempBuffer = temp;
    }

    /// <summary>
    /// Solves for steady-state Laplace equilibrium (∇²T = 0) via Gauss-Seidel relaxation.
    /// </summary>
    public void RelaxToSteadyState(int iterations = 350)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Boundary == BoundaryType.DirichletFixed && (x == 0 || x == Width - 1 || y == 0 || y == Height - 1))
                    {
                        Temperature[x, y] = GetDirichletBoundaryValue(x, y);
                        continue;
                    }

                    double left = GetNeighbor(x - 1, y, x, y);
                    double right = GetNeighbor(x + 1, y, x, y);
                    double up = GetNeighbor(x, y - 1, x, y);
                    double down = GetNeighbor(x, y + 1, x, y);

                    Temperature[x, y] = 0.25 * (left + right + up + down);
                }
            }
            ApplyHeatSources(Temperature);
        }
    }

    private double GetNeighbor(int nx, int ny, int curX, int curY)
    {
        if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
        {
            return Temperature[nx, ny];
        }

        return Boundary switch
        {
            BoundaryType.NeumannInsulated => Temperature[curX, curY], // Zero flux ∂T/∂n = 0
            BoundaryType.Periodic => Temperature[(nx + Width) % Width, (ny + Height) % Height],
            _ => ny < 0 ? TopBoundary :
                 ny >= Height ? BottomBoundary :
                 nx < 0 ? LeftBoundary :
                 nx >= Width ? RightBoundary : AmbientTemperature
        };
    }


    private void ApplyHeatSources(double[,] grid)
    {
        foreach (var s in Sources)
        {
            if (!s.IsActive) continue;
            int cx = (int)(s.X * Width);
            int cy = (int)(s.Y * Height);
            int rad = Math.Max(1, (int)(s.Radius * Width));

            for (int dx = -rad; dx <= rad; dx++)
            {
                for (int dy = -rad; dy <= rad; dy++)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px >= 0 && px < Width && py >= 0 && py < Height)
                    {
                        double dist = Math.Sqrt(dx * dx + dy * dy) / rad;
                        if (dist <= 1.0)
                        {
                            // Smooth Gaussian source profile
                            double weight = Math.Exp(-2.5 * dist * dist);
                            grid[px, py] = grid[px, py] * (1.0 - weight) + s.Temperature * weight;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Computes the Fourier heat flux vector q = -k ∇T at normalized coordinates (u, v) in [0, 1].
    /// </summary>
    public (double Qx, double Qy, double Temp) SampleFlux(double u, double v)
    {
        int x = Math.Clamp((int)(u * (Width - 1)), 1, Width - 2);
        int y = Math.Clamp((int)(v * (Height - 1)), 1, Height - 2);

        double dTdx = (Temperature[x + 1, y] - Temperature[x - 1, y]) * 0.5;
        double dTdy = (Temperature[x, y + 1] - Temperature[x, y - 1]) * 0.5;

        // Fourier's Law: q = -k * ∇T (heat flows down temperature gradient)
        return (-dTdx, -dTdy, Temperature[x, y]);
    }

    public void LoadPreset(string preset)
    {
        Sources.Clear();
        switch (preset.ToLowerInvariant())
        {
            case "dipole":
                Reset(0.0);
                Boundary = BoundaryType.DirichletFixed;
                TopBoundary = BottomBoundary = LeftBoundary = RightBoundary = 0.0;
                Sources.Add(new HeatSource(0.32, 0.5, 100.0, 0.12));
                Sources.Add(new HeatSource(0.68, 0.5, -50.0, 0.12));
                RelaxToSteadyState(100);
                break;
            case "insulated":
                Reset(20.0);
                Boundary = BoundaryType.NeumannInsulated;
                Sources.Add(new HeatSource(0.5, 0.5, 120.0, 0.15));
                break;
            case "corners":
                Reset(0.0);
                Boundary = BoundaryType.DirichletFixed;
                TopBoundary = BottomBoundary = LeftBoundary = RightBoundary = 0.0;
                Sources.Add(new HeatSource(0.2, 0.2, 100.0, 0.10));
                Sources.Add(new HeatSource(0.8, 0.2, 100.0, 0.10));
                Sources.Add(new HeatSource(0.2, 0.8, -40.0, 0.10));
                Sources.Add(new HeatSource(0.8, 0.8, -40.0, 0.10));
                RelaxToSteadyState(80);
                break;
            case "laplace":
            default:
                Reset(0.0);
                Boundary = BoundaryType.DirichletFixed;
                TopBoundary = 100.0;
                BottomBoundary = 0.0;
                LeftBoundary = 20.0;
                RightBoundary = 20.0;
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                        {
                            Temperature[x, y] = GetDirichletBoundaryValue(x, y);
                        }
                    }
                }
                RelaxToSteadyState(350);
                break;
        }
    }
}
