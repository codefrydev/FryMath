using System;
using System.Collections.Generic;

namespace FryMath.Core.Calculus;

public enum CriticalPointKind
{
    LocalMinimum,
    LocalMaximum,
    InflectionPoint,
    Root
}

public sealed record CriticalPoint(double X, double Y, CriticalPointKind Kind, string Label);

public sealed record OsculatingCircle(double CenterX, double CenterY, double Radius, double Curvature);

public static class FunctionAnalyzer
{
    private const double H = 1e-5;

    public static double FirstDerivative(Func<double, double> f, double x)
    {
        return (f(x + H) - f(x - H)) / (2.0 * H);
    }

    public static double SecondDerivative(Func<double, double> f, double x)
    {
        return (f(x + H) - 2.0 * f(x) + f(x - H)) / (H * H);
    }

    public static OsculatingCircle ComputeOsculatingCircle(Func<double, double> f, double x)
    {
        double y = f(x);
        double d1 = FirstDerivative(f, x);
        double d2 = SecondDerivative(f, x);

        double denom = Math.Pow(1.0 + d1 * d1, 1.5);
        double curvature = Math.Abs(d2) / Math.Max(denom, 1e-12);

        if (Math.Abs(d2) < 1e-7 || curvature < 1e-5)
        {
            // Infinitely flat line; osculating circle degenerates to infinite radius
            return new OsculatingCircle(x, y, double.PositiveInfinity, 0.0);
        }

        double r = 1.0 / curvature;
        double cx = x - (d1 * (1.0 + d1 * d1)) / d2;
        double cy = y + (1.0 + d1 * d1) / d2;

        return new OsculatingCircle(cx, cy, r, curvature);
    }

    /// <summary>
    /// Scans a domain [xMin, xMax] for roots, local minima, local maxima, and inflection points.
    /// </summary>
    public static List<CriticalPoint> FindCriticalPoints(Func<double, double> f, double xMin, double xMax, int samples = 400)
    {
        var points = new List<CriticalPoint>();
        if (xMin >= xMax || samples < 10) return points;

        double step = (xMax - xMin) / samples;
        double prevX = xMin;
        double prevY = f(prevX);
        double prevD1 = FirstDerivative(f, prevX);
        double prevD2 = SecondDerivative(f, prevX);

        for (int i = 1; i <= samples; i++)
        {
            double currX = xMin + i * step;
            double currY = f(currX);
            double currD1 = FirstDerivative(f, currX);
            double currD2 = SecondDerivative(f, currX);

            // 1. Root check (sign change or near-zero hit)
            if (Math.Abs(currY) < 1e-9)
            {
                if (!points.Exists(p => p.Kind == CriticalPointKind.Root && Math.Abs(p.X - currX) < step * 0.5))
                {
                    points.Add(new CriticalPoint(currX, 0.0, CriticalPointKind.Root, $"Root ({currX:F3}, 0)"));
                }
            }
            else if (prevY * currY < 0.0 && !double.IsNaN(prevY) && !double.IsNaN(currY))
            {
                double rootX = RefineRoot(f, prevX, currX);
                if (!points.Exists(p => p.Kind == CriticalPointKind.Root && Math.Abs(p.X - rootX) < step * 0.5))
                {
                    points.Add(new CriticalPoint(rootX, 0.0, CriticalPointKind.Root, $"Root ({rootX:F3}, 0)"));
                }
            }

            // 2. Extrema check (sign change in 1st derivative)
            if (prevD1 * currD1 < 0.0 && !double.IsNaN(prevD1) && !double.IsNaN(currD1))
            {
                double critX = RefineRoot(x => FirstDerivative(f, x), prevX, currX);
                double critY = f(critX);
                double d2AtCrit = SecondDerivative(f, critX);

                if (d2AtCrit > 1e-5)
                {
                    points.Add(new CriticalPoint(critX, critY, CriticalPointKind.LocalMinimum, $"Min ({critX:F2}, {critY:F2})"));
                }
                else if (d2AtCrit < -1e-5)
                {
                    points.Add(new CriticalPoint(critX, critY, CriticalPointKind.LocalMaximum, $"Max ({critX:F2}, {critY:F2})"));
                }
            }

            // 3. Inflection check (sign change in 2nd derivative)
            if (prevD2 * currD2 < 0.0 && !double.IsNaN(prevD2) && !double.IsNaN(currD2))
            {
                double inflX = RefineRoot(x => SecondDerivative(f, x), prevX, currX);
                double inflY = f(inflX);
                points.Add(new CriticalPoint(inflX, inflY, CriticalPointKind.InflectionPoint, $"Inflection ({inflX:F2}, {inflY:F2})"));
            }

            prevX = currX;
            prevY = currY;
            prevD1 = currD1;
            prevD2 = currD2;
        }

        return points;
    }

    private static double RefineRoot(Func<double, double> f, double a, double b, int maxIter = 30)
    {
        double fa = f(a);
        for (int i = 0; i < maxIter; i++)
        {
            double mid = (a + b) * 0.5;
            double fmid = f(mid);
            if (Math.Abs(fmid) < 1e-9 || (b - a) < 1e-8) return mid;

            if (fa * fmid < 0)
            {
                b = mid;
            }
            else
            {
                a = mid;
                fa = fmid;
            }
        }
        return (a + b) * 0.5;
    }

    /// <summary>
    /// Computes intersection points between two functions f1 and f2 in [xMin, xMax].
    /// </summary>
    public static List<Point2D> FindIntersections(Func<double, double> f1, Func<double, double> f2, double xMin, double xMax, int samples = 300)
    {
        var intersections = new List<Point2D>();
        double diff(double x) => f1(x) - f2(x);

        double step = (xMax - xMin) / samples;
        double prevX = xMin;
        double prevD = diff(prevX);

        for (int i = 1; i <= samples; i++)
        {
            double currX = xMin + i * step;
            double currD = diff(currX);

            if (double.IsNaN(prevD) || double.IsNaN(currD))
            {
                prevX = currX;
                prevD = currD;
                continue;
            }

            if (prevD * currD <= 0.0)
            {
                double rootX = RefineRoot(diff, prevX, currX);
                double rootY = f1(rootX);
                if (!double.IsNaN(rootY) && !double.IsInfinity(rootY))
                {
                    if (!intersections.Exists(p => Math.Abs(p.X - rootX) < Math.Max(step * 1.5, 0.02)))
                    {
                        intersections.Add(new Point2D(rootX, rootY));
                    }
                }
            }

            prevX = currX;
            prevD = currD;
        }

        return intersections;
    }

    /// <summary>
    /// Computes arc length of f from a to b: \int_a^b \sqrt{1 + [f'(x)]^2} dx.
    /// </summary>
    public static double ArcLength(Func<double, double> f, double a, double b)
    {
        if (a > b) (a, b) = (b, a);
        double integrand(double x)
        {
            double d = FirstDerivative(f, x);
            return Math.Sqrt(1.0 + d * d);
        }
        return NumericalIntegration.IntegrateAdaptive(integrand, a, b);
    }

    /// <summary>
    /// Computes volume of solid of revolution around x-axis: \pi \int_a^b [f(x)]^2 dx.
    /// </summary>
    public static double VolumeOfRevolution(Func<double, double> f, double a, double b)
    {
        if (a > b) (a, b) = (b, a);
        double integrand(double x)
        {
            double y = f(x);
            return Math.PI * y * y;
        }
        return NumericalIntegration.IntegrateAdaptive(integrand, a, b);
    }

    /// <summary>
    /// Computes the secant slope between x0 and x0 + deltaX: \frac{f(x0 + deltaX) - f(x0)}{deltaX}.
    /// </summary>
    public static double SecantSlope(Func<double, double> f, double x0, double deltaX)
    {
        if (Math.Abs(deltaX) < 1e-12) return FirstDerivative(f, x0);
        return (f(x0 + deltaX) - f(x0)) / deltaX;
    }

    /// <summary>
    /// Returns slope m and intercept c for tangent line y = mx + c at x0.
    /// </summary>
    public static (double Slope, double Intercept) TangentLineEquation(Func<double, double> f, double x0)
    {
        double m = FirstDerivative(f, x0);
        double y0 = f(x0);
        double c = y0 - m * x0;
        return (m, c);
    }

    /// <summary>
    /// Returns slope m and intercept c for normal line at x0.
    /// </summary>
    public static (double Slope, double Intercept)? NormalLineEquation(Func<double, double> f, double x0)
    {
        double m = FirstDerivative(f, x0);
        if (Math.Abs(m) < 1e-12) return null; // Vertical normal
        double normalM = -1.0 / m;
        double y0 = f(x0);
        double c = y0 - normalM * x0;
        return (normalM, c);
    }

    public record Point2D(double X, double Y);
}
