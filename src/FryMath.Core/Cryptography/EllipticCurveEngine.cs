using System;
using System.Collections.Generic;

namespace FryMath.Core.Cryptography;

/// <summary>
/// A point on an elliptic curve over the real field R.
/// </summary>
public readonly record struct RealPoint(double X, double Y, bool IsInfinity = false)
{
    public static readonly RealPoint Infinity = new(0, 0, true);

    public override string ToString() => IsInfinity ? "O (Infinity)" : $"({X:F3}, {Y:F3})";
}

/// <summary>
/// A discrete point on an elliptic curve over a finite field F_p.
/// </summary>
public readonly record struct FinitePoint(long X, long Y, bool IsInfinity = false)
{
    public static readonly FinitePoint Infinity = new(0, 0, true);

    public override string ToString() => IsInfinity ? "O" : $"({X}, {Y})";
}

/// <summary>
/// Visualization mode for the Elliptic Curve & Cryptography laboratory.
/// </summary>
public enum CurveDisplayMode
{
    RealWeierstrass,
    FiniteFieldGrid,
    EcdhKeyExchange,
    EcdlpScalarScatter
}

/// <summary>
/// Mathematical engine for Weierstrass elliptic curves y^2 = x^3 + ax + b,
/// supporting exact real group addition, finite field F_p group law,
/// Diffie-Hellman key exchange, and scalar multiplication hopping trajectories.
/// </summary>
public sealed class EllipticCurveEngine
{
    // Curve parameters: y^2 = x^3 + a*x + b
    public double A { get; set; } = -3.0;
    public double B { get; set; } = 3.0;

    // Finite field parameters
    public long PrimeP { get; set; } = 97;
    public long FiniteA { get; set; } = -3;
    public long FiniteB { get; set; } = 7;

    // Real Points P and Q for interactive addition
    public RealPoint PointP { get; set; } = RealPoint.Infinity;
    public RealPoint PointQ { get; set; } = RealPoint.Infinity;

    // ECDH Key Exchange configuration
    public FinitePoint GeneratorG { get; set; } = FinitePoint.Infinity;
    public long AlicePrivateKey { get; set; } = 5;
    public long BobPrivateKey { get; set; } = 7;

    // ECDLP scalar multiplier
    public long ScalarK { get; set; } = 12;

    public CurveDisplayMode Mode { get; set; } = CurveDisplayMode.RealWeierstrass;

    public EllipticCurveEngine()
    {
        InitializeDefaultPoints();
    }

    public void InitializeDefaultPoints()
    {
        // Pick initial points on y^2 = x^3 - 3x + 3
        PointP = FindPointClosestToX(-1.5, positiveY: true);
        PointQ = FindPointClosestToX(1.5, positiveY: true);

        // Find generator on F_97 for secp256k1 demo (a=0, b=7 or standard a=-3, b=7)
        var points = GetFinitePoints(FiniteA, FiniteB, PrimeP);
        if (points.Count > 1)
        {
            // Pick a non-trivial generator
            GeneratorG = points[1];
        }
    }

    #region Real Weierstrass Arithmetic

    /// <summary>
    /// Computes the curve discriminant Delta = -16 * (4*a^3 + 27*b^2).
    /// Delta != 0 ensures the curve is smooth (non-singular).
    /// </summary>
    public double Discriminant => -16.0 * (4.0 * A * A * A + 27.0 * B * B);

    public bool IsSingular => Math.Abs(Discriminant) < 1e-9;

    /// <summary>
    /// Evaluates the right-hand side f(x) = x^3 + a*x + b.
    /// </summary>
    public double EvaluateRhs(double x) => x * x * x + A * x + B;

    /// <summary>
    /// Checks if a real point lies on the curve within numerical tolerance.
    /// </summary>
    public bool IsOnCurve(RealPoint p, double tolerance = 1e-4)
    {
        if (p.IsInfinity) return true;
        double lhs = p.Y * p.Y;
        double rhs = EvaluateRhs(p.X);
        return Math.Abs(lhs - rhs) <= tolerance;
    }

    /// <summary>
    /// Finds a valid real curve point closest to the specified x coordinate.
    /// </summary>
    public RealPoint FindPointClosestToX(double targetX, bool positiveY = true)
    {
        // If targetX has positive rhs, compute y directly
        double rhs = EvaluateRhs(targetX);
        if (rhs >= 0)
        {
            double y = Math.Sqrt(rhs);
            return new RealPoint(targetX, positiveY ? y : -y);
        }

        // Search for nearest valid x within range [-10, 10]
        double step = 0.02;
        for (double d = 0; d <= 10.0; d += step)
        {
            double xPlus = targetX + d;
            if (EvaluateRhs(xPlus) >= 0)
            {
                double y = Math.Sqrt(EvaluateRhs(xPlus));
                return new RealPoint(xPlus, positiveY ? y : -y);
            }

            double xMinus = targetX - d;
            if (EvaluateRhs(xMinus) >= 0)
            {
                double y = Math.Sqrt(EvaluateRhs(xMinus));
                return new RealPoint(xMinus, positiveY ? y : -y);
            }
        }

        return RealPoint.Infinity;
    }

    /// <summary>
    /// Adds two points on the real elliptic curve via the geometric chord-and-tangent group law.
    /// Returns the sum R = P + Q, along with the secant/tangent slope and intermediate intersection R'.
    /// </summary>
    public (RealPoint Sum, double Slope, double Intercept, RealPoint IntersectRPrime) AddReal(RealPoint p, RealPoint q)
    {
        // Identity cases
        if (p.IsInfinity) return (q, 0, 0, RealPoint.Infinity);
        if (q.IsInfinity) return (p, 0, 0, RealPoint.Infinity);

        // Case 1: P and Q have same X but opposite Y (vertical line -> sum is Infinity)
        if (Math.Abs(p.X - q.X) < 1e-6 && Math.Abs(p.Y + q.Y) < 1e-6)
        {
            return (RealPoint.Infinity, double.PositiveInfinity, p.X, RealPoint.Infinity);
        }

        double m;
        // Case 2: Point doubling P == Q
        if (Math.Abs(p.X - q.X) < 1e-6 && Math.Abs(p.Y - q.Y) < 1e-6)
        {
            if (Math.Abs(p.Y) < 1e-6)
            {
                // Tangent is vertical -> Infinity
                return (RealPoint.Infinity, double.PositiveInfinity, p.X, RealPoint.Infinity);
            }
            // Implicit differentiation: 2y dy = (3x^2 + a) dx -> m = (3x^2 + a) / (2y)
            m = (3.0 * p.X * p.X + A) / (2.0 * p.Y);
        }
        else
        {
            // Case 3: Distinct points P != Q (secant line)
            m = (q.Y - p.Y) / (q.X - p.X);
        }

        double c = p.Y - m * p.X;

        // Third intersection point X3' = m^2 - x1 - x2
        double x3 = m * m - p.X - q.X;
        double y3Prime = m * x3 + c; // Intermediate intersection R'
        double y3 = -y3Prime;        // Reflection across x-axis R = P + Q

        RealPoint rPrime = new(x3, y3Prime);
        RealPoint sum = new(x3, y3);

        return (sum, m, c, rPrime);
    }

    #endregion

    #region Finite Field F_p Arithmetic

    /// <summary>
    /// Computes non-negative modulo: a mod m in [0, m-1].
    /// </summary>
    public static long Mod(long a, long m)
    {
        long r = a % m;
        return r < 0 ? r + m : r;
    }

    /// <summary>
    /// Extended Euclidean Algorithm to compute modular inverse a^-1 mod m.
    /// Throws InvalidOperationException if gcd(a, m) != 1.
    /// </summary>
    public static long ModInverse(long a, long m)
    {
        a = Mod(a, m);
        if (a == 0) throw new InvalidOperationException("Zero has no modular inverse.");

        long m0 = m;
        long y = 0, x = 1;

        if (m == 1) return 0;

        while (a > 1)
        {
            long q = a / m;
            long t = m;
            m = a % m;
            a = t;
            t = y;
            y = x - q * y;
            x = t;
        }

        if (x < 0) x += m0;
        return x;
    }

    /// <summary>
    /// Modular exponentiation: (b^exp) mod m.
    /// </summary>
    public static long ModPow(long b, long exp, long m)
    {
        long res = 1;
        b = Mod(b, m);
        while (exp > 0)
        {
            if ((exp & 1) == 1) res = (long)((Int128)res * b % m);
            exp >>= 1;
            b = (long)((Int128)b * b % m);
        }
        return res;
    }

    /// <summary>
    /// Euler's criterion: a is a quadratic residue mod p iff a^((p-1)/2) = 1 (mod p).
    /// </summary>
    public static bool IsQuadraticResidue(long a, long p)
    {
        a = Mod(a, p);
        if (a == 0) return true;
        return ModPow(a, (p - 1) / 2, p) == 1;
    }

    /// <summary>
    /// Square root modulo prime p using Tonelli-Shanks algorithm.
    /// </summary>
    public static long? ModSqrt(long n, long p)
    {
        n = Mod(n, p);
        if (n == 0) return 0;
        if (p == 2) return n;
        if (!IsQuadraticResidue(n, p)) return null;

        // If p = 3 mod 4, direct formula: n^((p+1)/4) mod p
        if (p % 4 == 3)
        {
            return ModPow(n, (p + 1) / 4, p);
        }

        // Tonelli-Shanks for general prime p
        long q = p - 1;
        long s = 0;
        while ((q & 1) == 0)
        {
            s++;
            q >>= 1;
        }

        // Find non-residue z
        long z = 2;
        while (IsQuadraticResidue(z, p)) z++;

        long m = s;
        long c = ModPow(z, q, p);
        long t = ModPow(n, q, p);
        long r = ModPow(n, (q + 1) / 2, p);

        while (t != 1)
        {
            long t2 = t;
            long i = 0;
            for (i = 0; i < m; i++)
            {
                if (t2 == 1) break;
                t2 = (long)((Int128)t2 * t2 % p);
            }

            long b = ModPow(c, 1L << (int)(m - i - 1), p);
            m = i;
            c = (long)((Int128)b * b % p);
            t = (long)((Int128)t * c % p);
            r = (long)((Int128)r * b % p);
        }

        return r;
    }

    /// <summary>
    /// Enumerates all discrete points on the elliptic curve E(F_p): y^2 = x^3 + a*x + b mod p.
    /// </summary>
    public static List<FinitePoint> GetFinitePoints(long a, long b, long p)
    {
        var points = new List<FinitePoint> { FinitePoint.Infinity };

        for (long x = 0; x < p; x++)
        {
            long rhs = Mod(x * x % p * x + a * x + b, p);
            if (rhs == 0)
            {
                points.Add(new FinitePoint(x, 0));
            }
            else
            {
                long? root = ModSqrt(rhs, p);
                if (root.HasValue)
                {
                    long y1 = root.Value;
                    long y2 = Mod(p - y1, p);
                    points.Add(new FinitePoint(x, y1));
                    if (y1 != y2)
                    {
                        points.Add(new FinitePoint(x, y2));
                    }
                }
            }
        }

        return points;
    }

    /// <summary>
    /// Adds two finite points on E(F_p).
    /// </summary>
    public static FinitePoint AddFinite(FinitePoint p1, FinitePoint p2, long a, long p)
    {
        if (p1.IsInfinity) return p2;
        if (p2.IsInfinity) return p1;

        if (p1.X == p2.X && Mod(p1.Y + p2.Y, p) == 0)
        {
            return FinitePoint.Infinity;
        }

        long m;
        if (p1.X == p2.X && p1.Y == p2.Y)
        {
            // Point doubling: m = (3x^2 + a) * (2y)^-1 mod p
            if (p1.Y == 0) return FinitePoint.Infinity;
            long num = Mod(3 * p1.X % p * p1.X + a, p);
            long den = Mod(2 * p1.Y, p);
            m = Mod(num * ModInverse(den, p), p);
        }
        else
        {
            // Point addition: m = (y2 - y1) * (x2 - x1)^-1 mod p
            long num = Mod(p2.Y - p1.Y, p);
            long den = Mod(p2.X - p1.X, p);
            m = Mod(num * ModInverse(den, p), p);
        }

        // x3 = m^2 - x1 - x2 mod p
        long x3 = Mod(m * m - p1.X - p2.X, p);
        // y3 = m*(x1 - x3) - y1 mod p
        long y3 = Mod(m * (p1.X - x3) - p1.Y, p);

        return new FinitePoint(x3, y3);
    }

    /// <summary>
    /// Scalar multiplication k * P on E(F_p) via double-and-add algorithm.
    /// </summary>
    public static FinitePoint MultiplyFinite(FinitePoint p, long k, long a, long prime)
    {
        if (p.IsInfinity || k == 0) return FinitePoint.Infinity;
        if (k < 0)
        {
            k = -k;
            p = new FinitePoint(p.X, Mod(prime - p.Y, prime));
        }

        FinitePoint result = FinitePoint.Infinity;
        FinitePoint current = p;

        while (k > 0)
        {
            if ((k & 1) == 1)
            {
                result = AddFinite(result, current, a, prime);
            }
            current = AddFinite(current, current, a, prime);
            k >>= 1;
        }

        return result;
    }

    /// <summary>
    /// Generates the sequence of scalar multiples 1*G, 2*G, 3*G, ... k*G.
    /// Demonstrates the pseudo-random hopping trajectory of the discrete log problem.
    /// </summary>
    public List<FinitePoint> GetScalarHopTrajectory(FinitePoint g, long maxSteps)
    {
        var trajectory = new List<FinitePoint>();
        FinitePoint current = FinitePoint.Infinity;

        for (long i = 1; i <= maxSteps; i++)
        {
            current = AddFinite(current, g, FiniteA, PrimeP);
            trajectory.Add(current);
            if (current.IsInfinity) break; // Reached order of subgroup
        }

        return trajectory;
    }

    /// <summary>
    /// Executes the Diffie-Hellman Key Exchange steps:
    /// Alice chooses private key d_A -> public key Q_A = d_A * G
    /// Bob chooses private key d_B -> public key Q_B = d_B * G
    /// Alice computes shared secret S_A = d_A * Q_B
    /// Bob computes shared secret S_B = d_B * Q_A
    /// Verifies S_A == S_B.
    /// </summary>
    public (FinitePoint AlicePublic, FinitePoint BobPublic, FinitePoint SharedAlice, FinitePoint SharedBob, bool Success) RunEcdh()
    {
        if (GeneratorG.IsInfinity)
        {
            var pts = GetFinitePoints(FiniteA, FiniteB, PrimeP);
            if (pts.Count > 1) GeneratorG = pts[1];
        }

        var qA = MultiplyFinite(GeneratorG, AlicePrivateKey, FiniteA, PrimeP);
        var qB = MultiplyFinite(GeneratorG, BobPrivateKey, FiniteA, PrimeP);

        var sA = MultiplyFinite(qB, AlicePrivateKey, FiniteA, PrimeP);
        var sB = MultiplyFinite(qA, BobPrivateKey, FiniteA, PrimeP);

        bool success = (sA == sB) && !sA.IsInfinity;
        return (qA, qB, sA, sB, success);
    }

    #endregion

    #region Presets

    public void ApplySecp256k1Preset()
    {
        // secp256k1: y^2 = x^3 + 7
        A = 0;
        B = 7;
        FiniteA = 0;
        FiniteB = 7;
        PrimeP = 97;
        PointP = FindPointClosestToX(1.0, positiveY: true);
        PointQ = FindPointClosestToX(2.0, positiveY: true);
        var pts = GetFinitePoints(FiniteA, FiniteB, PrimeP);
        if (pts.Count > 2) GeneratorG = pts[2];
        Mode = CurveDisplayMode.RealWeierstrass;
    }

    public void ApplyTwoComponentsPreset()
    {
        // y^2 = x^3 - 3x + 1 (Delta > 0: isolated egg and infinite branch)
        A = -3;
        B = 1;
        FiniteA = -3;
        FiniteB = 1;
        PrimeP = 89;
        PointP = FindPointClosestToX(-1.5, positiveY: true); // On the egg
        PointQ = FindPointClosestToX(1.5, positiveY: true);  // On the open branch
        var pts = GetFinitePoints(FiniteA, FiniteB, PrimeP);
        if (pts.Count > 1) GeneratorG = pts[1];
        Mode = CurveDisplayMode.RealWeierstrass;
    }

    public void ApplyNodalSingularPreset()
    {
        // Nodal curve: y^2 = x^3 + x^2 (Delta = 0, self-intersection at origin)
        // Shifted to Weierstrass form approx or pure x^3 - 3x + 2
        A = -3;
        B = 2; // Delta = -16 * (4*(-27) + 27*4) = 0! Node at x=1
        FiniteA = -3;
        FiniteB = 2;
        PrimeP = 97;
        PointP = new RealPoint(-2.0, 0.0);
        PointQ = new RealPoint(2.0, 2.0);
        Mode = CurveDisplayMode.RealWeierstrass;
    }

    public void ApplyCuspSingularPreset()
    {
        // Cusp curve: y^2 = x^3 (Delta = 0, cusp at origin)
        A = 0;
        B = 0;
        FiniteA = 0;
        FiniteB = 0;
        PrimeP = 97;
        PointP = new RealPoint(1.0, 1.0);
        PointQ = new RealPoint(4.0, 8.0);
        Mode = CurveDisplayMode.RealWeierstrass;
    }

    public void ApplyEcdhPreset()
    {
        FiniteA = -3;
        FiniteB = 7;
        PrimeP = 89;
        AlicePrivateKey = 5;
        BobPrivateKey = 7;
        var pts = GetFinitePoints(FiniteA, FiniteB, PrimeP);
        if (pts.Count > 3) GeneratorG = pts[3];
        Mode = CurveDisplayMode.EcdhKeyExchange;
    }

    public void ApplyEcdlpScatterPreset()
    {
        FiniteA = 0;
        FiniteB = 7;
        PrimeP = 101;
        ScalarK = 25;
        var pts = GetFinitePoints(FiniteA, FiniteB, PrimeP);
        if (pts.Count > 2) GeneratorG = pts[2];
        Mode = CurveDisplayMode.EcdlpScalarScatter;
    }

    #endregion
}
