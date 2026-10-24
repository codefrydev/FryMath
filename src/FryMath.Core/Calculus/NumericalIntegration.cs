using System;
using System.Collections.Generic;

namespace FryMath.Core.Calculus;

public enum RiemannRule
{
    Left,
    Right,
    Midpoint,
    Trapezoid,
    Simpson
}

public sealed record RiemannPartition(double XLeft, double XRight, double SampledY, double Area);

public static class NumericalIntegration
{
    /// <summary>
    /// Computes Riemann partitions for visual rendering and area summation on MathCanvas2D.
    /// </summary>
    public static (List<RiemannPartition> Partitions, double TotalArea) ComputeRiemannSum(
        Func<double, double> f,
        double a,
        double b,
        int partitions,
        RiemannRule rule)
    {
        var list = new List<RiemannPartition>(partitions);
        if (partitions <= 0 || double.IsNaN(a) || double.IsNaN(b))
        {
            return (list, 0.0);
        }

        if (a > b) (a, b) = (b, a);
        double dx = (b - a) / partitions;
        double totalArea = 0.0;

        for (int i = 0; i < partitions; i++)
        {
            double x0 = a + i * dx;
            double x1 = x0 + dx;

            double sampleY = rule switch
            {
                RiemannRule.Left => f(x0),
                RiemannRule.Right => f(x1),
                RiemannRule.Midpoint => f((x0 + x1) * 0.5),
                RiemannRule.Trapezoid => 0.5 * (f(x0) + f(x1)),
                RiemannRule.Simpson => (f(x0) + 4.0 * f((x0 + x1) * 0.5) + f(x1)) / 6.0,
                _ => f(x0)
            };

            if (double.IsNaN(sampleY) || double.IsInfinity(sampleY)) sampleY = 0.0;

            double area = sampleY * dx;
            totalArea += area;
            list.Add(new RiemannPartition(x0, x1, sampleY, area));
        }

        return (list, totalArea);
    }

    public static double IntegrateRiemann(Func<double, double> f, double a, double b, int partitions, RiemannRule rule)
    {
        var (_, totalArea) = ComputeRiemannSum(f, a, b, partitions, rule);
        return totalArea;
    }

    /// <summary>
    /// Adaptive Simpson's 3/8 Quadrature for high-precision numerical definite integration.
    /// </summary>
    public static double IntegrateAdaptive(Func<double, double> f, double a, double b, double eps = 1e-7, int maxDepth = 20)
    {
        if (Math.Abs(a - b) < 1e-15) return 0.0;
        bool reversed = a > b;
        if (reversed) (a, b) = (b, a);

        double fa = f(a);
        double fb = f(b);
        double c = (a + b) * 0.5;
        double fc = f(c);

        double whole = (b - a) / 6.0 * (fa + 4.0 * fc + fb);
        double res = AdaptiveSimpsonRecursive(f, a, b, eps, whole, fa, fb, fc, maxDepth);

        return reversed ? -res : res;
    }

    private static double AdaptiveSimpsonRecursive(
        Func<double, double> f,
        double a,
        double b,
        double eps,
        double whole,
        double fa,
        double fb,
        double fc,
        int depth)
    {
        double c = (a + b) * 0.5;
        double d = (a + c) * 0.5;
        double e = (c + b) * 0.5;

        double fd = f(d);
        double fe = f(e);

        double h = b - a;
        double left = h / 12.0 * (fa + 4.0 * fd + fc);
        double right = h / 12.0 * (fc + 4.0 * fe + fb);
        double sum = left + right;

        if (depth <= 0 || Math.Abs(sum - whole) <= 15.0 * eps)
        {
            return sum + (sum - whole) / 15.0;
        }

        return AdaptiveSimpsonRecursive(f, a, c, eps * 0.5, left, fa, fc, fd, depth - 1) +
               AdaptiveSimpsonRecursive(f, c, b, eps * 0.5, right, fc, fb, fe, depth - 1);
    }
}
