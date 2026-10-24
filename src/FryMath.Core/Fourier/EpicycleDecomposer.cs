using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FryMath.Core.LinearAlgebra;

namespace FryMath.Core.Fourier;

public sealed record Epicycle(int Frequency, double Radius, double InitialPhase)
{
    public Vector2D EvaluateAt(double t)
    {
        double angle = 2.0 * Math.PI * Frequency * t + InitialPhase;
        return new Vector2D(Radius * Math.Cos(angle), Radius * Math.Sin(angle));
    }
}

public static class EpicycleDecomposer
{
    /// <summary>
    /// Decomposes a closed sequence of 2D points into a sorted chain of Fourier epicycles.
    /// </summary>
    public static List<Epicycle> Decompose(IReadOnlyList<Vector2D> points, int maxHarmonics = 30)
    {
        var result = new List<Epicycle>();
        int n = points.Count;
        if (n == 0) return result;

        maxHarmonics = Math.Clamp(maxHarmonics, 1, n / 2);

        // Discrete Fourier transform: c_k = (1/N) * sum_{j=0}^{N-1} z_j * exp(-i 2pi k j / N)
        for (int k = -maxHarmonics; k <= maxHarmonics; k++)
        {
            Complex sum = Complex.Zero;
            for (int j = 0; j < n; j++)
            {
                double angle = -2.0 * Math.PI * k * j / n;
                Complex exponential = new(Math.Cos(angle), Math.Sin(angle));
                Complex z = new(points[j].X, points[j].Y);
                sum += z * exponential;
            }

            Complex ck = sum / n;
            result.Add(new Epicycle(k, ck.Magnitude, ck.Phase));
        }

        // Sort descending by radius so the biggest dominant circles render first
        return result.OrderByDescending(e => e.Radius).ToList();
    }

    /// <summary>
    /// Generates canonical test paths (e.g. Square wave, Sawtooth, Trefoil Knot, Heart Curve).
    /// </summary>
    public static List<Vector2D> GeneratePresetPath(string presetName, int sampleCount = 256)
    {
        var list = new List<Vector2D>(sampleCount);
        double dt = 1.0 / sampleCount;

        for (int i = 0; i < sampleCount; i++)
        {
            double t = i * dt;
            double angle = 2.0 * Math.PI * t;

            Vector2D pt = presetName.ToLowerInvariant() switch
            {
                "square" => new Vector2D(
                    (t - 0.5) * 400.0,
                    t < 0.5 ? 80.0 : -80.0
                ),
                "sawtooth" => new Vector2D(
                    (t - 0.5) * 400.0,
                    (2.0 * t - 1.0) * 80.0
                ),
                "triangle" => new Vector2D(
                    (t - 0.5) * 400.0,
                    (Math.Abs(4.0 * (t - 0.5)) - 1.0) * 80.0
                ),
                "heart" => new Vector2D(
                    16.0 * Math.Pow(Math.Sin(angle), 3) * 8.0,
                    -(13.0 * Math.Cos(angle) - 5.0 * Math.Cos(2.0 * angle) - 2.0 * Math.Cos(3.0 * angle) - Math.Cos(4.0 * angle)) * 8.0
                ),
                "trefoil" => new Vector2D(
                    (Math.Sin(angle) + 2.0 * Math.Sin(2.0 * angle)) * 50.0,
                    (Math.Cos(angle) - 2.0 * Math.Cos(2.0 * angle)) * 50.0
                ),
                "star" => new Vector2D(
                    (110.0 + 45.0 * Math.Cos(5.0 * angle)) * Math.Cos(angle),
                    (110.0 + 45.0 * Math.Cos(5.0 * angle)) * Math.Sin(angle)
                ),
                "infinity" or "lemniscate" => new Vector2D(
                    170.0 * Math.Cos(angle) / (1.0 + Math.Pow(Math.Sin(angle), 2)),
                    170.0 * Math.Sin(angle) * Math.Cos(angle) / (1.0 + Math.Pow(Math.Sin(angle), 2))
                ),
                "pi" => new Vector2D(
                    120.0 * Math.Cos(angle) + 25.0 * Math.Cos(3.0 * angle),
                    -(100.0 * Math.Sin(angle) - 30.0 * Math.Sin(2.0 * angle))
                ),
                "fhole" => new Vector2D(
                    40.0 * Math.Sin(angle) + 25.0 * Math.Sin(3.0 * angle),
                    140.0 * Math.Cos(angle) - 30.0 * Math.Cos(2.0 * angle)
                ),
                _ => new Vector2D(120.0 * Math.Cos(angle), 120.0 * Math.Sin(angle))
            };

            list.Add(pt);
        }

        return list;
    }

    /// <summary>
    /// Resamples an arbitrary sequence of user-drawn mouse/stylus points into an equidistant closed contour,
    /// centers it at the origin, and scales it to standard viewport dimensions.
    /// </summary>
    public static List<Vector2D> ResampleAndCenter(IReadOnlyList<Vector2D> rawPoints, int targetCount = 256)
    {
        if (rawPoints == null || rawPoints.Count < 3)
        {
            return GeneratePresetPath("circle", targetCount);
        }

        // Calculate cumulative arc length
        var cumLengths = new List<double>(rawPoints.Count + 1) { 0.0 };
        for (int i = 0; i < rawPoints.Count; i++)
        {
            var p1 = rawPoints[i];
            var p2 = rawPoints[(i + 1) % rawPoints.Count];
            double segLen = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            cumLengths.Add(cumLengths[^1] + Math.Max(segLen, 1e-6));
        }

        double totalLen = cumLengths[^1];
        if (totalLen < 1.0)
        {
            return GeneratePresetPath("circle", targetCount);
        }

        var resampled = new List<Vector2D>(targetCount);
        double step = totalLen / targetCount;

        int segIdx = 0;
        for (int i = 0; i < targetCount; i++)
        {
            double targetDist = i * step;
            while (segIdx < rawPoints.Count && cumLengths[segIdx + 1] < targetDist)
            {
                segIdx++;
            }

            if (segIdx >= rawPoints.Count) segIdx = rawPoints.Count - 1;

            double d0 = cumLengths[segIdx];
            double d1 = cumLengths[segIdx + 1];
            double t = (targetDist - d0) / Math.Max(d1 - d0, 1e-9);

            var pA = rawPoints[segIdx];
            var pB = rawPoints[(segIdx + 1) % rawPoints.Count];

            double rx = pA.X + (pB.X - pA.X) * t;
            double ry = pA.Y + (pB.Y - pA.Y) * t;
            resampled.Add(new Vector2D(rx, ry));
        }

        // Compute centroid
        double meanX = resampled.Average(p => p.X);
        double meanY = resampled.Average(p => p.Y);

        // Center points
        for (int i = 0; i < resampled.Count; i++)
        {
            resampled[i] = new Vector2D(resampled[i].X - meanX, resampled[i].Y - meanY);
        }

        // Compute maximum radius and normalize scale
        double maxRadius = resampled.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y));
        if (maxRadius > 1e-4)
        {
            double scale = 140.0 / maxRadius;
            for (int i = 0; i < resampled.Count; i++)
            {
                resampled[i] = new Vector2D(resampled[i].X * scale, resampled[i].Y * scale);
            }
        }

        return resampled;
    }
}
