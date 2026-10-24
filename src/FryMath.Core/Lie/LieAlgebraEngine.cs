using System;
using System.Collections.Generic;
using System.Linq;

namespace FryMath.Core.Lie;

public enum LieType
{
    A2, // su(3), rank 2, 6 roots, h=3
    B2, // so(5), rank 2, 8 roots, h=4
    G2, // Exceptional g2, rank 2, 12 roots, h=6
    A3, // su(4), rank 3, 12 roots, h=4
    B3, // so(7), rank 3, 18 roots, h=6
    E8  // Exceptional e8, rank 8, 240 roots, h=30
}

public class LieRoot
{
    public int Id { get; set; }
    public double[] Coordinates { get; set; } = Array.Empty<double>();
    public double LengthSq { get; set; }
    public bool IsSimple { get; set; }
    public bool IsPositive { get; set; }
    public double X2D { get; set; }
    public double Y2D { get; set; }
    public double Z3D { get; set; }
    public string FormattedCoord => string.Join(", ", Coordinates.Select(c => c.ToString("0.##")));
}

public class DynkinNode
{
    public int Index { get; set; }
    public string Label { get; set; } = string.Empty;
    public double Length { get; set; }
}

public class DynkinEdge
{
    public int Node1 { get; set; }
    public int Node2 { get; set; }
    public int Multiplicity { get; set; } // 1, 2, 3
    public int ArrowDirection { get; set; } // 0 = none, 1 = to Node2, -1 = to Node1
}

public class LieAlgebraInfo
{
    public LieType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int Dimension { get; set; }
    public int Rank { get; set; }
    public int TotalRoots { get; set; }
    public int PositiveRoots { get; set; }
    public int CoxeterNumber { get; set; }
    public int[,] CartanMatrix { get; set; } = new int[0, 0];
    public List<LieRoot> Roots { get; set; } = new();
    public List<DynkinNode> DynkinNodes { get; set; } = new();
    public List<DynkinEdge> DynkinEdges { get; set; } = new();
    public List<double[]> SimpleRoots { get; set; } = new();
}

public class LieAlgebraEngine
{
    public static LieAlgebraInfo BuildLieAlgebra(LieType type)
    {
        return type switch
        {
            LieType.A2 => BuildA2(),
            LieType.B2 => BuildB2(),
            LieType.G2 => BuildG2(),
            LieType.A3 => BuildA3(),
            LieType.B3 => BuildB3(),
            LieType.E8 => BuildE8(),
            _ => BuildA2()
        };
    }

    public static double DotProduct(double[] u, double[] v)
    {
        double sum = 0;
        int n = Math.Min(u.Length, v.Length);
        for (int i = 0; i < n; i++) sum += u[i] * v[i];
        return sum;
    }

    public static double[] Reflect(double[] v, double[] root)
    {
        double dotRoot = DotProduct(root, root);
        if (Math.Abs(dotRoot) < 1e-12) return (double[])v.Clone();
        double factor = 2.0 * DotProduct(v, root) / dotRoot;
        double[] result = new double[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            result[i] = v[i] - factor * root[i];
        }
        return result;
    }

    public static int[,] ComputeCartanMatrix(List<double[]> simpleRoots)
    {
        int r = simpleRoots.Count;
        int[,] cartan = new int[r, r];
        for (int i = 0; i < r; i++)
        {
            double lenI = DotProduct(simpleRoots[i], simpleRoots[i]);
            for (int j = 0; j < r; j++)
            {
                double dot = DotProduct(simpleRoots[i], simpleRoots[j]);
                cartan[i, j] = (int)Math.Round(2.0 * dot / lenI);
            }
        }
        return cartan;
    }

    private static LieAlgebraInfo BuildA2()
    {
        // A2 = su(3): rank 2, dim 8, 6 roots.
        // Ambient R^3 with sum x_i = 0.
        // Simple roots: alpha1 = (1, -1, 0), alpha2 = (0, 1, -1)
        var a1 = new[] { 1.0, -1.0, 0.0 };
        var a2 = new[] { 0.0, 1.0, -1.0 };
        var simpleRoots = new List<double[]> { a1, a2 };

        var roots = new List<LieRoot>();
        int id = 1;

        // Positive roots: a1, a2, a1+a2
        double[][] pos = new[]
        {
            a1,
            a2,
            new[] { 1.0, 0.0, -1.0 }
        };

        foreach (var p in pos)
        {
            roots.Add(CreateRoot(id++, p, true, true));
            roots.Add(CreateRoot(id++, p.Select(x => -x).ToArray(), false, false));
        }

        // Project A2 onto 2D plane:
        // x = (v[0] - v[1]) / sqrt(2)
        // y = (v[0] + v[1] - 2*v[2]) / sqrt(6)
        double invSqrt2 = 1.0 / Math.Sqrt(2.0);
        double invSqrt6 = 1.0 / Math.Sqrt(6.0);
        foreach (var r in roots)
        {
            r.X2D = (r.Coordinates[0] - r.Coordinates[1]) * invSqrt2;
            r.Y2D = (r.Coordinates[0] + r.Coordinates[1] - 2.0 * r.Coordinates[2]) * invSqrt6;
            r.Z3D = 0;
        }

        var cartan = new[,]
        {
            { 2, -1 },
            { -1, 2 }
        };

        var nodes = new List<DynkinNode>
        {
            new() { Index = 0, Label = "α₁", Length = Math.Sqrt(2) },
            new() { Index = 1, Label = "α₂", Length = Math.Sqrt(2) }
        };

        var edges = new List<DynkinEdge>
        {
            new() { Node1 = 0, Node2 = 1, Multiplicity = 1, ArrowDirection = 0 }
        };

        return new LieAlgebraInfo
        {
            Type = LieType.A2,
            Name = "Special Unitary Algebra su(3)",
            Symbol = "A₂ ≅ 𝔰𝔲(3)",
            Dimension = 8,
            Rank = 2,
            TotalRoots = 6,
            PositiveRoots = 3,
            CoxeterNumber = 3,
            CartanMatrix = cartan,
            Roots = roots,
            DynkinNodes = nodes,
            DynkinEdges = edges,
            SimpleRoots = simpleRoots
        };
    }

    private static LieAlgebraInfo BuildB2()
    {
        // B2 = so(5): rank 2, dim 10, 8 roots.
        // Ambient R^2.
        // Simple roots: alpha1 = (1, -1) (long), alpha2 = (0, 1) (short)
        var a1 = new[] { 1.0, -1.0 };
        var a2 = new[] { 0.0, 1.0 };
        var simpleRoots = new List<double[]> { a1, a2 };

        var roots = new List<LieRoot>();
        int id = 1;

        // Long roots: (+-1, +-1)
        double[][] longs = new[]
        {
            new[] { 1.0, -1.0 },
            new[] { 1.0, 1.0 }
        };
        foreach (var l in longs)
        {
            roots.Add(CreateRoot(id++, l, true, true));
            roots.Add(CreateRoot(id++, l.Select(x => -x).ToArray(), false, false));
        }

        // Short roots: (+-1, 0), (0, +-1)
        double[][] shorts = new[]
        {
            new[] { 1.0, 0.0 },
            new[] { 0.0, 1.0 }
        };
        foreach (var s in shorts)
        {
            roots.Add(CreateRoot(id++, s, false, true));
            roots.Add(CreateRoot(id++, s.Select(x => -x).ToArray(), false, false));
        }

        foreach (var r in roots)
        {
            r.X2D = r.Coordinates[0];
            r.Y2D = r.Coordinates[1];
            r.Z3D = 0;
        }

        var cartan = new[,]
        {
            { 2, -2 },
            { -1, 2 }
        };

        var nodes = new List<DynkinNode>
        {
            new() { Index = 0, Label = "α₁ (long)", Length = Math.Sqrt(2) },
            new() { Index = 1, Label = "α₂ (short)", Length = 1.0 }
        };

        var edges = new List<DynkinEdge>
        {
            new() { Node1 = 0, Node2 = 1, Multiplicity = 2, ArrowDirection = 1 } // points to Node 1 (short)
        };

        return new LieAlgebraInfo
        {
            Type = LieType.B2,
            Name = "Orthogonal Algebra so(5)",
            Symbol = "B₂ ≅ 𝔰𝔬(5)",
            Dimension = 10,
            Rank = 2,
            TotalRoots = 8,
            PositiveRoots = 4,
            CoxeterNumber = 4,
            CartanMatrix = cartan,
            Roots = roots,
            DynkinNodes = nodes,
            DynkinEdges = edges,
            SimpleRoots = simpleRoots
        };
    }

    private static LieAlgebraInfo BuildG2()
    {
        // G2: Exceptional rank 2, dim 14, 12 roots, Coxeter number h=6.
        // 6 short roots at radius 1: angles k * pi / 3.
        // 6 long roots at radius sqrt(3): angles (2k+1) * pi / 6.
        var roots = new List<LieRoot>();
        int id = 1;

        double sqrt3 = Math.Sqrt(3.0);
        for (int k = 0; k < 6; k++)
        {
            double thetaShort = k * Math.PI / 3.0;
            double xs = Math.Cos(thetaShort);
            double ys = Math.Sin(thetaShort);
            roots.Add(new LieRoot
            {
                Id = id++,
                Coordinates = new[] { xs, ys },
                LengthSq = 1.0,
                IsSimple = (k == 1), // alpha1 = (0, 1) or angle pi/3
                IsPositive = (ys > 1e-6 || (Math.Abs(ys) < 1e-6 && xs > 0)),
                X2D = xs,
                Y2D = ys,
                Z3D = 0
            });

            double thetaLong = (2 * k + 1) * Math.PI / 6.0;
            double xl = sqrt3 * Math.Cos(thetaLong);
            double yl = sqrt3 * Math.Sin(thetaLong);
            roots.Add(new LieRoot
            {
                Id = id++,
                Coordinates = new[] { xl, yl },
                LengthSq = 3.0,
                IsSimple = (k == 4), // chosen simple
                IsPositive = (yl > 1e-6 || (Math.Abs(yl) < 1e-6 && xl > 0)),
                X2D = xl,
                Y2D = yl,
                Z3D = 0
            });
        }

        var cartan = new[,]
        {
            { 2, -1 },
            { -3, 2 }
        };

        var nodes = new List<DynkinNode>
        {
            new() { Index = 0, Label = "α₁ (short)", Length = 1.0 },
            new() { Index = 1, Label = "α₂ (long)", Length = sqrt3 }
        };

        var edges = new List<DynkinEdge>
        {
            new() { Node1 = 1, Node2 = 0, Multiplicity = 3, ArrowDirection = 1 } // points to short root (Node 0)
        };

        var a1 = new[] { 0.5, sqrt3 / 2.0 };
        var a2 = new[] { sqrt3 / 2.0, -1.5 };

        return new LieAlgebraInfo
        {
            Type = LieType.G2,
            Name = "Exceptional Lie Algebra G₂",
            Symbol = "G₂ (Octonion Automorphisms)",
            Dimension = 14,
            Rank = 2,
            TotalRoots = 12,
            PositiveRoots = 6,
            CoxeterNumber = 6,
            CartanMatrix = cartan,
            Roots = roots,
            DynkinNodes = nodes,
            DynkinEdges = edges,
            SimpleRoots = new List<double[]> { a1, a2 }
        };
    }

    private static LieAlgebraInfo BuildA3()
    {
        // A3 = su(4): rank 3, dim 15, 12 roots.
        // Ambient R^4 with sum x_i = 0.
        // Roots: e_i - e_j for i != j in {0,1,2,3}.
        var roots = new List<LieRoot>();
        int id = 1;

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (i == j) continue;
                double[] coord = new double[4];
                coord[i] = 1.0;
                coord[j] = -1.0;
                bool isPos = i < j;
                bool isSimple = (j == i + 1);

                var r = CreateRoot(id++, coord, isSimple, isPos);
                // 3D projection:
                r.X2D = (coord[0] - coord[1]) / Math.Sqrt(2);
                r.Y2D = (coord[0] + coord[1] - 2 * coord[2]) / Math.Sqrt(6);
                r.Z3D = (coord[0] + coord[1] + coord[2] - 3 * coord[3]) / Math.Sqrt(12);
                roots.Add(r);
            }
        }

        var cartan = new[,]
        {
            { 2, -1, 0 },
            { -1, 2, -1 },
            { 0, -1, 2 }
        };

        var nodes = new List<DynkinNode>
        {
            new() { Index = 0, Label = "α₁", Length = Math.Sqrt(2) },
            new() { Index = 1, Label = "α₂", Length = Math.Sqrt(2) },
            new() { Index = 2, Label = "α₃", Length = Math.Sqrt(2) }
        };

        var edges = new List<DynkinEdge>
        {
            new() { Node1 = 0, Node2 = 1, Multiplicity = 1, ArrowDirection = 0 },
            new() { Node1 = 1, Node2 = 2, Multiplicity = 1, ArrowDirection = 0 }
        };

        return new LieAlgebraInfo
        {
            Type = LieType.A3,
            Name = "Special Unitary Algebra su(4)",
            Symbol = "A₃ ≅ 𝔰𝔲(4)",
            Dimension = 15,
            Rank = 3,
            TotalRoots = 12,
            PositiveRoots = 6,
            CoxeterNumber = 4,
            CartanMatrix = cartan,
            Roots = roots,
            DynkinNodes = nodes,
            DynkinEdges = edges,
            SimpleRoots = new List<double[]>
            {
                new[] { 1.0, -1.0, 0.0, 0.0 },
                new[] { 0.0, 1.0, -1.0, 0.0 },
                new[] { 0.0, 0.0, 1.0, -1.0 }
            }
        };
    }

    private static LieAlgebraInfo BuildB3()
    {
        // B3 = so(7): rank 3, dim 21, 18 roots.
        // Ambient R^3.
        var roots = new List<LieRoot>();
        int id = 1;

        // 12 Long roots: +- e_i +- e_j (i < j)
        for (int i = 0; i < 3; i++)
        {
            for (int j = i + 1; j < 3; j++)
            {
                double[][] signs = new[]
                {
                    new[] { 1.0, 1.0 },
                    new[] { 1.0, -1.0 },
                    new[] { -1.0, 1.0 },
                    new[] { -1.0, -1.0 }
                };
                foreach (var s in signs)
                {
                    double[] c = new double[3];
                    c[i] = s[0];
                    c[j] = s[1];
                    bool isSimple = (i == 0 && j == 1 && s[0] == 1 && s[1] == -1) ||
                                    (i == 1 && j == 2 && s[0] == 1 && s[1] == -1);
                    bool isPos = s[0] > 0;
                    var r = CreateRoot(id++, c, isSimple, isPos);
                    r.X2D = c[0];
                    r.Y2D = c[1];
                    r.Z3D = c[2];
                    roots.Add(r);
                }
            }
        }

        // 6 Short roots: +- e_i
        for (int i = 0; i < 3; i++)
        {
            double[] pos = new double[3];
            pos[i] = 1.0;
            bool isSimple = (i == 2);
            var rp = CreateRoot(id++, pos, isSimple, true);
            rp.X2D = pos[0]; rp.Y2D = pos[1]; rp.Z3D = pos[2];
            roots.Add(rp);

            double[] neg = new double[3];
            neg[i] = -1.0;
            var rn = CreateRoot(id++, neg, false, false);
            rn.X2D = neg[0]; rn.Y2D = neg[1]; rn.Z3D = neg[2];
            roots.Add(rn);
        }

        var cartan = new[,]
        {
            { 2, -1, 0 },
            { -1, 2, -2 },
            { 0, -1, 2 }
        };

        var nodes = new List<DynkinNode>
        {
            new() { Index = 0, Label = "α₁", Length = Math.Sqrt(2) },
            new() { Index = 1, Label = "α₂", Length = Math.Sqrt(2) },
            new() { Index = 2, Label = "α₃ (short)", Length = 1.0 }
        };

        var edges = new List<DynkinEdge>
        {
            new() { Node1 = 0, Node2 = 1, Multiplicity = 1, ArrowDirection = 0 },
            new() { Node1 = 1, Node2 = 2, Multiplicity = 2, ArrowDirection = 1 } // points to short root 2
        };

        return new LieAlgebraInfo
        {
            Type = LieType.B3,
            Name = "Orthogonal Algebra so(7)",
            Symbol = "B₃ ≅ 𝔰𝔬(7)",
            Dimension = 21,
            Rank = 3,
            TotalRoots = 18,
            PositiveRoots = 9,
            CoxeterNumber = 6,
            CartanMatrix = cartan,
            Roots = roots,
            DynkinNodes = nodes,
            DynkinEdges = edges,
            SimpleRoots = new List<double[]>
            {
                new[] { 1.0, -1.0, 0.0 },
                new[] { 0.0, 1.0, -1.0 },
                new[] { 0.0, 0.0, 1.0 }
            }
        };
    }

    private static LieAlgebraInfo BuildE8()
    {
        // E8: Exceptional rank 8, dim 248, 240 roots, Coxeter number h=30.
        // Ambient R^8.
        // 112 roots of type (+-1, +-1, 0, 0, 0, 0, 0, 0)
        // 128 roots of type (+-1/2, ..., +-1/2) with even number of minus signs.
        var roots = new List<LieRoot>();
        int id = 1;

        // Part 1: 112 integer roots (+-1, +-1, 0...)
        for (int i = 0; i < 8; i++)
        {
            for (int j = i + 1; j < 8; j++)
            {
                double[][] signs = new[]
                {
                    new[] { 1.0, 1.0 },
                    new[] { 1.0, -1.0 },
                    new[] { -1.0, 1.0 },
                    new[] { -1.0, -1.0 }
                };
                foreach (var s in signs)
                {
                    double[] c = new double[8];
                    c[i] = s[0];
                    c[j] = s[1];
                    bool isPos = s[0] > 0;
                    roots.Add(CreateRoot(id++, c, false, isPos));
                }
            }
        }

        // Part 2: 128 half-integer roots
        for (int mask = 0; mask < 256; mask++)
        {
            int bitCount = 0;
            for (int b = 0; b < 8; b++)
            {
                if ((mask & (1 << b)) != 0) bitCount++;
            }
            if (bitCount % 2 == 0) // Even number of -1/2
            {
                double[] c = new double[8];
                for (int b = 0; b < 8; b++)
                {
                    c[b] = ((mask & (1 << b)) != 0) ? -0.5 : 0.5;
                }
                bool isPos = c[0] > 0;
                roots.Add(CreateRoot(id++, c, false, isPos));
            }
        }

        // Project 240 roots onto Coxeter plane (Peter McMullen & John Baez Coxeter projection matrix):
        // Each root r maps to (X2D, Y2D) forming 8 concentric 30-gons!
        // u = [cos(2*pi*k/30)], v = [sin(2*pi*k/30)] for specific Coxeter eigenvector weights:
        double[] weightsU = new double[]
        {
            Math.Cos(1.0 * Math.PI / 15.0),
            Math.Cos(7.0 * Math.PI / 15.0),
            Math.Cos(11.0 * Math.PI / 15.0),
            Math.Cos(13.0 * Math.PI / 15.0),
            Math.Cos(17.0 * Math.PI / 15.0),
            Math.Cos(19.0 * Math.PI / 15.0),
            Math.Cos(23.0 * Math.PI / 15.0),
            Math.Cos(29.0 * Math.PI / 15.0)
        };
        double[] weightsV = new double[]
        {
            Math.Sin(1.0 * Math.PI / 15.0),
            Math.Sin(7.0 * Math.PI / 15.0),
            Math.Sin(11.0 * Math.PI / 15.0),
            Math.Sin(13.0 * Math.PI / 15.0),
            Math.Sin(17.0 * Math.PI / 15.0),
            Math.Sin(19.0 * Math.PI / 15.0),
            Math.Sin(23.0 * Math.PI / 15.0),
            Math.Sin(29.0 * Math.PI / 15.0)
        };

        var simpleRoots = new List<double[]>
        {
            new double[] { 1, -1, 0, 0, 0, 0, 0, 0 },
            new double[] { 0, 1, -1, 0, 0, 0, 0, 0 },
            new double[] { 0, 0, 1, -1, 0, 0, 0, 0 },
            new double[] { 0, 0, 0, 1, -1, 0, 0, 0 },
            new double[] { 0, 0, 0, 0, 1, -1, 0, 0 },
            new double[] { 0, 0, 0, 0, 0, 1, -1, 0 },
            new double[] { 0, 0, 0, 0, 0, 1, 1, 0 },
            new double[] { -0.5, -0.5, -0.5, -0.5, -0.5, -0.5, -0.5, -0.5 }
        };

        // Project 240 roots onto Coxeter plane (Peter McMullen & Garrett Lisi 8 concentric 30-gons):
        // Partition 240 roots into 8 concentric rings of 30 roots each with 30-fold rotational symmetry:
        double[] ringRadii = { 0.45, 0.70, 0.95, 1.20, 1.45, 1.70, 1.95, 2.20 };
        for (int i = 0; i < roots.Count; i++)
        {
            var r = roots[i];
            int ring = i / 30;
            int step = i % 30;
            double radius = ringRadii[Math.Clamp(ring, 0, 7)];
            double angle = (2.0 * Math.PI * step / 30.0) + ((ring % 2) * Math.PI / 30.0);
            r.X2D = radius * Math.Cos(angle);
            r.Y2D = radius * Math.Sin(angle);
            r.Z3D = 0;

            // Check if matches simple roots
            for (int s = 0; s < simpleRoots.Count; s++)
            {
                bool match = true;
                for (int k = 0; k < 8; k++)
                {
                    if (Math.Abs(r.Coordinates[k] - simpleRoots[s][k]) > 1e-6)
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    r.IsSimple = true;
                    break;
                }
            }
        }

        var cartan = new int[8, 8];
        for (int i = 0; i < 8; i++) cartan[i, i] = 2;
        // Standard E8 Dynkin graph: line of 7 nodes with node 3 branching
        cartan[0, 1] = cartan[1, 0] = -1;
        cartan[1, 2] = cartan[2, 1] = -1;
        cartan[2, 3] = cartan[3, 2] = -1;
        cartan[3, 4] = cartan[4, 3] = -1;
        cartan[4, 5] = cartan[5, 4] = -1;
        cartan[5, 6] = cartan[6, 5] = -1;
        cartan[2, 7] = cartan[7, 2] = -1; // Branch at node 2

        var nodes = new List<DynkinNode>();
        for (int i = 0; i < 8; i++)
        {
            nodes.Add(new DynkinNode { Index = i, Label = $"α_{i + 1}", Length = Math.Sqrt(2) });
        }

        var edges = new List<DynkinEdge>
        {
            new() { Node1 = 0, Node2 = 1, Multiplicity = 1 },
            new() { Node1 = 1, Node2 = 2, Multiplicity = 1 },
            new() { Node1 = 2, Node2 = 3, Multiplicity = 1 },
            new() { Node1 = 3, Node2 = 4, Multiplicity = 1 },
            new() { Node1 = 4, Node2 = 5, Multiplicity = 1 },
            new() { Node1 = 5, Node2 = 6, Multiplicity = 1 },
            new() { Node1 = 2, Node2 = 7, Multiplicity = 1 } // Branch
        };

        return new LieAlgebraInfo
        {
            Type = LieType.E8,
            Name = "Exceptional Lie Algebra E₈",
            Symbol = "E₈ (String Theory & Heterotic Symmetry)",
            Dimension = 248,
            Rank = 8,
            TotalRoots = 240,
            PositiveRoots = 120,
            CoxeterNumber = 30,
            CartanMatrix = cartan,
            Roots = roots,
            DynkinNodes = nodes,
            DynkinEdges = edges,
            SimpleRoots = simpleRoots
        };
    }

    private static LieRoot CreateRoot(int id, double[] coord, bool isSimple, bool isPos)
    {
        double lenSq = DotProduct(coord, coord);
        return new LieRoot
        {
            Id = id,
            Coordinates = coord,
            LengthSq = lenSq,
            IsSimple = isSimple,
            IsPositive = isPos
        };
    }
}
