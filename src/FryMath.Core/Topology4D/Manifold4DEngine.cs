using System;
using System.Collections.Generic;

namespace FryMath.Core.Topology4D;

public enum ManifoldPresetType
{
    Tesseract,
    CliffordTorus,
    KleinBottle4D,
    HopfFibration,
    Cell24
}

public record Vector4D(double X, double Y, double Z, double W)
{
    public static Vector4D Zero => new(0, 0, 0, 0);

    public Vector4D RotateXW(double angle)
    {
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        return new Vector4D(X * cos - W * sin, Y, Z, X * sin + W * cos);
    }

    public Vector4D RotateYW(double angle)
    {
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        return new Vector4D(X, Y * cos - W * sin, Z, Y * sin + W * cos);
    }

    public Vector4D RotateZW(double angle)
    {
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        return new Vector4D(X, Y, Z * cos - W * sin, Z * sin + W * cos);
    }

    public Vector4D RotateXY(double angle)
    {
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        return new Vector4D(X * cos - Y * sin, X * sin + Y * cos, Z, W);
    }

    public (double X3, double Y3, double Z3) ProjectStereographic(double d = 2.4)
    {
        double denom = Math.Max(0.2, d - W);
        return (X / denom, Y / denom, Z / denom);
    }
}

public class Edge4D
{
    public int V1 { get; }
    public int V2 { get; }

    public Edge4D(int v1, int v2)
    {
        V1 = v1;
        V2 = v2;
    }
}

public class Manifold4DEngine
{
    public List<Vector4D> BaseVertices { get; } = [];
    public List<Edge4D> Edges { get; } = [];

    public double AngleXW { get; set; } = 0.0;
    public double AngleYW { get; set; } = 0.0;
    public double AngleZW { get; set; } = 0.0;
    public double AngleXY { get; set; } = 0.0;

    public double CameraYaw { get; set; } = 0.45;
    public double CameraPitch { get; set; } = 0.35;
    public double ProjectionDistance { get; set; } = 2.4;

    public bool IsRotating { get; set; } = true;
    public ManifoldPresetType CurrentPreset { get; private set; } = ManifoldPresetType.Tesseract;

    public Manifold4DEngine()
    {
        LoadPreset(ManifoldPresetType.Tesseract);
    }

    public void LoadPreset(ManifoldPresetType preset)
    {
        CurrentPreset = preset;
        BaseVertices.Clear();
        Edges.Clear();

        switch (preset)
        {
            case ManifoldPresetType.Tesseract:
                BuildTesseract();
                break;
            case ManifoldPresetType.CliffordTorus:
                BuildCliffordTorus();
                break;
            case ManifoldPresetType.KleinBottle4D:
                BuildKleinBottle4D();
                break;
            case ManifoldPresetType.HopfFibration:
                BuildHopfFibration();
                break;
            case ManifoldPresetType.Cell24:
                Build24Cell();
                break;
        }
    }

    private void BuildTesseract()
    {
        // 16 vertices: all combinations of (+/-1, +/-1, +/-1, +/-1) scaled by 0.75
        double s = 0.75;
        for (int i = 0; i < 16; i++)
        {
            double x = ((i & 1) != 0 ? 1 : -1) * s;
            double y = ((i & 2) != 0 ? 1 : -1) * s;
            double z = ((i & 4) != 0 ? 1 : -1) * s;
            double w = ((i & 8) != 0 ? 1 : -1) * s;
            BaseVertices.Add(new Vector4D(x, y, z, w));
        }

        // 32 edges connecting vertices that differ in exactly one coordinate (Hamming distance 1)
        for (int i = 0; i < 16; i++)
        {
            for (int bit = 1; bit <= 8; bit <<= 1)
            {
                int j = i ^ bit;
                if (i < j)
                {
                    Edges.Add(new Edge4D(i, j));
                }
            }
        }
    }

    private void BuildCliffordTorus()
    {
        // Parameterized as: (cos u, sin u, cos v, sin v) / sqrt(2)
        int uSteps = 16;
        int vSteps = 16;
        double s = 1.0 / Math.Sqrt(2.0);

        for (int i = 0; i < uSteps; i++)
        {
            double u = 2.0 * Math.PI * i / uSteps;
            for (int j = 0; j < vSteps; j++)
            {
                double v = 2.0 * Math.PI * j / vSteps;
                BaseVertices.Add(new Vector4D(
                    s * Math.Cos(u),
                    s * Math.Sin(u),
                    s * Math.Cos(v),
                    s * Math.Sin(v)));
            }
        }

        // Grid edges
        for (int i = 0; i < uSteps; i++)
        {
            for (int j = 0; j < vSteps; j++)
            {
                int curr = i * vSteps + j;
                int nextU = ((i + 1) % uSteps) * vSteps + j;
                int nextV = i * vSteps + ((j + 1) % vSteps);

                Edges.Add(new Edge4D(curr, nextU));
                Edges.Add(new Edge4D(curr, nextV));
            }
        }
    }

    private void BuildKleinBottle4D()
    {
        // 4D non-self-intersecting Klein bottle embedding:
        // x = (r + cos(u/2)sin v - sin(u/2)sin 2v) cos u
        // y = (r + cos(u/2)sin v - sin(u/2)sin 2v) sin u
        // z = sin(u/2)sin v + cos(u/2)sin 2v
        // w = cos(u/2)cos v
        int uSteps = 22;
        int vSteps = 14;
        double r = 1.6;

        for (int i = 0; i < uSteps; i++)
        {
            double u = 2.0 * Math.PI * i / uSteps;
            double cu2 = Math.Cos(u * 0.5);
            double su2 = Math.Sin(u * 0.5);

            for (int j = 0; j < vSteps; j++)
            {
                double v = 2.0 * Math.PI * j / vSteps;
                double cv = Math.Cos(v);
                double sv = Math.Sin(v);
                double s2v = Math.Sin(2.0 * v);

                double rad = r + cu2 * sv - su2 * s2v;
                double x = rad * Math.Cos(u) * 0.45;
                double y = rad * Math.Sin(u) * 0.45;
                double z = (su2 * sv + cu2 * s2v) * 0.45;
                double w = (cu2 * cv) * 0.45;

                BaseVertices.Add(new Vector4D(x, y, z, w));
            }
        }

        for (int i = 0; i < uSteps; i++)
        {
            for (int j = 0; j < vSteps; j++)
            {
                int curr = i * vSteps + j;
                int nextU = ((i + 1) % uSteps) * vSteps + j;
                int nextV = i * vSteps + ((j + 1) % vSteps);

                Edges.Add(new Edge4D(curr, nextU));
                Edges.Add(new Edge4D(curr, nextV));
            }
        }
    }

    private void BuildHopfFibration()
    {
        // Family of linked circles (fibers) in S^3
        int numCircles = 8;
        int ptsPerCircle = 24;

        for (int c = 0; c < numCircles; c++)
        {
            double eta = Math.PI * 0.25; // mid torus
            double xi = 2.0 * Math.PI * c / numCircles;

            int circleStart = BaseVertices.Count;
            for (int p = 0; p < ptsPerCircle; p++)
            {
                double phi = 2.0 * Math.PI * p / ptsPerCircle;
                double psi1 = phi;
                double psi2 = phi + xi;

                double x = Math.Cos(eta) * Math.Cos(psi1);
                double y = Math.Cos(eta) * Math.Sin(psi1);
                double z = Math.Sin(eta) * Math.Cos(psi2);
                double w = Math.Sin(eta) * Math.Sin(psi2);

                BaseVertices.Add(new Vector4D(x * 0.9, y * 0.9, z * 0.9, w * 0.9));
            }

            for (int p = 0; p < ptsPerCircle; p++)
            {
                Edges.Add(new Edge4D(circleStart + p, circleStart + ((p + 1) % ptsPerCircle)));
            }
        }
    }

    private void Build24Cell()
    {
        // 24 vertices of icositetrachoron:
        // 8 vertices: (+/-1, 0, 0, 0) and permutations
        // 16 vertices: (+/-0.5, +/-0.5, +/-0.5, +/-0.5)
        double s = 1.0;
        // 8 axis vertices
        BaseVertices.Add(new Vector4D(s, 0, 0, 0));
        BaseVertices.Add(new Vector4D(-s, 0, 0, 0));
        BaseVertices.Add(new Vector4D(0, s, 0, 0));
        BaseVertices.Add(new Vector4D(0, -s, 0, 0));
        BaseVertices.Add(new Vector4D(0, 0, s, 0));
        BaseVertices.Add(new Vector4D(0, 0, -s, 0));
        BaseVertices.Add(new Vector4D(0, 0, 0, s));
        BaseVertices.Add(new Vector4D(0, 0, 0, -s));

        // 16 half-vertices
        double h = 0.5;
        for (int i = 0; i < 16; i++)
        {
            double x = ((i & 1) != 0 ? 1 : -1) * h;
            double y = ((i & 2) != 0 ? 1 : -1) * h;
            double z = ((i & 4) != 0 ? 1 : -1) * h;
            double w = ((i & 8) != 0 ? 1 : -1) * h;
            BaseVertices.Add(new Vector4D(x, y, z, w));
        }

        // 96 edges: connect vertices at distance 1.0 apart
        for (int i = 0; i < BaseVertices.Count; i++)
        {
            for (int j = i + 1; j < BaseVertices.Count; j++)
            {
                double dx = BaseVertices[i].X - BaseVertices[j].X;
                double dy = BaseVertices[i].Y - BaseVertices[j].Y;
                double dz = BaseVertices[i].Z - BaseVertices[j].Z;
                double dw = BaseVertices[i].W - BaseVertices[j].W;
                double dist2 = dx * dx + dy * dy + dz * dz + dw * dw;
                if (Math.Abs(dist2 - 1.0) < 0.05)
                {
                    Edges.Add(new Edge4D(i, j));
                }
            }
        }
    }

    public List<(double ScreenX, double ScreenY, double DepthW)> GetProjectedVertices(double screenWidth, double screenHeight, double scale)
    {
        var result = new List<(double ScreenX, double ScreenY, double DepthW)>(BaseVertices.Count);

        double cosYaw = Math.Cos(CameraYaw);
        double sinYaw = Math.Sin(CameraYaw);
        double cosPitch = Math.Cos(CameraPitch);
        double sinPitch = Math.Sin(CameraPitch);

        double ox = screenWidth / 2.0;
        double oy = screenHeight / 2.0;

        foreach (var v in BaseVertices)
        {
            // 1. 4D Rotations
            var r = v.RotateXW(AngleXW)
                     .RotateYW(AngleYW)
                     .RotateZW(AngleZW)
                     .RotateXY(AngleXY);

            // 2. Stereographic Projection from R^4 to R^3
            var (x3, y3, z3) = r.ProjectStereographic(ProjectionDistance);

            // 3. 3D Camera Orbit Projection to Screen
            double xCam = x3 * cosYaw - z3 * sinYaw;
            double zTemp = x3 * sinYaw + z3 * cosYaw;
            double yCam = y3 * cosPitch - zTemp * sinPitch;
            double zCam = y3 * sinPitch + zTemp * cosPitch;

            double persp = 3.5 / Math.Max(0.5, 3.5 + zCam);
            double sx = ox + xCam * scale * persp;
            double sy = oy - yCam * scale * persp;

            result.Add((sx, sy, r.W));
        }

        return result;
    }

    public void Step(double dt = 0.02)
    {
        if (!IsRotating) return;
        AngleXW += 0.8 * dt;
        AngleYW += 0.5 * dt;
        AngleZW += 0.3 * dt;
        AngleXY += 0.4 * dt;
    }
}
