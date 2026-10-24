using System;

namespace FryMath.Core.LinearAlgebra;

public readonly record struct Vector2D(double X, double Y)
{
    public static readonly Vector2D Zero = new(0, 0);
    public static readonly Vector2D UnitX = new(1, 0);
    public static readonly Vector2D UnitY = new(0, 1);

    public double Length => Math.Sqrt(X * X + Y * Y);
    public double LengthSquared => X * X + Y * Y;
    public double Angle => Math.Atan2(Y, X);

    public Vector2D Normalize()
    {
        double len = Length;
        return len > 1e-12 ? new Vector2D(X / len, Y / len) : Zero;
    }

    public double Dot(Vector2D other) => X * other.X + Y * other.Y;

    // 2D pseudo cross-product (determinant of [v1, v2]): signed area
    public double Cross(Vector2D other) => X * other.Y - Y * other.X;

    public Vector2D Rotate(double radians)
    {
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        return new Vector2D(X * cos - Y * sin, X * sin + Y * cos);
    }

    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2D operator *(Vector2D a, double scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vector2D operator *(double scalar, Vector2D a) => new(a.X * scalar, a.Y * scalar);
    public static Vector2D operator /(Vector2D a, double scalar) => new(a.X / scalar, a.Y / scalar);
    public static Vector2D operator -(Vector2D a) => new(-a.X, -a.Y);

    public override string ToString() => $"({X:F3}, {Y:F3})";
}

public readonly record struct Vector3D(double X, double Y, double Z)
{
    public static readonly Vector3D Zero = new(0, 0, 0);
    public static readonly Vector3D UnitX = new(1, 0, 0);
    public static readonly Vector3D UnitY = new(0, 1, 0);
    public static readonly Vector3D UnitZ = new(0, 0, 1);

    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
    public double LengthSquared => X * X + Y * Y + Z * Z;

    public Vector3D Normalize()
    {
        double len = Length;
        return len > 1e-12 ? new Vector3D(X / len, Y / len, Z / len) : Zero;
    }

    public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3D Cross(Vector3D other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X
    );

    public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3D operator *(Vector3D a, double scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    public static Vector3D operator *(double scalar, Vector3D a) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    public static Vector3D operator /(Vector3D a, double scalar) => new(a.X / scalar, a.Y / scalar, a.Z / scalar);
    public static Vector3D operator -(Vector3D a) => new(-a.X, -a.Y, -a.Z);

    public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";
}
