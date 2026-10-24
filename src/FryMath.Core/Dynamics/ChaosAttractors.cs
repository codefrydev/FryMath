using System;
using System.Collections.Generic;
using FryMath.Core.LinearAlgebra;

namespace FryMath.Core.Dynamics;

public sealed class LorenzAttractorSimulation
{
    public double Sigma { get; set; } = 10.0;
    public double Rho { get; set; } = 28.0;
    public double Beta { get; set; } = 8.0 / 3.0;

    public Vector3D CurrentPosition { get; private set; }
    public List<Vector3D> TrajectoryHistory { get; } = new();
    public int MaxHistory { get; set; } = 1200;

    public LorenzAttractorSimulation(double x0 = 0.1, double y0 = 0.0, double z0 = 0.0)
    {
        CurrentPosition = new Vector3D(x0, y0, z0);
        TrajectoryHistory.Add(CurrentPosition);
    }

    public void Step(double dt)
    {
        // 4th Order Runge-Kutta (RK4)
        var p = CurrentPosition;

        var k1 = Derivatives(p);
        var k2 = Derivatives(p + k1 * (0.5 * dt));
        var k3 = Derivatives(p + k2 * (0.5 * dt));
        var k4 = Derivatives(p + k3 * dt);

        CurrentPosition = p + (k1 + k2 * 2.0 + k3 * 2.0 + k4) * (dt / 6.0);

        TrajectoryHistory.Add(CurrentPosition);
        if (TrajectoryHistory.Count > MaxHistory)
        {
            TrajectoryHistory.RemoveAt(0);
        }
    }

    private Vector3D Derivatives(Vector3D v)
    {
        double dx = Sigma * (v.Y - v.X);
        double dy = v.X * (Rho - v.Z) - v.Y;
        double dz = v.X * v.Y - Beta * v.Z;
        return new Vector3D(dx, dy, dz);
    }

    public void Reset(double x0 = 0.1, double y0 = 0.0, double z0 = 0.0)
    {
        CurrentPosition = new Vector3D(x0, y0, z0);
        TrajectoryHistory.Clear();
        TrajectoryHistory.Add(CurrentPosition);
    }

    /// <summary>
    /// Projects 3D attractor point onto 2D viewport with yaw/pitch rotation and screen centering.
    /// In Lorenz dynamics, Z is the vertical coordinate centered around 25.0.
    /// </summary>
    public static Vector2D Project(Vector3D pt, double yaw, double pitch, double scale, double screenCenterX, double screenCenterY, double zOffset = 25.0)
    {
        double cosY = Math.Cos(yaw);
        double sinY = Math.Sin(yaw);
        double cosP = Math.Cos(pitch);
        double sinP = Math.Sin(pitch);

        // Horizontal rotation (yaw around vertical Z axis)
        double x1 = pt.X * cosY - pt.Y * sinY;
        double y1 = pt.X * sinY + pt.Y * cosY;

        // Vertical tilt (pitch looking down onto Z-Y plane)
        double zc = pt.Z - zOffset;
        double yScreen = zc * cosP - y1 * sinP;

        return new Vector2D(screenCenterX + x1 * scale, screenCenterY - yScreen * scale);
    }
}
