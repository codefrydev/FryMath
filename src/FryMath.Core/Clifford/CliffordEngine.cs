using System;
using FryMath.Core.LinearAlgebra;

namespace FryMath.Core.Clifford;

/// <summary>
/// Oriented 2-blade (bivector) representing an oriented planar area element in 3D Euclidean Geometric Algebra G3.
/// Standard basis: e12 = e1*e2, e23 = e2*e3, e31 = e3*e1.
/// Satisfies (e_ij)^2 = -1 (geometric origin of imaginary unit i).
/// </summary>
public readonly record struct Bivector3D(double E12, double E23, double E31)
{
    public static readonly Bivector3D Zero = new(0, 0, 0);
    public static readonly Bivector3D Unit12 = new(1, 0, 0);
    public static readonly Bivector3D Unit23 = new(0, 1, 0);
    public static readonly Bivector3D Unit31 = new(0, 0, 1);

    public double MagnitudeSquared => E12 * E12 + E23 * E23 + E31 * E31;
    public double Magnitude => Math.Sqrt(MagnitudeSquared);

    /// <summary>
    /// Returns the unit bivector in the same plane orientation.
    /// </summary>
    public Bivector3D Normalize()
    {
        double mag = Magnitude;
        return mag > 1e-12 ? new Bivector3D(E12 / mag, E23 / mag, E31 / mag) : Zero;
    }

    /// <summary>
    /// Dual vector n = -I*B in G3, pointing normal to the oriented bivector plane.
    /// For B = e12, n = -I*e12 = e3 (the standard right-hand normal).
    /// </summary>
    public Vector3D Dual() => new(E23, E31, E12);

    /// <summary>
    /// Creates a bivector from its normal vector (Hodge dual inverse B = I*n).
    /// </summary>
    public static Bivector3D FromDual(Vector3D normal) => new(normal.Z, normal.X, normal.Y);

    public static Bivector3D operator +(Bivector3D a, Bivector3D b) =>
        new(a.E12 + b.E12, a.E23 + b.E23, a.E31 + b.E31);

    public static Bivector3D operator -(Bivector3D a, Bivector3D b) =>
        new(a.E12 - b.E12, a.E23 - b.E23, a.E31 - b.E31);

    public static Bivector3D operator -(Bivector3D a) =>
        new(-a.E12, -a.E23, -a.E31);

    public static Bivector3D operator *(Bivector3D b, double scalar) =>
        new(b.E12 * scalar, b.E23 * scalar, b.E31 * scalar);

    public static Bivector3D operator *(double scalar, Bivector3D b) =>
        new(b.E12 * scalar, b.E23 * scalar, b.E31 * scalar);

    public static Bivector3D operator /(Bivector3D b, double scalar) =>
        new(b.E12 / scalar, b.E23 / scalar, b.E31 / scalar);

    /// <summary>
    /// Commutator bracket [B1, B2] = 0.5 * (B1*B2 - B2*B1), generating the Lie algebra so(3).
    /// </summary>
    public Bivector3D Commutator(Bivector3D other)
    {
        // Vector cross product of their dual vectors mapped back to a bivector
        Vector3D n1 = Dual();
        Vector3D n2 = other.Dual();
        Vector3D cross = n1.Cross(n2);
        return FromDual(cross);
    }

    public override string ToString() =>
        $"{E12:F3} e12 + {E23:F3} e23 + {E31:F3} e31";
}

/// <summary>
/// General 8-component multivector in 3D Euclidean Clifford Geometric Algebra G3:
/// M = s + v1*e1 + v2*e2 + v3*e3 + b12*e12 + b23*e23 + b31*e31 + p*I.
/// Unifies scalars, vectors, bivectors, and trivectors under the associative geometric product.
/// </summary>
public readonly record struct Multivector3(
    double Scalar,
    double E1, double E2, double E3,
    double E12, double E23, double E31,
    double Pseudoscalar)
{
    public static readonly Multivector3 Zero = new(0, 0, 0, 0, 0, 0, 0, 0);
    public static readonly Multivector3 One = new(1, 0, 0, 0, 0, 0, 0, 0);

    public static readonly Multivector3 E1_Basis = new(0, 1, 0, 0, 0, 0, 0, 0);
    public static readonly Multivector3 E2_Basis = new(0, 0, 1, 0, 0, 0, 0, 0);
    public static readonly Multivector3 E3_Basis = new(0, 0, 0, 1, 0, 0, 0, 0);

    public static readonly Multivector3 E12_Basis = new(0, 0, 0, 0, 1, 0, 0, 0);
    public static readonly Multivector3 E23_Basis = new(0, 0, 0, 0, 0, 1, 0, 0);
    public static readonly Multivector3 E31_Basis = new(0, 0, 0, 0, 0, 0, 1, 0);

    public static readonly Multivector3 I_Basis = new(0, 0, 0, 0, 0, 0, 0, 1);

    public double Grade0 => Scalar;
    public Vector3D Grade1 => new(E1, E2, E3);
    public Bivector3D Grade2 => new(E12, E23, E31);
    public double Grade3 => Pseudoscalar;

    public Multivector3(double scalar) : this(scalar, 0, 0, 0, 0, 0, 0, 0) { }

    public Multivector3(Vector3D v) : this(0, v.X, v.Y, v.Z, 0, 0, 0, 0) { }

    public Multivector3(Bivector3D b) : this(0, 0, 0, 0, b.E12, b.E23, b.E31, 0) { }

    public Multivector3(double s, Vector3D v, Bivector3D b, double p) :
        this(s, v.X, v.Y, v.Z, b.E12, b.E23, b.E31, p) { }

    /// <summary>
    /// Multivector reversion M^dagger: reverses the order of vector factors.
    /// Grade 0 -> +1, Grade 1 -> +1, Grade 2 -> -1, Grade 3 -> -1.
    /// </summary>
    public Multivector3 Reverse() =>
        new(Scalar, E1, E2, E3, -E12, -E23, -E31, -Pseudoscalar);

    /// <summary>
    /// Grade involution (main automorphism): replaces e_i with -e_i.
    /// </summary>
    public Multivector3 GradeInvolution() =>
        new(Scalar, -E1, -E2, -E3, E12, E23, E31, -Pseudoscalar);

    /// <summary>
    /// Clifford conjugation: composition of grade involution and reversion.
    /// </summary>
    public Multivector3 CliffordConjugate() =>
        new(Scalar, -E1, -E2, -E3, -E12, -E23, -E31, Pseudoscalar);

    public double NormSquared =>
        Scalar * Scalar + E1 * E1 + E2 * E2 + E3 * E3 +
        E12 * E12 + E23 * E23 + E31 * E31 + Pseudoscalar * Pseudoscalar;

    public double Norm => Math.Sqrt(NormSquared);

    public Multivector3 Normalize()
    {
        double n = Norm;
        return n > 1e-12 ? this / n : Zero;
    }

    public static Multivector3 operator +(Multivector3 a, Multivector3 b) => new(
        a.Scalar + b.Scalar,
        a.E1 + b.E1, a.E2 + b.E2, a.E3 + b.E3,
        a.E12 + b.E12, a.E23 + b.E23, a.E31 + b.E31,
        a.Pseudoscalar + b.Pseudoscalar
    );

    public static Multivector3 operator -(Multivector3 a, Multivector3 b) => new(
        a.Scalar - b.Scalar,
        a.E1 - b.E1, a.E2 - b.E2, a.E3 - b.E3,
        a.E12 - b.E12, a.E23 - b.E23, a.E31 - b.E31,
        a.Pseudoscalar - b.Pseudoscalar
    );

    public static Multivector3 operator -(Multivector3 a) => new(
        -a.Scalar,
        -a.E1, -a.E2, -a.E3,
        -a.E12, -a.E23, -a.E31,
        -a.Pseudoscalar
    );

    public static Multivector3 operator *(Multivector3 a, double s) => new(
        a.Scalar * s,
        a.E1 * s, a.E2 * s, a.E3 * s,
        a.E12 * s, a.E23 * s, a.E31 * s,
        a.Pseudoscalar * s
    );

    public static Multivector3 operator *(double s, Multivector3 a) => a * s;

    public static Multivector3 operator /(Multivector3 a, double s) => new(
        a.Scalar / s,
        a.E1 / s, a.E2 / s, a.E3 / s,
        a.E12 / s, a.E23 / s, a.E31 / s,
        a.Pseudoscalar / s
    );

    /// <summary>
    /// Full closed-form Clifford Geometric Product A * B in G3.
    /// Unifies inner and outer products across all 8 basis blades.
    /// </summary>
    public static Multivector3 operator *(Multivector3 a, Multivector3 b)
    {
        // Scalar component (Grade 0)
        double s = a.Scalar * b.Scalar
                   + a.E1 * b.E1 + a.E2 * b.E2 + a.E3 * b.E3
                   - a.E12 * b.E12 - a.E23 * b.E23 - a.E31 * b.E31
                   - a.Pseudoscalar * b.Pseudoscalar;

        // Vector components (Grade 1)
        double e1 = a.Scalar * b.E1 + a.E1 * b.Scalar
                    - a.E2 * b.E12 + a.E12 * b.E2
                    + a.E3 * b.E31 - a.E31 * b.E3
                    - a.E23 * b.Pseudoscalar - a.Pseudoscalar * b.E23;

        double e2 = a.Scalar * b.E2 + a.E2 * b.Scalar
                    + a.E1 * b.E12 - a.E12 * b.E1
                    - a.E3 * b.E23 + a.E23 * b.E3
                    - a.E31 * b.Pseudoscalar - a.Pseudoscalar * b.E31;

        double e3 = a.Scalar * b.E3 + a.E3 * b.Scalar
                    + a.E2 * b.E23 - a.E23 * b.E2
                    - a.E1 * b.E31 + a.E31 * b.E1
                    - a.E12 * b.Pseudoscalar - a.Pseudoscalar * b.E12;

        // Bivector components (Grade 2)
        double e12 = a.Scalar * b.E12 + a.E12 * b.Scalar
                     + a.E1 * b.E2 - a.E2 * b.E1
                     - a.E23 * b.E31 + a.E31 * b.E23
                     + a.E3 * b.Pseudoscalar + a.Pseudoscalar * b.E3;

        double e23 = a.Scalar * b.E23 + a.E23 * b.Scalar
                     + a.E2 * b.E3 - a.E3 * b.E2
                     - a.E31 * b.E12 + a.E12 * b.E31
                     + a.E1 * b.Pseudoscalar + a.Pseudoscalar * b.E1;

        double e31 = a.Scalar * b.E31 + a.E31 * b.Scalar
                     + a.E3 * b.E1 - a.E1 * b.E3
                     - a.E12 * b.E23 + a.E23 * b.E12
                     + a.E2 * b.Pseudoscalar + a.Pseudoscalar * b.E2;

        // Pseudoscalar component (Grade 3)
        double p = a.Scalar * b.Pseudoscalar + a.Pseudoscalar * b.Scalar
                   + a.E1 * b.E23 + a.E23 * b.E1
                   + a.E2 * b.E31 + a.E31 * b.E2
                   + a.E3 * b.E12 + a.E12 * b.E3;

        return new Multivector3(s, e1, e2, e3, e12, e23, e31, p);
    }

    public override string ToString() =>
        $"[{Scalar:F2} + ({E1:F2}e1 + {E2:F2}e2 + {E3:F2}e3) + ({E12:F2}e12 + {E23:F2}e23 + {E31:F2}e31) + {Pseudoscalar:F2}I]";
}

/// <summary>
/// Rotor R = cos(theta/2) - B * sin(theta/2) in the even subalgebra G3+ (isomorphic to Quaternions).
/// Performs active, singularity-free, coordinate-free rotations via the sandwich product v' = R * v * R^dagger.
/// </summary>
public readonly record struct Rotor3D(double Scalar, Bivector3D Bivector)
{
    public static readonly Rotor3D Identity = new(1, Bivector3D.Zero);

    public Rotor3D Reverse => new(Scalar, -Bivector);

    public double NormSquared => Scalar * Scalar + Bivector.MagnitudeSquared;
    public double Norm => Math.Sqrt(NormSquared);

    /// <summary>
    /// Creates a rotor for rotation by angle theta around oriented unit bivector plane B.
    /// </summary>
    public static Rotor3D FromAngleAndPlane(double angleRadians, Bivector3D unitPlane)
    {
        double half = angleRadians * 0.5;
        double cos = Math.Cos(half);
        double sin = Math.Sin(half);
        // R = cos(theta/2) - B * sin(theta/2)
        return new Rotor3D(cos, -unitPlane.Normalize() * sin);
    }

    /// <summary>
    /// Creates a rotor that rotates unit vector u to unit vector v.
    /// R = (1 + v*u) / sqrt(2*(1 + u.v))
    /// </summary>
    public static Rotor3D FromVectorToVector(Vector3D u, Vector3D v)
    {
        Vector3D uNorm = u.Normalize();
        Vector3D vNorm = v.Normalize();
        double dot = uNorm.Dot(vNorm);

        if (dot >= 0.9999999)
            return Identity;

        if (dot <= -0.9999999)
        {
            // 180 degree rotation: pick an orthogonal vector
            Vector3D orth = Math.Abs(uNorm.X) < 0.8 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0);
            Vector3D normal = uNorm.Cross(orth).Normalize();
            return FromAngleAndPlane(Math.PI, Bivector3D.FromDual(normal));
        }

        // Wedge product u ^ v
        Bivector3D wedge = new(
            uNorm.X * vNorm.Y - uNorm.Y * vNorm.X,
            uNorm.Y * vNorm.Z - uNorm.Z * vNorm.Y,
            uNorm.Z * vNorm.X - uNorm.X * vNorm.Z
        );

        double s = Math.Sqrt(2 * (1.0 + dot));
        double scalar = (1.0 + dot) / s;
        Bivector3D bivec = -wedge / s;
        return new Rotor3D(scalar, bivec);
    }

    /// <summary>
    /// Rotates a 3D vector using the Clifford sandwich product v' = R * v * R^dagger.
    /// </summary>
    public Vector3D Rotate(Vector3D v)
    {
        Multivector3 rM = new(Scalar, 0, 0, 0, Bivector.E12, Bivector.E23, Bivector.E31, 0);
        Multivector3 vM = new(v);
        Multivector3 revM = rM.Reverse();

        Multivector3 result = rM * vM * revM;
        return result.Grade1;
    }

    /// <summary>
    /// Spherical Linear Interpolation (SLERP) between two rotors in G3.
    /// </summary>
    public static Rotor3D Slerp(Rotor3D r0, Rotor3D r1, double t)
    {
        double dot = r0.Scalar * r1.Scalar +
                     r0.Bivector.E12 * r1.Bivector.E12 +
                     r0.Bivector.E23 * r1.Bivector.E23 +
                     r0.Bivector.E31 * r1.Bivector.E31;

        Rotor3D r1Adjusted = r1;
        if (dot < 0)
        {
            dot = -dot;
            r1Adjusted = new Rotor3D(-r1.Scalar, -r1.Bivector);
        }

        if (dot > 0.9995)
        {
            // Linear blend for very small angles
            double s = r0.Scalar + t * (r1Adjusted.Scalar - r0.Scalar);
            Bivector3D b = r0.Bivector + (r1Adjusted.Bivector - r0.Bivector) * t;
            double len = Math.Sqrt(s * s + b.MagnitudeSquared);
            return new Rotor3D(s / len, b / len);
        }

        double theta0 = Math.Acos(Math.Clamp(dot, -1.0, 1.0));
        double theta = theta0 * t;
        double sinTheta0 = Math.Sin(theta0);
        double sinTheta = Math.Sin(theta);

        double s0 = Math.Cos(theta) - dot * sinTheta / sinTheta0;
        double s1 = sinTheta / sinTheta0;

        double finalScalar = r0.Scalar * s0 + r1Adjusted.Scalar * s1;
        Bivector3D finalBivec = r0.Bivector * s0 + r1Adjusted.Bivector * s1;
        return new Rotor3D(finalScalar, finalBivec);
    }
}

/// <summary>
/// Complete computational engine for Clifford Geometric Algebra & Multivector Calculus.
/// Integrates multivector products, wedge products, rotors, reflections, dualities, and physical field bivectors.
/// </summary>
public static class CliffordEngine
{
    /// <summary>
    /// Computes the geometric product a*b = a.b + a^b for two vectors.
    /// </summary>
    public static Multivector3 GeometricProduct(Vector3D a, Vector3D b)
    {
        double dot = a.Dot(b);
        Bivector3D wedge = Wedge(a, b);
        return new Multivector3(dot, 0, 0, 0, wedge.E12, wedge.E23, wedge.E31, 0);
    }

    /// <summary>
    /// Outer (wedge) product a ^ b = 0.5 * (a*b - b*a) generating an oriented bivector plane segment.
    /// </summary>
    public static Bivector3D Wedge(Vector3D a, Vector3D b) => new(
        a.X * b.Y - a.Y * b.X,
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z
    );

    /// <summary>
    /// Triple wedge product (trivector / oriented volume) a ^ b ^ c = (a x b) . c.
    /// Represents the signed volume of the parallelepiped spanned by the three vectors.
    /// </summary>
    public static double TrivectorVolume(Vector3D a, Vector3D b, Vector3D c)
    {
        Vector3D cross = a.Cross(b);
        return cross.Dot(c);
    }

    /// <summary>
    /// Single vector reflection across hyperplane orthogonal to unit vector n:
    /// v' = -n * v * n = v - 2*(v.n)*n.
    /// </summary>
    public static Vector3D Reflect(Vector3D v, Vector3D normal)
    {
        Vector3D n = normal.Normalize();
        return v - 2.0 * v.Dot(n) * n;
    }

    /// <summary>
    /// Cartan-Dieudonne Theorem: Any rotation is the composition of TWO reflections:
    /// v'' = -n2 * (-n1 * v * n1) * n2 = (n2 * n1) * v * (n1 * n2) = R * v * R^dagger.
    /// </summary>
    public static (Vector3D FinalVector, Rotor3D EquivalentRotor) DoubleReflect(
        Vector3D v, Vector3D n1, Vector3D n2)
    {
        Vector3D norm1 = n1.Normalize();
        Vector3D norm2 = n2.Normalize();

        Vector3D vReflectedOnce = Reflect(v, norm1);
        Vector3D vReflectedTwice = Reflect(vReflectedOnce, norm2);

        // Rotor is the geometric product of the two normals: R = n2 * n1
        Multivector3 rMultivec = GeometricProduct(norm2, norm1);
        Rotor3D rotor = new(rMultivec.Scalar, rMultivec.Grade2);

        return (vReflectedTwice, rotor);
    }

    /// <summary>
    /// Electromagnetic Field Bivector F = E + I*c*B.
    /// Computes the Lorentz invariants:
    /// 0.5 * F^2 = 0.5*(E^2 - c^2*B^2) + I*c*(E.B).
    /// </summary>
    public static (double LagrangianDensity, double TopologicalCharge) MaxwellInvariants(
        Vector3D E, Vector3D B, double c = 1.0)
    {
        double eMagSq = E.LengthSquared;
        double bMagSq = B.LengthSquared;

        double lagrangian = 0.5 * (eMagSq - c * c * bMagSq);
        double topoCharge = c * E.Dot(B);

        return (lagrangian, topoCharge);
    }

    /// <summary>
    /// Hyperbolic Lorentz boost rotor in Spacetime Algebra (STA G1,3):
    /// L = cosh(zeta/2) + e01 * sinh(zeta/2) with rapidity zeta = artanh(beta).
    /// </summary>
    public static (double CtPrime, double XPrime, double Gamma, double Rapidity) SpacetimeLorentzBoost(
        double ct, double x, double beta)
    {
        beta = Math.Clamp(beta, -0.999, 0.999);
        double gamma = 1.0 / Math.Sqrt(1.0 - beta * beta);
        double rapidity = 0.5 * Math.Log((1.0 + beta) / (1.0 - beta));

        double ctPrime = gamma * (ct - beta * x);
        double xPrime = gamma * (x - beta * ct);

        return (ctPrime, xPrime, gamma, rapidity);
    }

    /// <summary>
    /// Computes a set of polygon vertices representing the oriented bivector parallelogram in 3D.
    /// </summary>
    public static Vector3D[] BivectorParallelogram(Vector3D origin, Vector3D a, Vector3D b)
    {
        return new[]
        {
            origin,
            origin + a,
            origin + a + b,
            origin + b
        };
    }
}
