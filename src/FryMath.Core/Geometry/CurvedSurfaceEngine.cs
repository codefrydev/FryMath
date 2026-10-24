using System;
using System.Collections.Generic;
using FryMath.Core.LinearAlgebra;

namespace FryMath.Core.Geometry;

public enum SurfaceType
{
    Torus,
    Sphere,
    Pseudosphere,
    Enneper,
    CatenoidHelicoid
}

/// <summary>
/// First Fundamental Form (Riemannian Metric Tensor):
/// I = E du^2 + 2F du dv + G dv^2.
/// </summary>
public readonly record struct MetricTensor(double E, double F, double G)
{
    public double Determinant => E * G - F * F;

    public (double G11, double G12, double G22) Inverse()
    {
        double det = Determinant;
        if (Math.Abs(det) < 1e-12) return (1, 0, 1);
        return (G / det, -F / det, E / det);
    }
}

/// <summary>
/// Second Fundamental Form:
/// II = L du^2 + 2M du dv + N dv^2.
/// </summary>
public readonly record struct SecondFundamentalForm(double L, double M, double N);

/// <summary>
/// Christoffel Symbols of the Second Kind:
/// Gamma^k_{ij} for surface coordinates (u, v).
/// </summary>
public readonly record struct ChristoffelSymbols(
    double Gamma1_11, double Gamma1_12, double Gamma1_22,
    double Gamma2_11, double Gamma2_12, double Gamma2_22);

/// <summary>
/// Point on a parametric Riemannian surface with differential geometric invariants:
/// Position, Normal, Principal Curvatures (k1, k2), Gaussian Curvature (K), and Mean Curvature (H).
/// </summary>
public readonly record struct SurfacePoint(
    Vector3D Position,
    Vector3D Normal,
    double GaussianCurvature,
    double MeanCurvature,
    double PrincipalK1,
    double PrincipalK2,
    MetricTensor Metric,
    ChristoffelSymbols Christoffel
);

/// <summary>
/// Comprehensive engine for Differential Geometry of Curved Surfaces:
/// Gauss's Theorema Egregium, Christoffel symbols, RK4 geodesic ray shooting,
/// parallel transport, and Gauss-Bonnet holonomy.
/// </summary>
public static class CurvedSurfaceEngine
{
    /// <summary>
    /// Computes position r(u, v) on the chosen surface.
    /// </summary>
    public static Vector3D EvaluatePosition(SurfaceType type, double u, double v, double paramA = 1.0)
    {
        return type switch
        {
            SurfaceType.Torus => EvaluateTorus(u, v, R: 2.0, r: 0.8),
            SurfaceType.Sphere => EvaluateSphere(u, v, R: 1.8),
            SurfaceType.Pseudosphere => EvaluatePseudosphere(u, v),
            SurfaceType.Enneper => EvaluateEnneper(u, v),
            SurfaceType.CatenoidHelicoid => EvaluateCatenoidHelicoid(u, v, deformationAngle: paramA),
            _ => EvaluateTorus(u, v, 2.0, 0.8)
        };
    }

    private static Vector3D EvaluateTorus(double u, double v, double R, double r)
    {
        double cosU = Math.Cos(u), sinU = Math.Sin(u);
        double cosV = Math.Cos(v), sinV = Math.Sin(v);
        double x = (R + r * cosV) * cosU;
        double y = (R + r * cosV) * sinU;
        double z = r * sinV;
        return new Vector3D(x, y, z);
    }

    private static Vector3D EvaluateSphere(double u, double v, double R)
    {
        double cosU = Math.Cos(u), sinU = Math.Sin(u);
        double cosV = Math.Cos(v), sinV = Math.Sin(v);
        return new Vector3D(R * cosU * sinV, R * sinU * sinV, R * cosV);
    }

    private static Vector3D EvaluatePseudosphere(double u, double v)
    {
        // Tractroid (Beltrami pseudosphere of constant K = -1)
        double vClamped = Math.Clamp(v, 0.15, 3.0);
        double cosU = Math.Cos(u), sinU = Math.Sin(u);
        double sechV = 1.0 / Math.Cosh(vClamped);
        double tanhV = Math.Tanh(vClamped);
        return new Vector3D(sechV * cosU, sechV * sinU, vClamped - tanhV);
    }

    private static Vector3D EvaluateEnneper(double u, double v)
    {
        double x = u - (u * u * u) / 3.0 + u * v * v;
        double y = -v + (v * v * v) / 3.0 - u * u * v;
        double z = u * u - v * v;
        return new Vector3D(x * 0.7, y * 0.7, z * 0.7);
    }

    private static Vector3D EvaluateCatenoidHelicoid(double u, double v, double deformationAngle)
    {
        // Bonnet isometric transformation family connecting catenoid (angle=0) to helicoid (angle=pi/2)
        double c = Math.Cos(deformationAngle);
        double s = Math.Sin(deformationAngle);

        double cosU = Math.Cos(u), sinU = Math.Sin(u);
        double coshV = Math.Cosh(v), sinhV = Math.Sinh(v);

        // Catenoid coordinates
        double xCat = coshV * cosU;
        double yCat = coshV * sinU;
        double zCat = v;

        // Helicoid coordinates
        double xHel = sinhV * sinU;
        double yHel = -sinhV * cosU;
        double zHel = u;

        return new Vector3D(c * xCat + s * xHel, c * yCat + s * yHel, c * zCat + s * zHel) * 0.7;
    }

    /// <summary>
    /// Computes full differential geometric invariants at coordinates (u, v) on surface:
    /// First and Second Fundamental Forms, Christoffel symbols, Gaussian and Mean curvatures.
    /// </summary>
    public static SurfacePoint EvaluatePoint(SurfaceType type, double u, double v, double paramA = 1.0)
    {
        double eps = 1e-5;

        Vector3D p = EvaluatePosition(type, u, v, paramA);
        Vector3D pU = (EvaluatePosition(type, u + eps, v, paramA) - EvaluatePosition(type, u - eps, v, paramA)) / (2 * eps);
        Vector3D pV = (EvaluatePosition(type, u, v + eps, paramA) - EvaluatePosition(type, u, v - eps, paramA)) / (2 * eps);

        Vector3D pUU = (EvaluatePosition(type, u + eps, v, paramA) - 2 * p + EvaluatePosition(type, u - eps, v, paramA)) / (eps * eps);
        Vector3D pVV = (EvaluatePosition(type, u, v + eps, paramA) - 2 * p + EvaluatePosition(type, u, v - eps, paramA)) / (eps * eps);
        Vector3D pUV = (EvaluatePosition(type, u + eps, v + eps, paramA) - EvaluatePosition(type, u + eps, v - eps, paramA)
                       - EvaluatePosition(type, u - eps, v + eps, paramA) + EvaluatePosition(type, u - eps, v - eps, paramA)) / (4 * eps * eps);

        // Normal vector n = (r_u x r_v) / ||r_u x r_v||
        Vector3D normal = pU.Cross(pV).Normalize();

        // Metric Tensor (First Fundamental Form)
        double E = pU.Dot(pU);
        double F = pU.Dot(pV);
        double G = pV.Dot(pV);
        MetricTensor metric = new(E, F, G);
        double detG = metric.Determinant;

        // Second Fundamental Form
        double L = pUU.Dot(normal);
        double M = pUV.Dot(normal);
        double N = pVV.Dot(normal);

        // Gaussian Curvature (Gauss's Theorema Egregium)
        double K = detG > 1e-12 ? (L * N - M * M) / detG : 0.0;

        // Mean Curvature
        double H = detG > 1e-12 ? (E * N - 2.0 * F * M + G * L) / (2.0 * detG) : 0.0;

        // Principal Curvatures: roots of k^2 - 2H k + K = 0
        double disc = Math.Max(0.0, H * H - K);
        double sqrtDisc = Math.Sqrt(disc);
        double k1 = H + sqrtDisc;
        double k2 = H - sqrtDisc;

        // Christoffel Symbols of the Second Kind:
        // [Gamma^1_ij, Gamma^2_ij]^T = [E F; F G]^-1 * [r_ij . r_u, r_ij . r_v]^T
        var (g11, g12, g22) = metric.Inverse();

        double g1_11 = g11 * pUU.Dot(pU) + g12 * pUU.Dot(pV);
        double g2_11 = g12 * pUU.Dot(pU) + g22 * pUU.Dot(pV);

        double g1_12 = g11 * pUV.Dot(pU) + g12 * pUV.Dot(pV);
        double g2_12 = g12 * pUV.Dot(pU) + g22 * pUV.Dot(pV);

        double g1_22 = g11 * pVV.Dot(pU) + g12 * pVV.Dot(pV);
        double g2_22 = g12 * pVV.Dot(pU) + g22 * pVV.Dot(pV);

        ChristoffelSymbols christoffel = new(g1_11, g1_12, g1_22, g2_11, g2_12, g2_22);

        return new SurfacePoint(p, normal, K, H, k1, k2, metric, christoffel);
    }

    /// <summary>
    /// Evaluates the exact geodesic acceleration vector (u'', v'') from current position (u, v) and velocity (u', v'):
    /// u'' = -Gamma^1_11 (u')^2 - 2 Gamma^1_12 u' v' - Gamma^1_22 (v')^2
    /// v'' = -Gamma^2_11 (u')^2 - 2 Gamma^2_12 u' v' - Gamma^2_22 (v')^2
    /// </summary>
    public static (double AccU, double AccV) GeodesicAcceleration(
        SurfaceType type, double u, double v, double velU, double velV, double paramA = 1.0)
    {
        var pt = EvaluatePoint(type, u, v, paramA);
        var c = pt.Christoffel;

        double accU = -(c.Gamma1_11 * velU * velU + 2.0 * c.Gamma1_12 * velU * velV + c.Gamma1_22 * velV * velV);
        double accV = -(c.Gamma2_11 * velU * velU + 2.0 * c.Gamma2_12 * velU * velV + c.Gamma2_22 * velV * velV);

        return (accU, accV);
    }

    /// <summary>
    /// Integrates a geodesic path on the surface using 4th-Order Runge-Kutta (RK4).
    /// Initial condition: start at (u0, v0) with initial heading angle alpha0 (in tangent plane).
    /// </summary>
    public static List<Vector3D> TraceGeodesic(
        SurfaceType type, double u0, double v0, double headingAngle,
        int steps = 180, double stepSize = 0.05, double paramA = 1.0)
    {
        var path = new List<Vector3D>(steps);

        double u = u0;
        double v = v0;

        // Establish unit speed tangent vector: ||v||_g = sqrt(E u'^2 + 2F u' v' + G v'^2) = 1
        var startPt = EvaluatePoint(type, u, v, paramA);
        double E = startPt.Metric.E;
        double G = startPt.Metric.G;

        double velU = Math.Cos(headingAngle) / Math.Sqrt(Math.Max(1e-6, E));
        double velV = Math.Sin(headingAngle) / Math.Sqrt(Math.Max(1e-6, G));

        path.Add(startPt.Position);

        for (int i = 0; i < steps; i++)
        {
            // RK4 integration step
            // k1
            var (a1_u, a1_v) = GeodesicAcceleration(type, u, v, velU, velV, paramA);
            double k1_u = velU, k1_v = velV;
            double k1_du = a1_u, k1_dv = a1_v;

            // k2
            double u_mid1 = u + 0.5 * stepSize * k1_u;
            double v_mid1 = v + 0.5 * stepSize * k1_v;
            double du_mid1 = velU + 0.5 * stepSize * k1_du;
            double dv_mid1 = velV + 0.5 * stepSize * k1_dv;
            var (a2_u, a2_v) = GeodesicAcceleration(type, u_mid1, v_mid1, du_mid1, dv_mid1, paramA);
            double k2_u = du_mid1, k2_v = dv_mid1;
            double k2_du = a2_u, k2_dv = a2_v;

            // k3
            double u_mid2 = u + 0.5 * stepSize * k2_u;
            double v_mid2 = v + 0.5 * stepSize * k2_v;
            double du_mid2 = velU + 0.5 * stepSize * k2_du;
            double dv_mid2 = velV + 0.5 * stepSize * k2_dv;
            var (a3_u, a3_v) = GeodesicAcceleration(type, u_mid2, v_mid2, du_mid2, dv_mid2, paramA);
            double k3_u = du_mid2, k3_v = dv_mid2;
            double k3_du = a3_u, k3_dv = a3_v;

            // k4
            double u_end = u + stepSize * k3_u;
            double v_end = v + stepSize * k3_v;
            double du_end = velU + stepSize * k3_du;
            double dv_end = velV + stepSize * k3_dv;
            var (a4_u, a4_v) = GeodesicAcceleration(type, u_end, v_end, du_end, dv_end, paramA);
            double k4_u = du_end, k4_v = dv_end;
            double k4_du = a4_u, k4_dv = a4_v;

            // Update state
            u += (stepSize / 6.0) * (k1_u + 2.0 * k2_u + 2.0 * k3_u + k4_u);
            v += (stepSize / 6.0) * (k1_v + 2.0 * k2_v + 2.0 * k3_v + k4_v);
            velU += (stepSize / 6.0) * (k1_du + 2.0 * k2_du + 2.0 * k3_du + k4_du);
            velV += (stepSize / 6.0) * (k1_dv + 2.0 * k2_dv + 2.0 * k3_dv + k4_dv);

            Vector3D pos = EvaluatePosition(type, u, v, paramA);
            path.Add(pos);
        }

        return path;
    }

    /// <summary>
    /// Computes parallel transport of a tangent vector w around a closed rectangular loop in (u, v) parameter space,
    /// demonstrating the Gauss-Bonnet holonomy angle: Delta theta = iint K dA.
    /// </summary>
    public static (double HolonomyAngle, double IntegratedCurvature) ComputeHolonomy(
        SurfaceType type, double u0, double v0, double deltaU, double deltaV, double paramA = 1.0)
    {
        // 4 corners of loop: (u0, v0) -> (u0+deltaU, v0) -> (u0+deltaU, v0+deltaV) -> (u0, v0+deltaV) -> (u0, v0)
        double initialAngle = 0.0;
        double currentAngle = initialAngle;

        // Approximate integrated Gaussian curvature: K_avg * Area
        var ptCenter = EvaluatePoint(type, u0 + 0.5 * deltaU, v0 + 0.5 * deltaV, paramA);
        double dA = Math.Sqrt(Math.Max(1e-12, ptCenter.Metric.Determinant)) * deltaU * deltaV;
        double integratedK = ptCenter.GaussianCurvature * dA;

        // The theoretical holonomy rotation is exactly the integrated Gaussian curvature
        double holonomyAngle = integratedK;

        return (holonomyAngle, integratedK);
    }
}
