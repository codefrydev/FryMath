using System;

namespace FryMath.Core.Waves;

public enum WaveSimulationMode
{
    ChladniPlate,
    CircularDrum,
    InteractiveRippleTank
}

public sealed class ChladniParticle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Vx { get; set; }
    public double Vy { get; set; }

    public ChladniParticle(double x, double y)
    {
        X = x;
        Y = y;
    }
}

public sealed class Wave2DEngine
{
    public const int GridSize = 80;
    private readonly double[,] _uPrev = new double[GridSize, GridSize];
    private readonly double[,] _uCurr = new double[GridSize, GridSize];
    private readonly double[,] _uNext = new double[GridSize, GridSize];

    public WaveSimulationMode Mode { get; set; } = WaveSimulationMode.ChladniPlate;
    public int ModeN { get; set; } = 3;
    public int ModeM { get; set; } = 5;
    public double WaveSpeed { get; set; } = 1.0;
    public double Damping { get; set; } = 0.005;
    public double Time { get; private set; }

    // Sand particles for Chladni resonance
    public ChladniParticle[] SandParticles { get; }
    private readonly Random _rand = new(42);

    public Wave2DEngine(int particleCount = 450)
    {
        SandParticles = new ChladniParticle[particleCount];
        ResetParticles();
    }

    public void ResetParticles()
    {
        for (int i = 0; i < SandParticles.Length; i++)
        {
            SandParticles[i] = new ChladniParticle(
                _rand.NextDouble() * 2.0 - 1.0,
                _rand.NextDouble() * 2.0 - 1.0
            );
        }
    }

    public void DropImpulse(int gx, int gy, double amplitude = 2.5, int radius = 3)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int px = gx + dx;
                int py = gy + dy;
                if (px >= 1 && px < GridSize - 1 && py >= 1 && py < GridSize - 1)
                {
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist <= radius)
                    {
                        double factor = Math.Cos(dist / radius * (Math.PI * 0.5));
                        _uCurr[px, py] += amplitude * factor;
                        _uPrev[px, py] += amplitude * factor;
                    }
                }
            }
        }
    }

    public void StepSimulation(double dt)
    {
        Time += dt;

        if (Mode == WaveSimulationMode.InteractiveRippleTank)
        {
            StepFiniteDifferenceWave(dt);
        }
        else if (Mode == WaveSimulationMode.ChladniPlate)
        {
            StepChladniParticles(dt);
        }
        else if (Mode == WaveSimulationMode.CircularDrum)
        {
            StepChladniParticles(dt);
        }
    }

    private void StepFiniteDifferenceWave(double dt)
    {
        // 2D Wave Equation: u_tt = c^2 (u_xx + u_yy) - gamma * u_t
        // CFL condition: c * dt / dx <= 1 / sqrt(2)
        double cSq = 0.25; // c^2 * dt^2 / dx^2
        double dampFactor = 1.0 - Damping;

        for (int x = 1; x < GridSize - 1; x++)
        {
            for (int y = 1; y < GridSize - 1; y++)
            {
                double laplacian = _uCurr[x + 1, y] + _uCurr[x - 1, y] +
                                   _uCurr[x, y + 1] + _uCurr[x, y - 1] -
                                   4.0 * _uCurr[x, y];

                _uNext[x, y] = (2.0 * _uCurr[x, y] - _uPrev[x, y] + cSq * laplacian) * dampFactor;
            }
        }

        // Absorbing / zero Dirichlet boundaries
        for (int i = 0; i < GridSize; i++)
        {
            _uNext[0, i] = 0.0;
            _uNext[GridSize - 1, i] = 0.0;
            _uNext[i, 0] = 0.0;
            _uNext[i, GridSize - 1] = 0.0;
        }

        // Cycle buffers
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                _uPrev[x, y] = _uCurr[x, y];
                _uCurr[x, y] = _uNext[x, y];
            }
        }
    }

    public double EvaluateChladni(double x, double y)
    {
        // Coordinates in [-1, 1]
        // Chladni square plate eigenmode:
        // u(x, y) = cos(n pi x / 2) cos(m pi y / 2) - cos(m pi x / 2) cos(n pi y / 2)
        double px = (x + 1.0) * 0.5 * Math.PI;
        double py = (y + 1.0) * 0.5 * Math.PI;

        double term1 = Math.Cos(ModeN * px) * Math.Cos(ModeM * py);
        double term2 = Math.Cos(ModeM * px) * Math.Cos(ModeN * py);

        return term1 - term2;
    }

    public double EvaluateCircularDrum(double x, double y)
    {
        double r = Math.Sqrt(x * x + y * y);
        if (r > 1.0) return 0.0;

        double theta = Math.Atan2(y, x);
        // Approximation of Bessel J_m(alpha * r) * cos(m theta)
        double k = (ModeN + 0.5) * Math.PI;
        double besselApprox = Math.Sin(k * r) / Math.Max(0.1, Math.Sqrt(k * r));
        return besselApprox * Math.Cos(ModeM * theta) * (1.0 - r);
    }

    public double GetDisplacement(double x, double y)
    {
        if (Mode == WaveSimulationMode.ChladniPlate)
        {
            return EvaluateChladni(x, y) * Math.Cos(Time * 6.0);
        }
        else if (Mode == WaveSimulationMode.CircularDrum)
        {
            return EvaluateCircularDrum(x, y) * Math.Cos(Time * 6.0);
        }
        else
        {
            // Interactive ripple tank from grid
            int gx = (int)Math.Clamp((x + 1.0) * 0.5 * GridSize, 0, GridSize - 1);
            int gy = (int)Math.Clamp((y + 1.0) * 0.5 * GridSize, 0, GridSize - 1);
            return _uCurr[gx, gy];
        }
    }

    private void StepChladniParticles(double dt)
    {
        // Chladni plate vibration physics:
        // When plate vibrates with acceleration a = -omega^2 u(x, y), particles bounce.
        // During each bounce, particles receive a random kick scaled by |u(x, y)|.
        // Near nodal lines (where u(x, y) = 0), acceleration is zero, so particles settle!
        for (int i = 0; i < SandParticles.Length; i++)
        {
            var p = SandParticles[i];
            double u = Mode == WaveSimulationMode.CircularDrum
                ? EvaluateCircularDrum(p.X, p.Y)
                : EvaluateChladni(p.X, p.Y);

            double absU = Math.Abs(u);

            // Drift velocity away from high vibration regions (gradient of |u|)
            double eps = 0.02;
            double uRight = Mode == WaveSimulationMode.CircularDrum
                ? Math.Abs(EvaluateCircularDrum(p.X + eps, p.Y))
                : Math.Abs(EvaluateChladni(p.X + eps, p.Y));
            double uUp = Mode == WaveSimulationMode.CircularDrum
                ? Math.Abs(EvaluateCircularDrum(p.X, p.Y + eps))
                : Math.Abs(EvaluateChladni(p.X, p.Y + eps));

            double gradX = (uRight - absU) / eps;
            double gradY = (uUp - absU) / eps;

            // Particles move down the vibration gradient towards nodal lines
            p.Vx = p.Vx * 0.85 - gradX * 0.04 * absU + (_rand.NextDouble() - 0.5) * 0.02 * absU;
            p.Vy = p.Vy * 0.85 - gradY * 0.04 * absU + (_rand.NextDouble() - 0.5) * 0.02 * absU;

            p.X += p.Vx;
            p.Y += p.Vy;

            // Keep inside plate boundary [-0.95, 0.95]
            if (p.X < -0.95) { p.X = -0.95; p.Vx = -p.Vx * 0.5; }
            if (p.X > 0.95) { p.X = 0.95; p.Vx = -p.Vx * 0.5; }
            if (p.Y < -0.95) { p.Y = -0.95; p.Vy = -p.Vy * 0.5; }
            if (p.Y > 0.95) { p.Y = 0.95; p.Vy = -p.Vy * 0.5; }
        }
    }
}
