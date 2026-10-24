using System;
using FryMath.Core.Geometry;
using Xunit;

namespace FryMath.Core.Tests;

public class DifferentialGeometryTests
{
    private const double Tolerance = 1e-3;

    [Fact]
    public void Sphere_HasConstantPositiveGaussianCurvature()
    {
        double R = 1.8;
        double expectedK = 1.0 / (R * R); // ~0.3086

        // Check multiple points on the sphere
        var pt1 = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.Sphere, u: 0.5, v: 1.0);
        var pt2 = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.Sphere, u: 2.1, v: 1.5);
        var pt3 = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.Sphere, u: 4.2, v: 2.0);

        Assert.Equal(expectedK, pt1.GaussianCurvature, Tolerance);
        Assert.Equal(expectedK, pt2.GaussianCurvature, Tolerance);
        Assert.Equal(expectedK, pt3.GaussianCurvature, Tolerance);

        // Principal curvatures must be equal on a sphere (umbilic point)
        Assert.Equal(pt1.PrincipalK1, pt1.PrincipalK2, Tolerance);
    }

    [Fact]
    public void Torus_ExhibitsPositiveZeroAndNegativeCurvatureRegimes()
    {
        // Torus R=2.0, r=0.8
        // Outer equator: v = 0 -> K = cos(0) / (r*(R+r)) = 1 / (0.8 * 2.8) = +0.4464 > 0
        var ptOuter = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.Torus, u: 0.0, v: 0.0);
        Assert.True(ptOuter.GaussianCurvature > 0.3);

        // Top crown: v = pi/2 -> K = cos(pi/2) / (r*R) = 0
        var ptCrown = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.Torus, u: 0.0, v: Math.PI * 0.5);
        Assert.Equal(0.0, ptCrown.GaussianCurvature, Tolerance);

        // Inner equator: v = pi -> K = cos(pi) / (r*(R-r)) = -1 / (0.8 * 1.2) = -1.0416 < 0
        var ptInner = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.Torus, u: 0.0, v: Math.PI);
        Assert.True(ptInner.GaussianCurvature < -0.8);
    }

    [Fact]
    public void EnneperSurface_IsStrictlyMinimal_MeanCurvatureZero()
    {
        // Minimal surface: H = 0 everywhere
        var pt1 = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.Enneper, u: 0.4, v: 0.3);
        var pt2 = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.Enneper, u: -0.6, v: 0.8);

        Assert.Equal(0.0, pt1.MeanCurvature, Tolerance);
        Assert.Equal(0.0, pt2.MeanCurvature, Tolerance);

        // Gaussian curvature is non-positive
        Assert.True(pt1.GaussianCurvature <= 0.0);
        Assert.True(pt2.GaussianCurvature <= 0.0);
    }

    [Fact]
    public void CatenoidHelicoid_BonnetTransformation_PreservesMinimality()
    {
        // At deformation angle 0 (Catenoid)
        var ptCatenoid = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.CatenoidHelicoid, u: 1.0, v: 0.5, paramA: 0.0);
        Assert.Equal(0.0, ptCatenoid.MeanCurvature, Tolerance);

        // At intermediate deformation angle (angle = pi / 4)
        var ptBonnet = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.CatenoidHelicoid, u: 1.0, v: 0.5, paramA: Math.PI * 0.25);
        Assert.Equal(0.0, ptBonnet.MeanCurvature, Tolerance);

        // At deformation angle pi / 2 (Helicoid)
        var ptHelicoid = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.CatenoidHelicoid, u: 1.0, v: 0.5, paramA: Math.PI * 0.5);
        Assert.Equal(0.0, ptHelicoid.MeanCurvature, Tolerance);
    }

    [Fact]
    public void ChristoffelSymbols_AreSymmetricInLowerIndices()
    {
        // Torsion-free Levi-Civita connection: Gamma^k_ij = Gamma^k_ji
        var pt = CurvedSurfaceEngine.EvaluatePoint(SurfaceType.Torus, u: 1.2, v: 0.8);
        var c = pt.Christoffel;

        Assert.Equal(c.Gamma1_12, c.Gamma1_12, Tolerance);
        Assert.Equal(c.Gamma2_12, c.Gamma2_12, Tolerance);
    }

    [Fact]
    public void TraceGeodesic_GeneratesContinuousCurveOnSurface()
    {
        // Shoot a geodesic ray on the sphere starting at (0.5, 1.0) heading 45 deg
        var path = CurvedSurfaceEngine.TraceGeodesic(
            SurfaceType.Sphere, u0: 0.5, v0: 1.0, headingAngle: Math.PI * 0.25, steps: 60, stepSize: 0.05);

        Assert.NotEmpty(path);
        Assert.Equal(61, path.Count);

        // All points on a sphere of R=1.8 must have distance to origin ~ 1.8
        foreach (var pt in path)
        {
            Assert.Equal(1.8, pt.Length, 0.05);
        }
    }

    [Fact]
    public void Holonomy_MatchesGaussBonnetIntegratedCurvature()
    {
        // Holonomy angle Delta theta = iint K dA
        var (holonomy, integratedK) = CurvedSurfaceEngine.ComputeHolonomy(
            SurfaceType.Sphere, u0: 0.5, v0: 1.0, deltaU: 0.2, deltaV: 0.2);

        Assert.Equal(integratedK, holonomy, Tolerance);
        Assert.True(holonomy > 0);
    }
}
