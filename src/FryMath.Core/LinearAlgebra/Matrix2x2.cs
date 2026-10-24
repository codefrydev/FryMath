using System;
using System.Collections.Generic;

namespace FryMath.Core.LinearAlgebra;

public readonly record struct EigenPair(double Lambda, Vector2D Vector, bool IsReal);

public readonly record struct Matrix2x2(double M11, double M12, double M21, double M22)
{
    public static readonly Matrix2x2 Identity = new(1, 0, 0, 1);
    public static readonly Matrix2x2 Zero = new(0, 0, 0, 0);

    public double Determinant => M11 * M22 - M12 * M21;
    public double Trace => M11 + M22;

    public Vector2D Transform(Vector2D v) => new(
        M11 * v.X + M12 * v.Y,
        M21 * v.X + M22 * v.Y
    );

    public Matrix2x2 Multiply(Matrix2x2 other) => new(
        M11 * other.M11 + M12 * other.M21,
        M11 * other.M12 + M12 * other.M22,
        M21 * other.M11 + M22 * other.M21,
        M21 * other.M12 + M22 * other.M22
    );

    public Matrix2x2 Transpose() => new(M11, M21, M12, M22);

    public Matrix2x2? Inverse()
    {
        double det = Determinant;
        if (Math.Abs(det) < 1e-12) return null;

        double invDet = 1.0 / det;
        return new Matrix2x2(
            M22 * invDet, -M12 * invDet,
            -M21 * invDet, M11 * invDet
        );
    }

    public List<EigenPair> ComputeEigenPairs()
    {
        var list = new List<EigenPair>();
        double tr = Trace;
        double det = Determinant;

        double disc = tr * tr - 4.0 * det;

        if (disc >= 0)
        {
            double sqrtDisc = Math.Sqrt(disc);
            double lambda1 = (tr + sqrtDisc) * 0.5;
            double lambda2 = (tr - sqrtDisc) * 0.5;

            list.Add(new EigenPair(lambda1, FindEigenVector(lambda1), true));
            if (Math.Abs(disc) > 1e-9)
            {
                list.Add(new EigenPair(lambda2, FindEigenVector(lambda2), true));
            }
        }
        else
        {
            // Complex conjugate eigenvalues
            list.Add(new EigenPair(tr * 0.5, Vector2D.UnitX, false));
        }

        return list;
    }

    private Vector2D FindEigenVector(double lambda)
    {
        // (A - lambda*I) v = 0
        double a = M11 - lambda;
        double b = M12;
        double c = M21;
        double d = M22 - lambda;

        if (Math.Abs(b) > 1e-7)
        {
            return new Vector2D(-b, a).Normalize();
        }
        if (Math.Abs(c) > 1e-7)
        {
            return new Vector2D(-d, c).Normalize();
        }
        if (Math.Abs(a) < 1e-7)
        {
            return Vector2D.UnitX;
        }
        return Vector2D.UnitY;
    }

    // Factory methods
    public static Matrix2x2 Rotation(double radians)
    {
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        return new Matrix2x2(cos, -sin, sin, cos);
    }

    public static Matrix2x2 Scaling(double sx, double sy) => new(sx, 0, 0, sy);
    public static Matrix2x2 ShearX(double k) => new(1, k, 0, 1);
    public static Matrix2x2 ShearY(double k) => new(1, 0, k, 1);
    public static Matrix2x2 Reflection(double normalAngleRadians)
    {
        double cos2 = Math.Cos(2.0 * normalAngleRadians);
        double sin2 = Math.Sin(2.0 * normalAngleRadians);
        return new Matrix2x2(cos2, sin2, sin2, -cos2);
    }

    public static Matrix2x2 Interpolate(Matrix2x2 a, Matrix2x2 b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return new Matrix2x2(
            a.M11 + (b.M11 - a.M11) * t,
            a.M12 + (b.M12 - a.M12) * t,
            a.M21 + (b.M21 - a.M21) * t,
            a.M22 + (b.M22 - a.M22) * t
        );
    }

    public static Matrix2x2 operator *(Matrix2x2 a, Matrix2x2 b) => a.Multiply(b);
    public static Vector2D operator *(Matrix2x2 m, Vector2D v) => m.Transform(v);

    public override string ToString() => $"[[{M11:F2}, {M12:F2}], [{M21:F2}, {M22:F2}]]";
}
