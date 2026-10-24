using System;
using System.Collections.Generic;

namespace FryMath.Core.Hyperbolic;

public enum HyperbolicModel
{
    PoincareDisk,
    KleinBeltrami,
    UpperHalfPlane
}

public enum HyperbolicTilingPreset
{
    Heptagonal7_3,
    Octagonal8_3,
    Order5Square4_5,
    Triangular3_7,
    IdealTriangle,
    GeodesicHolonomy
}

public sealed record HyperbolicPoint(double X, double Y);

public sealed record HyperbolicArc(
    HyperbolicPoint P1,
    HyperbolicPoint P2,
    bool IsDiameter,
    double CenterX = 0,
    double CenterY = 0,
    double Radius = 0,
    double StartAngle = 0,
    double SweepAngle = 0
);

public sealed record HyperbolicTriangle(
    HyperbolicPoint A,
    HyperbolicPoint B,
    HyperbolicPoint C,
    double AngleA,
    double AngleB,
    double AngleC,
    double HyperbolicArea,
    double AngleDefect
);

public class HyperbolicGeometryEngine
{
    public HyperbolicModel Model { get; set; } = HyperbolicModel.PoincareDisk;
    public HyperbolicTilingPreset Preset { get; private set; } = HyperbolicTilingPreset.Heptagonal7_3;

    public List<HyperbolicArc> Geodesics { get; } = new();
    public List<HyperbolicPoint> Vertices { get; } = new();
    public HyperbolicTriangle? ActiveTriangle { get; private set; }

    public int TilingDepth { get; set; } = 3;
    public double ViewOffsetX { get; set; } = 0.0;
    public double ViewOffsetY { get; set; } = 0.0;

    public HyperbolicGeometryEngine()
    {
        LoadPreset(HyperbolicTilingPreset.Heptagonal7_3);
    }

    public void LoadPreset(HyperbolicTilingPreset preset)
    {
        Preset = preset;
        Geodesics.Clear();
        Vertices.Clear();
        ActiveTriangle = null;

        switch (preset)
        {
            case HyperbolicTilingPreset.Heptagonal7_3:
                GenerateRegularTiling(7, 3, TilingDepth);
                break;
            case HyperbolicTilingPreset.Octagonal8_3:
                GenerateRegularTiling(8, 3, TilingDepth);
                break;
            case HyperbolicTilingPreset.Order5Square4_5:
                GenerateRegularTiling(4, 5, TilingDepth);
                break;
            case HyperbolicTilingPreset.Triangular3_7:
                GenerateRegularTiling(3, 7, TilingDepth);
                break;
            case HyperbolicTilingPreset.IdealTriangle:
                GenerateIdealTriangle();
                break;
            case HyperbolicTilingPreset.GeodesicHolonomy:
                GenerateHolonomyDemo();
                break;
        }
    }

    public static double HyperbolicDistance(HyperbolicPoint p1, HyperbolicPoint p2)
    {
        // d(z1, z2) = 2 * artanh( |z1 - z2| / |1 - z1 * conj(z2)| )
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        double numSq = dx * dx + dy * dy;

        // 1 - z1 * conj(z2) = 1 - (x1 + i*y1)(x2 - i*y2) = 1 - (x1*x2 + y1*y2) - i*(y1*x2 - x1*y2)
        double realPart = 1.0 - (p1.X * p2.X + p1.Y * p2.Y);
        double imagPart = -(p1.Y * p2.X - p1.X * p2.Y);
        double denSq = realPart * realPart + imagPart * imagPart;

        if (denSq <= 1e-12) return 20.0; // Near boundary horizon

        double ratio = Math.Sqrt(numSq / denSq);
        if (ratio >= 0.999999) ratio = 0.999999;

        // 2 * artanh(r) = ln( (1 + r) / (1 - r) )
        return Math.Log((1.0 + ratio) / (1.0 - ratio));
    }

    public static HyperbolicArc ConstructGeodesicArc(HyperbolicPoint p1, HyperbolicPoint p2)
    {
        // Check if points are collinear with origin (geodesic is a Euclidean diameter)
        double cross = p1.X * p2.Y - p1.Y * p2.X;
        if (Math.Abs(cross) < 1e-7)
        {
            return new HyperbolicArc(p1, p2, IsDiameter: true);
        }

        // Circular arc orthogonal to unit circle x² + y² = 1 passing through p1 and p2.
        // Inversion in unit circle leaves the orthogonal circle invariant.
        // The inverse of p1 is p1* = p1 / |p1|².
        double norm1Sq = p1.X * p1.X + p1.Y * p1.Y;
        if (norm1Sq < 1e-7)
        {
            // If p1 is origin, geodesic is diameter
            return new HyperbolicArc(p1, p2, IsDiameter: true);
        }

        double invP1X = p1.X / norm1Sq;
        double invP1Y = p1.Y / norm1Sq;

        // Find circumcenter of p1, p2, and invP1
        // Circle through (x1, y1), (x2, y2), (x3, y3):
        double x1 = p1.X, y1 = p1.Y;
        double x2 = p2.X, y2 = p2.Y;
        double x3 = invP1X, y3 = invP1Y;

        double d = 2 * (x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2));
        if (Math.Abs(d) < 1e-7)
        {
            return new HyperbolicArc(p1, p2, IsDiameter: true);
        }

        double sq1 = x1 * x1 + y1 * y1;
        double sq2 = x2 * x2 + y2 * y2;
        double sq3 = x3 * x3 + y3 * y3;

        double cx = (sq1 * (y2 - y3) + sq2 * (y3 - y1) + sq3 * (y1 - y2)) / d;
        double cy = (sq1 * (x3 - x2) + sq2 * (x1 - x3) + sq3 * (x2 - x1)) / d;

        double r = Math.Sqrt((x1 - cx) * (x1 - cx) + (y1 - cy) * (y1 - cy));

        double angle1 = Math.Atan2(y1 - cy, x1 - cx);
        double angle2 = Math.Atan2(y2 - cy, x2 - cx);

        // Normalize sweep angle
        double sweep = angle2 - angle1;
        while (sweep > Math.PI) sweep -= 2 * Math.PI;
        while (sweep < -Math.PI) sweep += 2 * Math.PI;

        return new HyperbolicArc(p1, p2, false, cx, cy, r, angle1, sweep);
    }

    public void SetInteractiveTriangle(HyperbolicPoint a, HyperbolicPoint b, HyperbolicPoint c)
    {
        // Compute angles and area defect
        double angleA = ComputeInteriorAngle(a, b, c);
        double angleB = ComputeInteriorAngle(b, a, c);
        double angleC = ComputeInteriorAngle(c, a, b);

        // Gauss-Bonnet Theorem in Hyperbolic Plane (K = -1):
        // Area(Δ) = π - (AngleA + AngleB + AngleC)
        double angleSum = angleA + angleB + angleC;
        double defect = Math.Max(0, Math.PI - angleSum);

        ActiveTriangle = new HyperbolicTriangle(a, b, c, angleA, angleB, angleC, defect, defect);
    }

    private static double ComputeInteriorAngle(HyperbolicPoint vertex, HyperbolicPoint p1, HyperbolicPoint p2)
    {
        // Tangent vectors to geodesics at vertex
        var arc1 = ConstructGeodesicArc(vertex, p1);
        var arc2 = ConstructGeodesicArc(vertex, p2);

        var t1 = GetTangentAtVertex(arc1, vertex);
        var t2 = GetTangentAtVertex(arc2, vertex);

        double dot = t1.X * t2.X + t1.Y * t2.Y;
        double len1 = Math.Sqrt(t1.X * t1.X + t1.Y * t1.Y);
        double len2 = Math.Sqrt(t2.X * t2.X + t2.Y * t2.Y);

        if (len1 < 1e-9 || len2 < 1e-9) return 0;
        double cosTheta = Math.Clamp(dot / (len1 * len2), -1.0, 1.0);
        return Math.Acos(cosTheta);
    }

    private static (double X, double Y) GetTangentAtVertex(HyperbolicArc arc, HyperbolicPoint vertex)
    {
        if (arc.IsDiameter)
        {
            var other = (Math.Abs(arc.P1.X - vertex.X) < 1e-6 && Math.Abs(arc.P1.Y - vertex.Y) < 1e-6) ? arc.P2 : arc.P1;
            return (other.X - vertex.X, other.Y - vertex.Y);
        }

        // Circular arc: tangent is perpendicular to radius vector (vertex - center)
        double rx = vertex.X - arc.CenterX;
        double ry = vertex.Y - arc.CenterY;

        // Determine correct direction pointing towards the other point
        var otherPt = (Math.Abs(arc.P1.X - vertex.X) < 1e-6 && Math.Abs(arc.P1.Y - vertex.Y) < 1e-6) ? arc.P2 : arc.P1;
        double dir = arc.SweepAngle >= 0 ? 1.0 : -1.0;

        // Tangent vector: (-ry, rx)
        double tx = -ry * dir;
        double ty = rx * dir;

        // Dot product with secant to verify direction towards other point
        double secX = otherPt.X - vertex.X;
        double secY = otherPt.Y - vertex.Y;
        if (tx * secX + ty * secY < 0)
        {
            tx = -tx;
            ty = -ty;
        }

        return (tx, ty);
    }

    private void GenerateRegularTiling(int p, int q, int maxDepth)
    {
        // Hyperbolic condition: (p-2)*(q-2) > 4
        if ((p - 2) * (q - 2) <= 4) p = 7; // Fallback to heptagonal

        // Radius of central regular p-gon with interior angle 2π/q
        // Formula: tanh(r) = sqrt( cos(π/p + π/q) / (sin(π/p) * sin(π/q)) ) ...
        // In the Poincaré disk: r_euc = sqrt( cos(π/p + π/q) / (cos(π/p - π/q) ) )
        double cosSum = Math.Cos(Math.PI / p + Math.PI / q);
        double cosDiff = Math.Cos(Math.PI / p - Math.PI / q);
        double rEuc = Math.Sqrt(Math.Max(0.01, cosSum / Math.Max(0.01, cosDiff)));
        rEuc = Math.Clamp(rEuc, 0.1, 0.92);

        // Generate central p-gon vertices
        var centralVertices = new List<HyperbolicPoint>();
        for (int i = 0; i < p; i++)
        {
            double theta = (2 * Math.PI * i) / p;
            var pt = new HyperbolicPoint(rEuc * Math.Cos(theta), rEuc * Math.Sin(theta));
            centralVertices.Add(pt);
            Vertices.Add(pt);
        }

        // Central p-gon edges
        for (int i = 0; i < p; i++)
        {
            var p1 = centralVertices[i];
            var p2 = centralVertices[(i + 1) % p];
            Geodesics.Add(ConstructGeodesicArc(p1, p2));
        }

        // Recursively reflect across edges to build layer 1 and 2
        var visited = new HashSet<(int, int)>();
        ReflectTilingLayer(centralVertices, 0, maxDepth, p, q, visited);
    }

    private void ReflectTilingLayer(List<HyperbolicPoint> poly, int depth, int maxDepth, int p, int q, HashSet<(int, int)> visited)
    {
        if (depth >= maxDepth) return;

        int n = poly.Count;
        for (int i = 0; i < n; i++)
        {
            var p1 = poly[i];
            var p2 = poly[(i + 1) % n];

            var arc = ConstructGeodesicArc(p1, p2);
            var reflectedPoly = ReflectPolygonAcrossArc(poly, arc);

            if (reflectedPoly != null && reflectedPoly.Count == n)
            {
                // Add reflected edges
                for (int j = 0; j < n; j++)
                {
                    var a1 = reflectedPoly[j];
                    var a2 = reflectedPoly[(j + 1) % n];

                    // Check bounds inside unit disk
                    if (a1.X * a1.X + a1.Y * a1.Y < 0.98 && a2.X * a2.X + a2.Y * a2.Y < 0.98)
                    {
                        Geodesics.Add(ConstructGeodesicArc(a1, a2));
                        Vertices.Add(a1);
                    }
                }

                if (depth + 1 < maxDepth)
                {
                    ReflectTilingLayer(reflectedPoly, depth + 1, maxDepth, p, q, visited);
                }
            }
        }
    }

    private static List<HyperbolicPoint>? ReflectPolygonAcrossArc(List<HyperbolicPoint> poly, HyperbolicArc arc)
    {
        var result = new List<HyperbolicPoint>();
        foreach (var pt in poly)
        {
            var reflected = InvertPointAcrossArc(pt, arc);
            if (reflected == null) return null;
            result.Add(reflected);
        }
        return result;
    }

    private static HyperbolicPoint? InvertPointAcrossArc(HyperbolicPoint pt, HyperbolicArc arc)
    {
        if (arc.IsDiameter)
        {
            // Reflection across line passing through origin
            double dx = arc.P2.X - arc.P1.X;
            double dy = arc.P2.Y - arc.P1.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-8) return null;

            double nx = -dy / Math.Sqrt(lenSq);
            double ny = dx / Math.Sqrt(lenSq);

            double dot = pt.X * nx + pt.Y * ny;
            return new HyperbolicPoint(pt.X - 2 * dot * nx, pt.Y - 2 * dot * ny);
        }

        // Circular inversion across orthogonal circle with center (cx, cy) and radius r:
        // p' = c + r² * (p - c) / |p - c|²
        double dxP = pt.X - arc.CenterX;
        double dyP = pt.Y - arc.CenterY;
        double distSq = dxP * dxP + dyP * dyP;
        if (distSq < 1e-8) return null;

        double factor = (arc.Radius * arc.Radius) / distSq;
        double rx = arc.CenterX + factor * dxP;
        double ry = arc.CenterY + factor * dyP;

        // Inversion in hyperbolic plane also requires inversion in unit circle to stay in disk
        double rNormSq = rx * rx + ry * ry;
        if (rNormSq > 0.99999) return null; // Outside disk

        return new HyperbolicPoint(rx, ry);
    }

    private void GenerateIdealTriangle()
    {
        // 3 ideal vertices on unit circle at angles 0, 2π/3, 4π/3
        double a1 = 0;
        double a2 = 2 * Math.PI / 3;
        double a3 = 4 * Math.PI / 3;

        double r = 0.999;
        var p1 = new HyperbolicPoint(r * Math.Cos(a1), r * Math.Sin(a1));
        var p2 = new HyperbolicPoint(r * Math.Cos(a2), r * Math.Sin(a2));
        var p3 = new HyperbolicPoint(r * Math.Cos(a3), r * Math.Sin(a3));

        Geodesics.Add(ConstructGeodesicArc(p1, p2));
        Geodesics.Add(ConstructGeodesicArc(p2, p3));
        Geodesics.Add(ConstructGeodesicArc(p3, p1));

        Vertices.Add(p1);
        Vertices.Add(p2);
        Vertices.Add(p3);

        // Subdivided horocycles inside ideal triangle
        for (double d = 0.2; d <= 0.8; d += 0.2)
        {
            var h1 = new HyperbolicPoint(d * Math.Cos(a1), d * Math.Sin(a1));
            var h2 = new HyperbolicPoint(d * Math.Cos(a2), d * Math.Sin(a2));
            var h3 = new HyperbolicPoint(d * Math.Cos(a3), d * Math.Sin(a3));

            Geodesics.Add(ConstructGeodesicArc(h1, h2));
            Geodesics.Add(ConstructGeodesicArc(h2, h3));
            Geodesics.Add(ConstructGeodesicArc(h3, h1));
        }

        // Ideal triangle has all angles 0, so Area = π - (0+0+0) = π ≈ 3.14159
        ActiveTriangle = new HyperbolicTriangle(p1, p2, p3, 0.0, 0.0, 0.0, Math.PI, Math.PI);
    }

    private void GenerateHolonomyDemo()
    {
        // Non-trivial hyperbolic triangle with angles < π
        var a = new HyperbolicPoint(0.0, 0.0);
        var b = new HyperbolicPoint(0.65, 0.0);
        var c = new HyperbolicPoint(0.0, 0.65);

        Geodesics.Add(ConstructGeodesicArc(a, b));
        Geodesics.Add(ConstructGeodesicArc(b, c));
        Geodesics.Add(ConstructGeodesicArc(c, a));

        Vertices.Add(a);
        Vertices.Add(b);
        Vertices.Add(c);

        SetInteractiveTriangle(a, b, c);
    }
}
