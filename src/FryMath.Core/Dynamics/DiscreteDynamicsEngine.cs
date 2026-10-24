using System;
using System.Collections.Generic;

namespace FryMath.Core.Dynamics;

public enum DiscreteDynamicsMode
{
    ArnoldCatMap,
    LogisticCobweb,
    LogisticBifurcation,
    IteratedFunctionSystems
}

/// <summary>
/// Core mathematical engine for discrete dynamical systems, including
/// Arnold's Cat Map on the 2D Torus, the Logistic Map period-doubling cascade,
/// Lyapunov chaos exponents, and Iterated Function Systems (IFS).
/// </summary>
public sealed class DiscreteDynamicsEngine
{
    public DiscreteDynamicsMode Mode { get; set; } = DiscreteDynamicsMode.ArnoldCatMap;

    #region Arnold's Cat Map

    public const int CatGridSize = 64;
    private byte[,] _initialCatPattern = new byte[CatGridSize, CatGridSize];
    private byte[,] _currentCatPattern = new byte[CatGridSize, CatGridSize];

    public int CatStep { get; private set; } = 0;
    public int CatPeriod { get; private set; } = 48; // For N=64, period is exactly 48

    public byte[,] CurrentCatPattern => _currentCatPattern;

    public void InitializeCatPattern()
    {
        _initialCatPattern = new byte[CatGridSize, CatGridSize];
        _currentCatPattern = new byte[CatGridSize, CatGridSize];

        // Draw a distinct geometric cat face pattern with ears, eyes, nose, and whiskers
        for (int y = 0; y < CatGridSize; y++)
        {
            for (int x = 0; x < CatGridSize; x++)
            {
                // Head ellipse
                double dx = (x - 32) / 22.0;
                double dy = (y - 36) / 20.0;
                bool isHead = (dx * dx + dy * dy) <= 1.0;

                // Left Ear triangle
                bool isLeftEar = (x >= 12 && x <= 26 && y >= 8 && y <= 26 && (y - 8) >= 1.2 * Math.Abs(x - 19));
                // Right Ear triangle
                bool isRightEar = (x >= 38 && x <= 52 && y >= 8 && y <= 26 && (y - 8) >= 1.2 * Math.Abs(x - 45));

                // Left Eye
                double edx1 = (x - 23) / 4.0;
                double edy1 = (y - 32) / 6.0;
                bool isLeftEye = (edx1 * edx1 + edy1 * edy1) <= 1.0;

                // Right Eye
                double edx2 = (x - 41) / 4.0;
                double edy2 = (y - 32) / 6.0;
                bool isRightEye = (edx2 * edx2 + edy2 * edy2) <= 1.0;

                // Nose triangle
                bool isNose = (x >= 29 && x <= 35 && y >= 39 && y <= 44);

                // Whiskers lines
                bool isWhiskers = ((y == 40 || y == 43) && (x >= 8 && x <= 22 || x >= 42 && x <= 56));

                byte val = 0;
                if (isLeftEye || isRightEye || isNose) val = 40; // Dark features
                else if (isHead || isLeftEar || isRightEar || isWhiskers) val = 220; // Glowing cat body
                else if ((x / 8 + y / 8) % 2 == 0) val = 15; // Faint background grid

                _initialCatPattern[x, y] = val;
                _currentCatPattern[x, y] = val;
            }
        }

        CatStep = 0;
        CatPeriod = ComputeCatPeriod(CatGridSize);
    }

    /// <summary>
    /// Computes the exact recurrence period of Arnold's Cat Map for grid size N.
    /// By Poincaré recurrence on finite lattice, orbit must be periodic.
    /// </summary>
    public static int ComputeCatPeriod(int n)
    {
        int x = 1, y = 1;
        int steps = 0;
        do
        {
            int nx = (2 * x + y) % n;
            int ny = (x + y) % n;
            x = nx;
            y = ny;
            steps++;
            if (steps > 10000) break; // Safeguard
        } while (x != 1 || y != 1);

        return steps;
    }

    /// <summary>
    /// Advances Arnold's Cat Map by one discrete step on the 2D torus:
    /// [x', y']^T = [2 1; 1 1] [x, y]^T mod N
    /// </summary>
    public void StepCatMap()
    {
        byte[,] next = new byte[CatGridSize, CatGridSize];
        for (int y = 0; y < CatGridSize; y++)
        {
            for (int x = 0; x < CatGridSize; x++)
            {
                int nx = (2 * x + y) % CatGridSize;
                int ny = (x + y) % CatGridSize;
                next[nx, ny] = _currentCatPattern[x, y];
            }
        }
        _currentCatPattern = next;
        CatStep = (CatStep + 1) % CatPeriod;
    }

    public void ResetCatMap()
    {
        for (int y = 0; y < CatGridSize; y++)
        {
            for (int x = 0; x < CatGridSize; x++)
            {
                _currentCatPattern[x, y] = _initialCatPattern[x, y];
            }
        }
        CatStep = 0;
    }

    #endregion

    #region Logistic Map & Cobweb Dynamics

    public double LogisticR { get; set; } = 3.65;
    public double LogisticX0 { get; set; } = 0.45;
    public int CobwebSteps { get; set; } = 60;

    /// <summary>
    /// Evaluates quadratic logistic function f(x) = r * x * (1 - x).
    /// </summary>
    public static double EvaluateLogistic(double x, double r) => r * x * (1.0 - x);

    /// <summary>
    /// Generates cobweb trajectory path points: (x_n, x_n) -> (x_n, x_{n+1}) -> (x_{n+1}, x_{n+1}).
    /// </summary>
    public List<(double X, double Y)> GenerateCobwebOrbit()
    {
        var points = new List<(double X, double Y)>();
        double currentX = LogisticX0;
        points.Add((currentX, 0.0));

        for (int i = 0; i < CobwebSteps; i++)
        {
            double nextX = EvaluateLogistic(currentX, LogisticR);
            // Vertical line up/down to function curve
            points.Add((currentX, nextX));
            // Horizontal line to y = x diagonal
            points.Add((nextX, nextX));
            currentX = nextX;
        }

        return points;
    }

    /// <summary>
    /// Computes the Lyapunov exponent lambda(r) = lim (1/N) sum ln |r (1 - 2x_n)|.
    /// Positive lambda indicates deterministic chaos; negative indicates periodic attractor.
    /// </summary>
    public double ComputeLyapunovExponent(double r, double x0 = 0.5, int iterations = 1000, int warmup = 200)
    {
        double x = x0;
        // Warmup to settle onto attractor
        for (int i = 0; i < warmup; i++)
        {
            x = EvaluateLogistic(x, r);
        }

        double sum = 0.0;
        int validSteps = 0;
        for (int i = 0; i < iterations; i++)
        {
            x = EvaluateLogistic(x, r);
            double derivative = Math.Abs(r * (1.0 - 2.0 * x));
            if (derivative > 1e-12)
            {
                sum += Math.Log(derivative);
                validSteps++;
            }
        }

        return validSteps > 0 ? sum / validSteps : 0.0;
    }

    /// <summary>
    /// Computes bifurcation orbit tree points across parameter range r in [rMin, rMax].
    /// </summary>
    public List<(double R, double X)> GenerateBifurcationDiagram(double rMin = 2.8, double rMax = 4.0, int rSteps = 240, int warmup = 150, int keep = 60)
    {
        var result = new List<(double R, double X)>(rSteps * keep);
        double dr = (rMax - rMin) / rSteps;

        for (int step = 0; step <= rSteps; step++)
        {
            double r = rMin + step * dr;
            double x = 0.5;

            // Discard transient steps
            for (int i = 0; i < warmup; i++)
            {
                x = EvaluateLogistic(x, r);
            }

            // Collect asymptotic orbit points
            for (int i = 0; i < keep; i++)
            {
                x = EvaluateLogistic(x, r);
                result.Add((r, x));
            }
        }

        return result;
    }

    #endregion

    #region Iterated Function Systems (IFS)

    public record IfsTransform(double A, double B, double C, double D, double E, double F, double Weight);

    public string IfsPreset { get; set; } = "barnsley";
    public int IfsPointsCount { get; set; } = 40000;

    // Barnsley Fern Affine Transformations
    public static readonly IfsTransform[] BarnsleyFern =
    [
        new(0.00,  0.00,  0.00,  0.16, 0.00, 0.00, 0.01), // Stem
        new(0.85,  0.04, -0.04,  0.85, 0.00, 1.60, 0.85), // Successive smaller leaflets
        new(0.20, -0.26,  0.23,  0.22, 0.00, 1.60, 0.07), // Left leaflet
        new(-0.15, 0.28,  0.26,  0.24, 0.00, 0.44, 0.07)  // Right leaflet
    ];

    // Sierpinski Triangle 3-contraction maps
    public static readonly IfsTransform[] SierpinskiGasket =
    [
        new(0.50, 0.00, 0.00, 0.50, 0.00, 0.00, 0.3333),
        new(0.50, 0.00, 0.00, 0.50, 0.50, 0.00, 0.3333),
        new(0.50, 0.00, 0.00, 0.50, 0.25, 0.433, 0.3334)
    ];

    /// <summary>
    /// Generates point cloud for the selected Iterated Function System via Chaos Game.
    /// </summary>
    public List<(double X, double Y, int TransformIndex)> GenerateIfsPoints()
    {
        var transforms = (IfsPreset == "sierpinski") ? SierpinskiGasket : BarnsleyFern;
        var points = new List<(double X, double Y, int TransformIndex)>(IfsPointsCount);

        var rand = new Random(42);
        double x = 0.0, y = 0.0;

        // Skip transient steps
        for (int i = 0; i < 50; i++)
        {
            double r = rand.NextDouble();
            double cum = 0.0;
            for (int t = 0; t < transforms.Length; t++)
            {
                cum += transforms[t].Weight;
                if (r <= cum || t == transforms.Length - 1)
                {
                    var m = transforms[t];
                    double nx = m.A * x + m.B * y + m.E;
                    double ny = m.C * x + m.D * y + m.F;
                    x = nx;
                    y = ny;
                    break;
                }
            }
        }

        // Collect fractal points
        for (int i = 0; i < IfsPointsCount; i++)
        {
            double r = rand.NextDouble();
            double cum = 0.0;
            int chosenIndex = 0;
            for (int t = 0; t < transforms.Length; t++)
            {
                cum += transforms[t].Weight;
                if (r <= cum || t == transforms.Length - 1)
                {
                    chosenIndex = t;
                    var m = transforms[t];
                    double nx = m.A * x + m.B * y + m.E;
                    double ny = m.C * x + m.D * y + m.F;
                    x = nx;
                    y = ny;
                    break;
                }
            }
            points.Add((x, y, chosenIndex));
        }

        return points;
    }

    #endregion

    public DiscreteDynamicsEngine()
    {
        InitializeCatPattern();
    }
}
