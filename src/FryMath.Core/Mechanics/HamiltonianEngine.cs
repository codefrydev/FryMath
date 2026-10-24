using System;
using System.Collections.Generic;
using System.Linq;

namespace FryMath.Core.Mechanics;

/// <summary>
/// Symplectic Geometry, Hamiltonian Mechanics & Phase Space Invariants Engine.
/// Implements canonical Hamiltonian systems (Hénon-Heiles, Duffing double-well, Nonlinear Pendulum),
/// Symplectic integrators (Störmer-Verlet, Yoshida 4th-order) vs non-symplectic RK4,
/// Liouville phase space volume conservation via Green's theorem,
/// and Poincaré surfaces of section.
/// </summary>
public static class HamiltonianEngine
{
    public record PhasePoint2D(double Q, double P);
    public record PhasePoint4D(double X, double Y, double Px, double Py);

    public record SimulationStepResult(
        double Time,
        PhasePoint4D State,
        double Energy,
        double EnergyError,
        double PhaseVolumeArea);

    public record PoincarePoint(double Y, double Py);

    // -------------------------------------------------------------
    // Canonical Hamiltonian Systems
    // -------------------------------------------------------------

    /// <summary>
    /// Hénon-Heiles Hamiltonian:
    /// H = 1/2(px² + py²) + 1/2(x² + y²) + x²y - 1/3 y³
    /// Escape energy Ec = 1/6 ≈ 0.16667.
    /// </summary>
    public static double EvaluateHenonHeilesH(double x, double y, double px, double py)
    {
        double kinetic = 0.5 * (px * px + py * py);
        double potential = 0.5 * (x * x + y * y) + x * x * y - (1.0 / 3.0) * y * y * y;
        return kinetic + potential;
    }

    public static (double dH_dx, double dH_dy) HenonHeilesGradV(double x, double y)
    {
        double dH_dx = x + 2.0 * x * y;
        double dH_dy = y + x * x - y * y;
        return (dH_dx, dH_dy);
    }

    /// <summary>
    /// Duffing double-well Hamiltonian:
    /// H = 1/2 p² - 1/2 q² + 1/4 q⁴
    /// Potential wells at q = ±1 with V = -0.25. Separatrix energy H = 0.
    /// </summary>
    public static double EvaluateDuffingH(double q, double p)
    {
        return 0.5 * p * p - 0.5 * q * q + 0.25 * q * q * q * q;
    }

    public static double DuffingGradV(double q)
    {
        return -q + q * q * q;
    }

    /// <summary>
    /// Nonlinear Pendulum Hamiltonian:
    /// H = 1/2 p² - cos(q)
    /// Stable fixed point at (0, 0), saddle at (±pi, 0).
    /// </summary>
    public static double EvaluatePendulumH(double q, double p)
    {
        return 0.5 * p * p - Math.Cos(q);
    }

    public static double PendulumGradV(double q)
    {
        return Math.Sin(q);
    }

    // -------------------------------------------------------------
    // Symplectic vs Non-Symplectic Integrators
    // -------------------------------------------------------------

    /// <summary>
    /// Störmer-Verlet (Leapfrog) 2nd-order symplectic integrator.
    /// Exactly preserves symplectic 2-form dq ∧ dp = const.
    /// </summary>
    public static PhasePoint4D StepStormerVerlet4D(PhasePoint4D s, double dt, Func<double, double, (double gx, double gy)> gradV)
    {
        // Half kick
        var (gx1, gy1) = gradV(s.X, s.Y);
        double pxHalf = s.Px - 0.5 * dt * gx1;
        double pyHalf = s.Py - 0.5 * dt * gy1;

        // Full drift
        double xNew = s.X + dt * pxHalf;
        double yNew = s.Y + dt * pyHalf;

        // Half kick
        var (gx2, gy2) = gradV(xNew, yNew);
        double pxNew = pxHalf - 0.5 * dt * gx2;
        double pyNew = pyHalf - 0.5 * dt * gy2;

        return new PhasePoint4D(xNew, yNew, pxNew, pyNew);
    }

    /// <summary>
    /// Yoshida 4th-order Symplectic Integrator (Haruo Yoshida 1990).
    /// Preserves the symplectic form with O(dt⁴) global accuracy and zero secular energy drift.
    /// </summary>
    public static PhasePoint4D StepYoshida4D(PhasePoint4D s, double dt, Func<double, double, (double gx, double gy)> gradV)
    {
        double cbrt2 = Math.Cbrt(2.0);
        double w1 = 1.0 / (2.0 - cbrt2);
        double w0 = -cbrt2 / (2.0 - cbrt2);

        double[] c = { w1 * 0.5, (w0 + w1) * 0.5, (w0 + w1) * 0.5, w1 * 0.5 };
        double[] d = { w1, w0, w1 };

        double x = s.X;
        double y = s.Y;
        double px = s.Px;
        double py = s.Py;

        // Step 1
        x += c[0] * dt * px;
        y += c[0] * dt * py;

        var (gx1, gy1) = gradV(x, y);
        px -= d[0] * dt * gx1;
        py -= d[0] * dt * gy1;

        // Step 2
        x += c[1] * dt * px;
        y += c[1] * dt * py;

        var (gx2, gy2) = gradV(x, y);
        px -= d[1] * dt * gx2;
        py -= d[1] * dt * gy2;

        // Step 3
        x += c[2] * dt * px;
        y += c[2] * dt * py;

        var (gx3, gy3) = gradV(x, y);
        px -= d[2] * dt * gx3;
        py -= d[2] * dt * gy3;

        // Step 4
        x += c[3] * dt * px;
        y += c[3] * dt * py;

        return new PhasePoint4D(x, y, px, py);
    }

    /// <summary>
    /// Standard Classical 4th-Order Runge-Kutta (RK4).
    /// Highly accurate locally but NON-SYMPLECTIC: fails to preserve phase volume and suffers artificial energy drift.
    /// </summary>
    public static PhasePoint4D StepRungeKutta4D(PhasePoint4D s, double dt, Func<double, double, (double gx, double gy)> gradV)
    {
        // k1
        double dx1 = s.Px;
        double dy1 = s.Py;
        var (gx1, gy1) = gradV(s.X, s.Y);
        double dpx1 = -gx1;
        double dpy1 = -gy1;

        // k2
        double x2 = s.X + 0.5 * dt * dx1;
        double y2 = s.Y + 0.5 * dt * dy1;
        double px2 = s.Px + 0.5 * dt * dpx1;
        double py2 = s.Py + 0.5 * dt * dpy1;
        double dx2 = px2;
        double dy2 = py2;
        var (gx2, gy2) = gradV(x2, y2);
        double dpx2 = -gx2;
        double dpy2 = -gy2;

        // k3
        double x3 = s.X + 0.5 * dt * dx2;
        double y3 = s.Y + 0.5 * dt * dy2;
        double px3 = s.Px + 0.5 * dt * dpx2;
        double py3 = s.Py + 0.5 * dt * dpy2;
        double dx3 = px3;
        double dy3 = py3;
        var (gx3, gy3) = gradV(x3, y3);
        double dpx3 = -gx3;
        double dpy3 = -gy3;

        // k4
        double x4 = s.X + dt * dx3;
        double y4 = s.Y + dt * dy3;
        double px4 = s.Px + dt * dpx3;
        double py4 = s.Py + dt * dpy3;
        double dx4 = px4;
        double dy4 = py4;
        var (gx4, gy4) = gradV(x4, y4);
        double dpx4 = -gx4;
        double dpy4 = -gy4;

        double xNew = s.X + (dt / 6.0) * (dx1 + 2.0 * dx2 + 2.0 * dx3 + dx4);
        double yNew = s.Y + (dt / 6.0) * (dy1 + 2.0 * dy2 + 2.0 * dy3 + dy4);
        double pxNew = s.Px + (dt / 6.0) * (dpx1 + 2.0 * dpx2 + 2.0 * dpx3 + dpx4);
        double pyNew = s.Py + (dt / 6.0) * (dpy1 + 2.0 * dpy2 + 2.0 * dpy3 + dpy4);

        return new PhasePoint4D(xNew, yNew, pxNew, pyNew);
    }

    // -------------------------------------------------------------
    // Liouville Phase Space Volume Conservation
    // -------------------------------------------------------------

    /// <summary>
    /// Generates an ensemble of points on an initial circular boundary in phase space (q, p).
    /// </summary>
    public static List<PhasePoint2D> GeneratePhaseLoop(double centerQ, double centerP, double radius, int count = 64)
    {
        var loop = new List<PhasePoint2D>(count);
        double dt = (2.0 * Math.PI) / count;
        for (int i = 0; i < count; i++)
        {
            double angle = i * dt;
            loop.Add(new PhasePoint2D(
                centerQ + radius * Math.Cos(angle),
                centerP + radius * Math.Sin(angle)
            ));
        }
        return loop;
    }

    /// <summary>
    /// Computes the enclosed phase area of a closed loop in (q, p) via Green's Theorem:
    /// Area = 1/2 ∮ (q dp - p dq).
    /// By Liouville's theorem, this area is strictly invariant under Hamiltonian flow.
    /// </summary>
    public static double ComputePhaseEnclosedArea(IReadOnlyList<PhasePoint2D> loop)
    {
        int n = loop.Count;
        if (n < 3) return 0.0;

        double area2 = 0.0;
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            area2 += loop[i].Q * loop[next].P - loop[next].Q * loop[i].P;
        }

        return 0.5 * Math.Abs(area2);
    }

    /// <summary>
    /// Evolves a phase space loop by one time step dt under a 1D Hamiltonian (e.g. Pendulum or Duffing).
    /// </summary>
    public static List<PhasePoint2D> StepPhaseLoop(IReadOnlyList<PhasePoint2D> loop, double dt, Func<double, double> gradV, bool useSymplectic = true)
    {
        var nextLoop = new List<PhasePoint2D>(loop.Count);

        foreach (var pt in loop)
        {
            if (useSymplectic)
            {
                // Leapfrog 1D
                double pMid = pt.P - 0.5 * dt * gradV(pt.Q);
                double qNew = pt.Q + dt * pMid;
                double pNew = pMid - 0.5 * dt * gradV(qNew);
                nextLoop.Add(new PhasePoint2D(qNew, pNew));
            }
            else
            {
                // RK4 1D
                double dq1 = pt.P;
                double dp1 = -gradV(pt.Q);

                double q2 = pt.Q + 0.5 * dt * dq1;
                double p2 = pt.P + 0.5 * dt * dp1;
                double dq2 = p2;
                double dp2 = -gradV(q2);

                double q3 = pt.Q + 0.5 * dt * dq2;
                double p3 = pt.P + 0.5 * dt * dp2;
                double dq3 = p3;
                double dp3 = -gradV(q3);

                double q4 = pt.Q + dt * dq3;
                double p4 = pt.P + dt * dp3;
                double dq4 = p4;
                double dp4 = -gradV(q4);

                double qNew = pt.Q + (dt / 6.0) * (dq1 + 2.0 * dq2 + 2.0 * dq3 + dq4);
                double pNew = pt.P + (dt / 6.0) * (dp1 + 2.0 * dp2 + 2.0 * dp3 + dp4);
                nextLoop.Add(new PhasePoint2D(qNew, pNew));
            }
        }

        return nextLoop;
    }

    // -------------------------------------------------------------
    // Poincaré Surface of Section & Chaos Detection
    // -------------------------------------------------------------

    /// <summary>
    /// Checks for a crossing of the Poincaré section x = 0 with px > 0.
    /// If a crossing occurred between sOld and sNew, interpolates the intersection (y, py).
    /// </summary>
    public static PoincarePoint? DetectPoincareCrossing(PhasePoint4D sOld, PhasePoint4D sNew)
    {
        // Check zero crossing from negative to positive: x_old <= 0 and x_new > 0
        if (sOld.X <= 0.0 && sNew.X > 0.0 && sNew.Px > 0.0)
        {
            double fraction = -sOld.X / (sNew.X - sOld.X);
            double yCross = sOld.Y + fraction * (sNew.Y - sOld.Y);
            double pyCross = sOld.Py + fraction * (sNew.Py - sOld.Py);
            return new PoincarePoint(yCross, pyCross);
        }

        return null;
    }

    /// <summary>
    /// Canonical Poisson Bracket {f, g}:
    /// {f, g} = (∂f/∂q)(∂g/∂p) - (∂f/∂p)(∂g/∂q).
    /// </summary>
    public static double EvaluatePoissonBracket2D(
        double df_dq, double df_dp,
        double dg_dq, double dg_dp)
    {
        return df_dq * dg_dp - df_dp * dg_dq;
    }
}
