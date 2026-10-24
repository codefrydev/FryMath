using System;
using System.Numerics;

namespace FryMath.Core.Geometry;

public enum SpaceCurvePreset
{
    CircularHelix,
    TrefoilKnot,
    VivianiCurve,
    TorusKnot,
    TwistedCubic,
    ConicalSpiral
}

public sealed record FrenetFrame(
    Vector3 Position,
    Vector3 Tangent,
    Vector3 Normal,
    Vector3 Binormal,
    double Curvature,
    double RadiusOfCurvature,
    double Torsion,
    double Speed,
    double ArcLength
);

public static class SpaceCurveEngine
{
    public static (double X, double Y, double Z) EvaluateCurveDouble(SpaceCurvePreset preset, double t)
    {
        return preset switch
        {
            SpaceCurvePreset.CircularHelix => (2.0 * Math.Cos(t), 2.0 * Math.Sin(t), 0.6 * t - 0.6 * 2.0 * Math.PI),
            SpaceCurvePreset.TrefoilKnot => (Math.Sin(t) + 2.0 * Math.Sin(2.0 * t), Math.Cos(t) - 2.0 * Math.Cos(2.0 * t), -2.0 * Math.Sin(3.0 * t)),
            SpaceCurvePreset.VivianiCurve => (2.0 * (1.0 + Math.Cos(t)), 2.0 * Math.Sin(t), 4.0 * Math.Sin(t * 0.5)),
            SpaceCurvePreset.TorusKnot => ((2.5 + 1.0 * Math.Cos(3.0 * t)) * Math.Cos(2.0 * t), (2.5 + 1.0 * Math.Cos(3.0 * t)) * Math.Sin(2.0 * t), 1.5 * Math.Sin(3.0 * t)),
            SpaceCurvePreset.TwistedCubic => (t * 1.5, t * t * 0.8 - 1.2, t * t * t * 0.4),
            SpaceCurvePreset.ConicalSpiral => (t * 0.22 * Math.Cos(t * 2.0), t * 0.22 * Math.Sin(t * 2.0), t * 0.35 - 2.0),
            _ => (2.0 * Math.Cos(t), 2.0 * Math.Sin(t), 0.6 * t)
        };
    }

    public static Vector3 EvaluateCurve(SpaceCurvePreset preset, double t)
    {
        var (x, y, z) = EvaluateCurveDouble(preset, t);
        return new Vector3((float)x, (float)y, (float)z);
    }

    public static (double TMin, double TMax) GetParameterRange(SpaceCurvePreset preset)
    {
        return preset switch
        {
            SpaceCurvePreset.CircularHelix => (0.0, 4.0 * Math.PI),
            SpaceCurvePreset.TrefoilKnot => (0.0, 2.0 * Math.PI),
            SpaceCurvePreset.VivianiCurve => (0.0, 4.0 * Math.PI),
            SpaceCurvePreset.TorusKnot => (0.0, 2.0 * Math.PI),
            SpaceCurvePreset.TwistedCubic => (-1.8, 1.8),
            SpaceCurvePreset.ConicalSpiral => (0.0, 4.0 * Math.PI),
            _ => (0.0, 2.0 * Math.PI)
        };
    }

    public static FrenetFrame ComputeFrenetFrame(SpaceCurvePreset preset, double t)
    {
        const double h = 1e-4;

        var r = EvaluateCurveDouble(preset, t);
        var rF = EvaluateCurveDouble(preset, t + h);
        var rB = EvaluateCurveDouble(preset, t - h);

        var rF2 = EvaluateCurveDouble(preset, t + 2.0 * h);
        var rB2 = EvaluateCurveDouble(preset, t - 2.0 * h);

        // Velocity r'(t)
        double vx = (rF.X - rB.X) / (2.0 * h);
        double vy = (rF.Y - rB.Y) / (2.0 * h);
        double vz = (rF.Z - rB.Z) / (2.0 * h);
        double speed = Math.Sqrt(vx * vx + vy * vy + vz * vz);
        if (speed < 1e-9) speed = 1e-9;

        // Acceleration r''(t)
        double ax = (rF.X - 2.0 * r.X + rB.X) / (h * h);
        double ay = (rF.Y - 2.0 * r.Y + rB.Y) / (h * h);
        double az = (rF.Z - 2.0 * r.Z + rB.Z) / (h * h);

        // Jerk r'''(t)
        double jx = (rF2.X - 2.0 * rF.X + 2.0 * rB.X - rB2.X) / (2.0 * h * h * h);
        double jy = (rF2.Y - 2.0 * rF.Y + 2.0 * rB.Y - rB2.Y) / (2.0 * h * h * h);
        double jz = (rF2.Z - 2.0 * rF.Z + 2.0 * rB.Z - rB2.Z) / (2.0 * h * h * h);

        // Cross product v x a
        double cx = vy * az - vz * ay;
        double cy = vz * ax - vx * az;
        double cz = vx * ay - vy * ax;
        double cLen = Math.Sqrt(cx * cx + cy * cy + cz * cz);

        // Curvature kappa = ||v x a|| / ||v||^3
        double curvature = cLen / (speed * speed * speed);
        double radius = curvature > 1e-6 ? 1.0 / curvature : 1000.0;

        // Unit Tangent T = v / ||v||
        Vector3 tangent = new((float)(vx / speed), (float)(vy / speed), (float)(vz / speed));

        Vector3 binormal;
        Vector3 normal;

        if (cLen > 1e-7)
        {
            // Unit Binormal B = (v x a) / ||v x a||
            binormal = new((float)(cx / cLen), (float)(cy / cLen), (float)(cz / cLen));
            // Unit Normal N = B x T
            normal = Vector3.Normalize(Vector3.Cross(binormal, tangent));
        }
        else
        {
            binormal = Vector3.UnitZ;
            normal = Vector3.UnitY;
        }

        // Torsion tau = (v x a) . r''' / ||v x a||^2
        double torsion = 0.0;
        if (cLen > 1e-7)
        {
            torsion = (cx * jx + cy * jy + cz * jz) / (cLen * cLen);
        }

        // Numerical arc length quadrature
        var (tMin, _) = GetParameterRange(preset);
        int steps = 80;
        double dt = (t - tMin) / steps;
        double s = 0.0;
        for (int i = 0; i < steps; i++)
        {
            double ti = tMin + (i + 0.5) * dt;
            var r1 = EvaluateCurveDouble(preset, ti - 0.5 * h);
            var r2 = EvaluateCurveDouble(preset, ti + 0.5 * h);
            double vxi = (r2.X - r1.X) / h;
            double vyi = (r2.Y - r1.Y) / h;
            double vzi = (r2.Z - r1.Z) / h;
            s += Math.Sqrt(vxi * vxi + vyi * vyi + vzi * vzi) * dt;
        }

        return new FrenetFrame(
            Position: new Vector3((float)r.X, (float)r.Y, (float)r.Z),
            Tangent: tangent,
            Normal: normal,
            Binormal: binormal,
            Curvature: curvature,
            RadiusOfCurvature: radius,
            Torsion: torsion,
            Speed: speed,
            ArcLength: s
        );
    }
}
