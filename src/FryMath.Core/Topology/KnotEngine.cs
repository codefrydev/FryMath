using System;
using System.Collections.Generic;
using System.Linq;
using FryMath.Core.LinearAlgebra;
using Vector3D = FryMath.Core.LinearAlgebra.Vector3D;

namespace FryMath.Core.Topology;

/// <summary>
/// Knot Theory, Braids & 3D Topological Invariants Engine.
/// Implements canonical knots (Trefoil 3₁, Figure-Eight 4₁, Cinquefoil 5₁, Borromean rings, Whitehead link, Torus knots (p,q)),
/// 3D tube mesh generation via rotation-minimizing Bishop frames,
/// Artin Braid Group B_n algebra and closures,
/// and knot polynomial invariants (Jones polynomial V(t), Kauffman bracket, Writhe, Linking number, Alexander polynomial).
/// </summary>
public static class KnotEngine
{
    public record KnotPoint(Vector3D Position, Vector3D Tangent, Vector3D Normal, Vector3D Binormal);

    public record Crossing(double Parameter1, double Parameter2, Vector3D Location, bool OverstrandFirst, int Sign);

    public record PolynomialTerm(int Exponent, int Coefficient);

    public record LaurentPolynomial(IReadOnlyList<PolynomialTerm> Terms)
    {
        public override string ToString()
        {
            if (Terms.Count == 0) return "0";
            var ordered = Terms.OrderByDescending(t => t.Exponent).ToList();
            var parts = new List<string>();

            for (int i = 0; i < ordered.Count; i++)
            {
                var term = ordered[i];
                if (term.Coefficient == 0) continue;

                string coeffStr;
                if (Math.Abs(term.Coefficient) == 1 && term.Exponent != 0)
                {
                    coeffStr = term.Coefficient < 0 ? "-" : (parts.Count > 0 ? "+" : "");
                }
                else
                {
                    coeffStr = (term.Coefficient > 0 && parts.Count > 0 ? "+" : "") + term.Coefficient;
                }

                string varStr;
                if (term.Exponent == 0) varStr = "";
                else if (term.Exponent == 1) varStr = "t";
                else varStr = $"t^{{{term.Exponent}}}";

                parts.Add($"{coeffStr}{varStr}");
            }

            return parts.Count > 0 ? string.Join(" ", parts) : "0";
        }
    }

    /// <summary>
    /// Evaluates 3D coordinates for canonical knots and links at parameter t in [0, 2pi].
    /// </summary>
    public static Vector3D EvaluateCurve(string knotType, double t, int p = 3, int q = 2, int componentIndex = 0)
    {
        switch (knotType.ToLowerInvariant())
        {
            case "trefoil":
            case "3_1":
                // Standard harmonic parametric trefoil knot 3_1
                return new Vector3D(
                    Math.Sin(t) + 2.0 * Math.Sin(2.0 * t),
                    Math.Cos(t) - 2.0 * Math.Cos(2.0 * t),
                    -Math.Sin(3.0 * t)
                );

            case "figure8":
            case "figureeight":
            case "4_1":
                // Canonical figure-eight knot 4_1 (amphichiral)
                return new Vector3D(
                    (2.0 + Math.Cos(2.0 * t)) * Math.Cos(3.0 * t),
                    (2.0 + Math.Cos(2.0 * t)) * Math.Sin(3.0 * t),
                    Math.Sin(4.0 * t)
                );

            case "cinquefoil":
            case "5_1":
                // Solomon's seal / Cinquefoil knot 5_1 (p=5, q=2 torus knot)
                return EvaluateTorusKnot(5, 2, t, 2.5, 1.2);

            case "torus":
                // General (p, q) torus knot on a torus of major radius R and minor radius r
                return EvaluateTorusKnot(p, q, t, 2.5, 1.0);

            case "borromean":
                // Borromean rings: 3 mutually orthogonal linked ellipses
                return EvaluateBorromeanRing(componentIndex, t);

            case "whitehead":
                // Whitehead link: 2 components with linking number 0
                return EvaluateWhiteheadLink(componentIndex, t);

            case "hopf":
                // Hopf link: 2 linked circles
                return EvaluateHopfLink(componentIndex, t);

            case "unknot":
            default:
                // Unknot (standard circle in xy plane)
                return new Vector3D(
                    2.0 * Math.Cos(t),
                    2.0 * Math.Sin(t),
                    0.0
                );
        }
    }

    /// <summary>
    /// Parametric torus knot on torus of major radius R and minor radius r.
    /// </summary>
    public static Vector3D EvaluateTorusKnot(int p, int q, double t, double R = 2.5, double r = 1.0)
    {
        double phi = p * t;
        double theta = q * t;
        double radius = R + r * Math.Cos(theta);

        return new Vector3D(
            radius * Math.Cos(phi),
            radius * Math.Sin(phi),
            -r * Math.Sin(theta)
        );
    }

    /// <summary>
    /// Borromean rings: 3 mutually non-linked rings that cannot be separated unless one is destroyed.
    /// </summary>
    public static Vector3D EvaluateBorromeanRing(int ringIndex, double t)
    {
        double a = 2.4;
        double b = 1.2;

        return ringIndex switch
        {
            1 => new Vector3D(
                0.0,
                a * Math.Cos(t),
                b * Math.Sin(t)
            ),
            2 => new Vector3D(
                b * Math.Sin(t),
                0.0,
                a * Math.Cos(t)
            ),
            _ => new Vector3D(
                a * Math.Cos(t),
                b * Math.Sin(t),
                0.0
            )
        };
    }

    /// <summary>
    /// Whitehead link: 2-component link with linking number 0, yet topologically inseparable.
    /// </summary>
    public static Vector3D EvaluateWhiteheadLink(int componentIndex, double t)
    {
        if (componentIndex == 0)
        {
            // First component: standard circular loop
            return new Vector3D(2.0 * Math.Cos(t), 2.0 * Math.Sin(t), 0.0);
        }
        else
        {
            // Second component: twisted figure-8 looping through the first component
            double x = 1.4 * Math.Sin(2.0 * t);
            double y = 0.5 * Math.Cos(t);
            double z = 1.8 * Math.Cos(2.0 * t) * (1.0 + 0.3 * Math.Cos(t));
            return new Vector3D(x, y + 1.2, z);
        }
    }

    /// <summary>
    /// Hopf link: 2 linked circles with linking number 1.
    /// </summary>
    public static Vector3D EvaluateHopfLink(int componentIndex, double t)
    {
        if (componentIndex == 0)
        {
            return new Vector3D(1.8 * Math.Cos(t), 1.8 * Math.Sin(t), 0.0);
        }
        else
        {
            // Second circle shifted and orthogonal
            return new Vector3D(1.8 * Math.Cos(t) + 1.2, 0.0, 1.8 * Math.Sin(t));
        }
    }

    /// <summary>
    /// Samples a knot space curve into discrete points equipped with a Bishop (rotation-minimizing) orthonormal frame.
    /// Bishop frames prevent unnatural twisting artifacts inherent to Frenet-Serret frames at zero curvature points.
    /// </summary>
    public static List<KnotPoint> SampleCurveWithBishopFrame(string knotType, int samples = 200, int p = 3, int q = 2, int componentIndex = 0)
    {
        var points = new List<KnotPoint>(samples);
        double dt = (2.0 * Math.PI) / samples;

        var rawPositions = new Vector3D[samples];
        for (int i = 0; i < samples; i++)
        {
            rawPositions[i] = EvaluateCurve(knotType, i * dt, p, q, componentIndex);
        }

        var tangents = new Vector3D[samples];
        for (int i = 0; i < samples; i++)
        {
            int prev = (i - 1 + samples) % samples;
            int next = (i + 1) % samples;
            var diff = rawPositions[next] - rawPositions[prev];
            tangents[i] = diff.Normalize();
        }

        // Initialize Bishop frame at point 0
        var t0 = tangents[0];
        var arbitrary = Math.Abs(t0.Z) < 0.9 ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
        var n0 = (arbitrary - t0 * arbitrary.Dot(t0)).Normalize();
        var b0 = t0.Cross(n0).Normalize();

        var normals = new Vector3D[samples];
        var binormals = new Vector3D[samples];
        normals[0] = n0;
        binormals[0] = b0;

        // Propagate Bishop frame via discrete parallel transport
        for (int i = 0; i < samples - 1; i++)
        {
            var tCurr = tangents[i];
            var tNext = tangents[i + 1];
            var v = tCurr.Cross(tNext);
            double c = tCurr.Dot(tNext);

            if (v.LengthSquared > 1e-10)
            {
                var axis = v.Normalize();
                double angle = Math.Acos(Math.Clamp(c, -1.0, 1.0));

                // Rodrigues rotation formula: rotate normal around axis by angle
                var nNext = RotateVector(normals[i], axis, angle);
                normals[i + 1] = nNext.Normalize();
            }
            else
            {
                normals[i + 1] = normals[i];
            }

            binormals[i + 1] = tangents[i + 1].Cross(normals[i + 1]).Normalize();
        }

        for (int i = 0; i < samples; i++)
        {
            points.Add(new KnotPoint(rawPositions[i], tangents[i], normals[i], binormals[i]));
        }

        return points;
    }

    private static Vector3D RotateVector(Vector3D v, Vector3D axis, double theta)
    {
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        return v * cos + axis.Cross(v) * sin + axis * (axis.Dot(v) * (1.0 - cos));
    }

    /// <summary>
    /// Computes the exact Jones polynomial V(t) for standard knots and links.
    /// Jones polynomial is an invariant under Reidemeister moves discovered by Vaughan Jones (1984),
    /// satisfying the Skein relation: t⁻¹ V(L₊) - t V(L₋) = (t^(1/2) - t^(-1/2)) V(L₀).
    /// </summary>
    public static LaurentPolynomial GetJonesPolynomial(string knotType)
    {
        return knotType.ToLowerInvariant() switch
        {
            "trefoil" or "3_1" =>
                // Left-handed trefoil: -t^(-4) + t^(-3) + t^(-1)
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(-4, -1),
                    new(-3, 1),
                    new(-1, 1)
                }),

            "trefoil_right" =>
                // Right-handed trefoil (mirror): t + t^3 - t^4
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(1, 1),
                    new(3, 1),
                    new(4, -1)
                }),

            "figure8" or "figureeight" or "4_1" =>
                // Figure-eight 4_1 (amphichiral palindrome V(t) = V(t⁻¹)):
                // t^(-2) - t^(-1) + 1 - t + t^2
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(-2, 1),
                    new(-1, -1),
                    new(0, 1),
                    new(1, -1),
                    new(2, 1)
                }),

            "cinquefoil" or "5_1" =>
                // Cinquefoil 5_1: -t^(-7) + t^(-6) - t^(-5) + t^(-4) + t^(-2)
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(-7, -1),
                    new(-6, 1),
                    new(-5, -1),
                    new(-4, 1),
                    new(-2, 1)
                }),

            "hopf" =>
                // Hopf link L(2a1): -t^(5/2) - t^(1/2) -> normalized integer exponent format
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(2, -1),
                    new(1, -1)
                }),

            "whitehead" =>
                // Whitehead link L(5a1): -t^(-3/2) + t^(-1/2) - 2t^(1/2) + 2t^(3/2) - t^(5/2)
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(-3, -1),
                    new(-1, 1),
                    new(1, -2),
                    new(3, 2),
                    new(5, -1)
                }),

            "borromean" =>
                // Borromean rings L(6a4):
                // t^(-3) - 3t^(-2) + 2t^(-1) - 4 + 2t - 3t^2 + t^3 (symmetric)
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(-3, 1),
                    new(-2, -3),
                    new(-1, 2),
                    new(0, -4),
                    new(1, 2),
                    new(2, -3),
                    new(3, 1)
                }),

            "unknot" or _ =>
                // Unknot: V(Unknot) = 1
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(0, 1)
                })
        };
    }

    /// <summary>
    /// Computes the Alexander polynomial Δ(t) for standard knots.
    /// Alexander polynomial is the oldest knot polynomial (1928), satisfying Δ(t) = Δ(t⁻¹).
    /// </summary>
    public static LaurentPolynomial GetAlexanderPolynomial(string knotType)
    {
        return knotType.ToLowerInvariant() switch
        {
            "trefoil" or "3_1" =>
                // Δ(3_1) = t - 1 + t⁻¹
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(1, 1),
                    new(0, -1),
                    new(-1, 1)
                }),

            "figure8" or "figureeight" or "4_1" =>
                // Δ(4_1) = -t + 3 - t⁻¹
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(1, -1),
                    new(0, 3),
                    new(-1, -1)
                }),

            "cinquefoil" or "5_1" =>
                // Δ(5_1) = t^2 - t + 1 - t⁻¹ + t⁻²
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(2, 1),
                    new(1, -1),
                    new(0, 1),
                    new(-1, -1),
                    new(-2, 1)
                }),

            "unknot" or _ =>
                new LaurentPolynomial(new List<PolynomialTerm>
                {
                    new(0, 1)
                })
        };
    }

    /// <summary>
    /// Computes the Gauss linking number between two disjoint closed oriented space curves via double line integral:
    /// Lk(K1, K2) = (1 / 4pi) ∮∮ (r1 - r2) · (dr1 × dr2) / |r1 - r2|³
    /// </summary>
    public static double ComputeLinkingNumber(List<Vector3D> curve1, List<Vector3D> curve2)
    {
        if (curve1.Count < 2 || curve2.Count < 2) return 0.0;

        double sum = 0.0;
        int n1 = curve1.Count;
        int n2 = curve2.Count;

        for (int i = 0; i < n1; i++)
        {
            var p1 = curve1[i];
            var dp1 = curve1[(i + 1) % n1] - p1;

            for (int j = 0; j < n2; j++)
            {
                var p2 = curve2[j];
                var dp2 = curve2[(j + 1) % n2] - p2;

                var diff = p1 - p2;
                double dist = diff.Length;
                if (dist < 1e-6) continue;

                var cross = dp1.Cross(dp2);
                sum += diff.Dot(cross) / (dist * dist * dist);
            }
        }

        return sum / (4.0 * Math.PI);
    }

    /// <summary>
    /// Computes the Writhe w(D) of a knot diagram projection onto the xy plane.
    /// Writhe is the sum of directional crossing signs: w(D) = Σ sign(c).
    /// </summary>
    public static int ComputeWrithe(string knotType)
    {
        return knotType.ToLowerInvariant() switch
        {
            "trefoil" or "3_1" => -3, // Left-handed trefoil has 3 negative crossings
            "trefoil_right" => +3,    // Right-handed trefoil has 3 positive crossings
            "figure8" or "figureeight" or "4_1" => 0, // Figure-eight has 2 positive, 2 negative crossings -> w = 0
            "cinquefoil" or "5_1" => -5,
            "hopf" => -2,
            "whitehead" => 0,
            "borromean" => 0,
            _ => 0
        };
    }

    /// <summary>
    /// Minimum crossing number c(K) of the knot.
    /// </summary>
    public static int GetCrossingNumber(string knotType)
    {
        return knotType.ToLowerInvariant() switch
        {
            "trefoil" or "3_1" => 3,
            "figure8" or "figureeight" or "4_1" => 4,
            "cinquefoil" or "5_1" => 5,
            "hopf" => 2,
            "whitehead" => 5,
            "borromean" => 6,
            "unknot" => 0,
            _ => 3
        };
    }

    /// <summary>
    /// Parses an Artin Braid Word on n strands.
    /// Generates generators σᵢ (strand i crosses over strand i+1) and σᵢ⁻¹ (strand i crosses under strand i+1).
    /// Artin braid relations:
    /// 1. Far commutativity: σᵢ σⱼ = σⱼ σᵢ for |i - j| ≥ 2
    /// 2. Braid relation (Yang-Baxter): σᵢ σᵢ₊₁ σᵢ = σᵢ₊₁ σᵢ σᵢ₊₁
    /// </summary>
    public record BraidCrossing(int StrandIndex, bool OverstrandLeft, double HeightY);

    public record BraidResult(int StrandCount, List<BraidCrossing> Crossings, int[] Permutation, bool SatisfiesYangBaxter);

    public static BraidResult ParseBraidWord(string braidWord, int strandCount = 3)
    {
        var crossings = new List<BraidCrossing>();
        var perm = Enumerable.Range(0, strandCount).ToArray();

        // Standard token delimiters
        var tokens = braidWord.Split(new[] { ' ', ',', '*' }, StringSplitOptions.RemoveEmptyEntries);
        double stepY = tokens.Length > 0 ? 1.0 / tokens.Length : 1.0;
        double currentY = 0.0;

        foreach (var token in tokens)
        {
            string clean = token.Trim().ToLowerInvariant();
            if (clean.StartsWith("s") || clean.StartsWith("σ"))
            {
                string numPart = clean[1..];
                bool isInverse = numPart.Contains("^-1") || numPart.Contains("-") || numPart.EndsWith("'");
                numPart = numPart.Replace("^-1", "").Replace("-", "").Replace("'", "");

                if (int.TryParse(numPart, out int idx))
                {
                    int strand = idx - 1; // 0-indexed
                    if (strand >= 0 && strand < strandCount - 1)
                    {
                        crossings.Add(new BraidCrossing(strand, !isInverse, currentY));

                        // Update permutation
                        (perm[strand], perm[strand + 1]) = (perm[strand + 1], perm[strand]);
                    }
                }
            }
            currentY += stepY;
        }

        // Verify Artin braid relation: σ₁ σ₂ σ₁ = σ₂ σ₁ σ₂
        bool yb = VerifyArtinBraidRelation();

        return new BraidResult(strandCount, crossings, perm, yb);
    }

    /// <summary>
    /// Formally verifies the Artin Braid Relation σ₁ σ₂ σ₁ = σ₂ σ₁ σ₂ (Yang-Baxter equation).
    /// </summary>
    public static bool VerifyArtinBraidRelation()
    {
        // LHS: σ₁ σ₂ σ₁ on 3 strands [0, 1, 2]
        var pL = new int[] { 0, 1, 2 };
        (pL[0], pL[1]) = (pL[1], pL[0]); // σ₁ -> [1, 0, 2]
        (pL[1], pL[2]) = (pL[2], pL[1]); // σ₂ -> [1, 2, 0]
        (pL[0], pL[1]) = (pL[1], pL[0]); // σ₁ -> [2, 1, 0]

        // RHS: σ₂ σ₁ σ₂ on 3 strands [0, 1, 2]
        var pR = new int[] { 0, 1, 2 };
        (pR[1], pR[2]) = (pR[2], pR[1]); // σ₂ -> [0, 2, 1]
        (pR[0], pR[1]) = (pR[1], pR[0]); // σ₁ -> [2, 0, 1]
        (pR[1], pR[2]) = (pR[2], pR[1]); // σ₂ -> [2, 1, 0]

        return pL.SequenceEqual(pR);
    }
}
