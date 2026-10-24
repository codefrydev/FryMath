using System;
using System.Collections.Generic;

namespace FryMath.Core.Catastrophe;

public enum CatastropheType
{
    Fold,       // A2: V(x) = x³/3 + ax
    Cusp,       // A3: V(x) = x⁴/4 + ax²/2 + bx
    Swallowtail,// A4: V(x) = x⁵/5 + ax³/3 + bx²/2 + cx
    Butterfly   // A5: V(x) = x⁶/6 + ax⁴/4 + bx³/3 + cx²/2 + dx
}

public readonly record struct CatastropheEquilibrium(double X, double Potential, double Curvature, bool IsStable);

public readonly record struct CuspBifurcationPoint(double A, double BPlus, double BMinus);

public class CatastropheEngine
{
    /// <summary>
    /// Computes potential V(x) for the Cusp Catastrophe: V(x) = x⁴/4 + a x²/2 + b x
    /// </summary>
    public double ComputeCuspPotential(double x, double a, double b)
    {
        return 0.25 * Math.Pow(x, 4) + 0.5 * a * x * x + b * x;
    }

    /// <summary>
    /// Computes dV/dx for Cusp: V'(x) = x³ + a x + b
    /// </summary>
    public double ComputeCuspDerivative(double x, double a, double b)
    {
        return x * x * x + a * x + b;
    }

    /// <summary>
    /// Computes d²V/dx² for Cusp: V''(x) = 3x² + a
    /// </summary>
    public double ComputeCuspCurvature(double x, double a)
    {
        return 3.0 * x * x + a;
    }

    /// <summary>
    /// Evaluates the discriminant of x³ + ax + b = 0: Δ = -(4a³ + 27b²) / 108.
    /// If 4a³ + 27b² < 0, there are 3 real equilibria (bistable cusp region).
    /// If 4a³ + 27b² > 0, there is 1 real equilibrium (monostable).
    /// If 4a³ + 27b² = 0, bifurcation occurs (fold points).
    /// </summary>
    public double ComputeCuspDiscriminant(double a, double b)
    {
        return 4.0 * Math.Pow(a, 3) + 27.0 * b * b;
    }

    /// <summary>
    /// Finds all real equilibria for the Cusp manifold: x³ + a x + b = 0 using Cardano-Viète formulas.
    /// Returns sorted list of equilibria with their stability.
    /// </summary>
    public List<CatastropheEquilibrium> SolveCuspEquilibria(double a, double b)
    {
        var roots = new List<double>();
        double disc = ComputeCuspDiscriminant(a, b);

        if (disc < -1e-9)
        {
            // 3 distinct real roots (Viète's trigonometric method)
            double p = a;
            double q = b;
            double r = Math.Sqrt(-Math.Pow(p / 3.0, 3));
            double phi = Math.Acos(Math.Clamp(-q / (2.0 * r), -1.0, 1.0));
            double m = 2.0 * Math.Cbrt(r);

            roots.Add(m * Math.Cos(phi / 3.0));
            roots.Add(m * Math.Cos((phi + 2.0 * Math.PI) / 3.0));
            roots.Add(m * Math.Cos((phi + 4.0 * Math.PI) / 3.0));
        }
        else if (Math.Abs(disc) <= 1e-9)
        {
            // Bifurcation: Multiple roots
            if (Math.Abs(a) < 1e-9 && Math.Abs(b) < 1e-9)
            {
                roots.Add(0.0);
            }
            else
            {
                double r1 = 3.0 * b / a;
                double r2 = -3.0 * b / (2.0 * a);
                roots.Add(r1);
                roots.Add(r2);
            }
        }
        else
        {
            // 1 real root (Cardano's formula)
            double halfQ = b / 2.0;
            double sqrtD = Math.Sqrt(disc / 108.0);
            double u = Math.Cbrt(-halfQ + sqrtD);
            double v = Math.Cbrt(-halfQ - sqrtD);
            roots.Add(u + v);
        }

        roots.Sort();
        // Remove duplicates within tolerance
        var uniqueRoots = new List<double>();
        foreach (var r in roots)
        {
            if (uniqueRoots.Count == 0 || Math.Abs(r - uniqueRoots[^1]) > 1e-5)
            {
                uniqueRoots.Add(r);
            }
        }

        var results = new List<CatastropheEquilibrium>();
        foreach (var x in uniqueRoots)
        {
            double pot = ComputeCuspPotential(x, a, b);
            double curv = ComputeCuspCurvature(x, a);
            bool isStable = curv > 1e-6;
            results.Add(new CatastropheEquilibrium(x, pot, curv, isStable));
        }

        return results;
    }

    /// <summary>
    /// Performs gradient relaxation to track state x under adiabatic parameter variation,
    /// reproducing realistic catastrophe jumps and hysteresis.
    /// dx/dt = -γ · V'(x)
    /// </summary>
    public double RelaxState(double currentX, double a, double b, double dt = 0.05, int iterations = 100)
    {
        double x = currentX;
        for (int i = 0; i < iterations; i++)
        {
            double grad = ComputeCuspDerivative(x, a, b);
            x -= dt * grad;
            // Prevent divergence
            if (double.IsNaN(x) || Math.Abs(x) > 10.0)
            {
                // Fallback to nearest stable equilibrium
                var eq = SolveCuspEquilibria(a, b);
                double bestX = 0;
                double minDist = double.MaxValue;
                foreach (var e in eq)
                {
                    if (e.IsStable && Math.Abs(e.X - currentX) < minDist)
                    {
                        minDist = Math.Abs(e.X - currentX);
                        bestX = e.X;
                    }
                }
                return bestX;
            }
        }
        return x;
    }

    /// <summary>
    /// Computes bifurcation curve points: 27b² + 4a³ = 0 => b = ± sqrt(-4a³/27) for a <= 0.
    /// </summary>
    public List<CuspBifurcationPoint> ComputeBifurcationCurve(double aMin = -4.0, double aMax = 0.0, int steps = 100)
    {
        var pts = new List<CuspBifurcationPoint>();
        double da = (aMax - aMin) / steps;
        for (int i = 0; i <= steps; i++)
        {
            double a = aMin + i * da;
            if (a > 0) continue;
            double bMag = Math.Sqrt(-4.0 * Math.Pow(a, 3) / 27.0);
            pts.Add(new CuspBifurcationPoint(a, bMag, -bMag));
        }
        return pts;
    }

    /// <summary>
    /// Computes potential for the Fold Catastrophe: V(x) = x³/3 + a x
    /// V'(x) = x² + a = 0.
    /// Stable for x = sqrt(-a), unstable for x = -sqrt(-a) when a < 0.
    /// </summary>
    public (double V, double VPrime, double VDoublePrime) EvaluateFold(double x, double a)
    {
        double v = (x * x * x) / 3.0 + a * x;
        double vp = x * x + a;
        double vpp = 2.0 * x;
        return (v, vp, vpp);
    }

    /// <summary>
    /// Computes potential for the Butterfly Catastrophe:
    /// V(x) = x⁶/6 + a x⁴/4 + b x³/3 + c x²/2 + d x
    /// Demonstrates tri-stability (three distinct stable potential wells!).
    /// </summary>
    public (double V, double VPrime, double VDoublePrime) EvaluateButterfly(double x, double a, double b, double c, double d)
    {
        double v = Math.Pow(x, 6) / 6.0 + a * Math.Pow(x, 4) / 4.0 + b * Math.Pow(x, 3) / 3.0 + c * Math.Pow(x, 2) / 2.0 + d * x;
        double vp = Math.Pow(x, 5) + a * Math.Pow(x, 3) + b * Math.Pow(x, 2) + c * x + d;
        double vpp = 5.0 * Math.Pow(x, 4) + 3.0 * a * Math.Pow(x, 2) + 2.0 * b * x + c;
        return (v, vp, vpp);
    }

    /// <summary>
    /// Simulates Zeeman's Catastrophe Machine: a wheel of radius R with a fixed spring
    /// and a control spring held at control point (a, b).
    /// </summary>
    public double ComputeZeemanPotential(double theta, double a, double b, double r0 = 1.0, double h = 2.0)
    {
        // Elastic potential energy of two springs connected to wheel perimeter (r0 cos theta, r0 sin theta)
        double px = r0 * Math.Cos(theta);
        double py = r0 * Math.Sin(theta);

        // Spring 1 anchored at (0, -h)
        double d1 = Math.Sqrt(px * px + Math.Pow(py + h, 2));
        // Spring 2 attached to control handle (a, b)
        double d2 = Math.Sqrt(Math.Pow(px - a, 2) + Math.Pow(py - b, 2));

        return 0.5 * Math.Pow(d1 - r0, 2) + 0.5 * Math.Pow(d2 - r0, 2);
    }
}
