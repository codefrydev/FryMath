using System;
using FryMath.Core.Clifford;
using FryMath.Core.LinearAlgebra;
using Xunit;

namespace FryMath.Core.Tests;

public class CliffordTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void BasisBlades_SquareProperties_AreExact()
    {
        // Vectors square to +1
        var e1 = Multivector3.E1_Basis;
        var e2 = Multivector3.E2_Basis;
        var e3 = Multivector3.E3_Basis;

        var e1Sq = e1 * e1;
        var e2Sq = e2 * e2;
        var e3Sq = e3 * e3;

        Assert.Equal(1.0, e1Sq.Scalar, Tolerance);
        Assert.Equal(1.0, e2Sq.Scalar, Tolerance);
        Assert.Equal(1.0, e3Sq.Scalar, Tolerance);

        // Bivectors square to -1
        var e12 = Multivector3.E12_Basis;
        var e23 = Multivector3.E23_Basis;
        var e31 = Multivector3.E31_Basis;

        var e12Sq = e12 * e12;
        var e23Sq = e23 * e23;
        var e31Sq = e31 * e31;

        Assert.Equal(-1.0, e12Sq.Scalar, Tolerance);
        Assert.Equal(-1.0, e23Sq.Scalar, Tolerance);
        Assert.Equal(-1.0, e31Sq.Scalar, Tolerance);

        // Pseudoscalar squares to -1
        var I = Multivector3.I_Basis;
        var ISq = I * I;
        Assert.Equal(-1.0, ISq.Scalar, Tolerance);
    }

    [Fact]
    public void BasisVectors_Anticommute_AndProduceBivectors()
    {
        var e1 = Multivector3.E1_Basis;
        var e2 = Multivector3.E2_Basis;

        var e1e2 = e1 * e2;
        var e2e1 = e2 * e1;

        // e1*e2 = e12
        Assert.Equal(1.0, e1e2.E12, Tolerance);
        Assert.Equal(0.0, e1e2.Scalar, Tolerance);

        // e2*e1 = -e12
        Assert.Equal(-1.0, e2e1.E12, Tolerance);
        Assert.Equal(0.0, e2e1.Scalar, Tolerance);

        // Anticommute: e1*e2 + e2*e1 = 0
        var sum = e1e2 + e2e1;
        Assert.Equal(0.0, sum.Norm, Tolerance);
    }

    [Fact]
    public void GeometricProduct_DecomposesIntoInnerAndWedge()
    {
        var a = new Vector3D(2, 3, -1);
        var b = new Vector3D(1, -2, 4);

        var aMult = new Multivector3(a);
        var bMult = new Multivector3(b);

        var ab = aMult * bMult;

        double expectedDot = a.Dot(b);
        var expectedWedge = CliffordEngine.Wedge(a, b);

        Assert.Equal(expectedDot, ab.Scalar, Tolerance);
        Assert.Equal(expectedWedge.E12, ab.E12, Tolerance);
        Assert.Equal(expectedWedge.E23, ab.E23, Tolerance);
        Assert.Equal(expectedWedge.E31, ab.E31, Tolerance);

        // Vector and pseudoscalar parts must be zero
        Assert.Equal(0.0, ab.E1, Tolerance);
        Assert.Equal(0.0, ab.E2, Tolerance);
        Assert.Equal(0.0, ab.E3, Tolerance);
        Assert.Equal(0.0, ab.Pseudoscalar, Tolerance);
    }

    [Fact]
    public void DualVector_MatchesClassicalCrossProduct()
    {
        var a = new Vector3D(3, 1, -2);
        var b = new Vector3D(-1, 4, 5);

        // Wedge product bivector
        var wedge = CliffordEngine.Wedge(a, b);

        // Dual vector: n = -I * (a ^ b)
        var dual = wedge.Dual();

        // Classical cross product: a x b
        var cross = a.Cross(b);

        Assert.Equal(cross.X, dual.X, Tolerance);
        Assert.Equal(cross.Y, dual.Y, Tolerance);
        Assert.Equal(cross.Z, dual.Z, Tolerance);
    }

    [Fact]
    public void RotorSandwichProduct_RotatesInPlane_PreservingLength()
    {
        // 90 degree rotation in XY plane (e12)
        var plane = Bivector3D.Unit12;
        double angle = Math.PI * 0.5;
        var rotor = Rotor3D.FromAngleAndPlane(angle, plane);

        var v = new Vector3D(1, 0, 0); // Along X
        var rotated = rotor.Rotate(v);

        // Should rotate to Y (0, 1, 0)
        Assert.Equal(0.0, rotated.X, 1e-6);
        Assert.Equal(1.0, rotated.Y, 1e-6);
        Assert.Equal(0.0, rotated.Z, 1e-6);
        Assert.Equal(v.Length, rotated.Length, 1e-6);
    }

    [Fact]
    public void OrthogonalVector_IsInvariantUnderRotor()
    {
        // Rotation in XY plane (e12) by arbitrary angle 42 degrees
        var plane = Bivector3D.Unit12;
        double angle = 0.733; // ~42 deg
        var rotor = Rotor3D.FromAngleAndPlane(angle, plane);

        // Vector along Z axis (orthogonal to XY plane)
        var vz = new Vector3D(0, 0, 5.0);
        var rotated = rotor.Rotate(vz);

        Assert.Equal(0.0, rotated.X, Tolerance);
        Assert.Equal(0.0, rotated.Y, Tolerance);
        Assert.Equal(5.0, rotated.Z, Tolerance);
    }

    [Fact]
    public void DoubleReflection_EqualsRotorRotation_CartanDieudonne()
    {
        // Two reflection mirrors with 30 degree angle between them
        var n1 = new Vector3D(1, 0, 0); // X mirror
        var n2 = new Vector3D(Math.Cos(Math.PI / 6), Math.Sin(Math.PI / 6), 0); // 30 deg mirror

        var testVector = new Vector3D(2, 3, 1);

        var (reflectedTwice, rotor) = CliffordEngine.DoubleReflect(testVector, n1, n2);
        var rotorRotated = rotor.Rotate(testVector);

        // Two mirrors at 30 deg must rotate the vector by 2 * 30 = 60 deg
        Assert.Equal(reflectedTwice.X, rotorRotated.X, 1e-7);
        Assert.Equal(reflectedTwice.Y, rotorRotated.Y, 1e-7);
        Assert.Equal(reflectedTwice.Z, rotorRotated.Z, 1e-7);
    }

    [Fact]
    public void SlerpInterpolation_InterpolatesBetweenRotorsSmoothly()
    {
        var r0 = Rotor3D.Identity;
        var r1 = Rotor3D.FromAngleAndPlane(Math.PI * 0.5, Bivector3D.Unit12);

        var midRotor = Rotor3D.Slerp(r0, r1, 0.5);

        // Vector along X
        var v = new Vector3D(1, 0, 0);
        var rotatedMid = midRotor.Rotate(v);

        // At t=0.5, angle should be 45 degrees
        double expectedX = Math.Cos(Math.PI * 0.25);
        double expectedY = Math.Sin(Math.PI * 0.25);

        Assert.Equal(expectedX, rotatedMid.X, 1e-5);
        Assert.Equal(expectedY, rotatedMid.Y, 1e-5);
    }

    [Fact]
    public void MaxwellInvariants_ComputeLagrangianAndTopologicalCharge()
    {
        var E = new Vector3D(0, 3, 0);
        var B = new Vector3D(0, 0, 2);

        var (lagrangian, topo) = CliffordEngine.MaxwellInvariants(E, B, c: 1.0);

        // Lagrangian: 0.5 * (E^2 - c^2 B^2) = 0.5 * (9 - 4) = 2.5
        Assert.Equal(2.5, lagrangian, Tolerance);

        // E . B = 0 (perpendicular fields) -> Topological charge = 0
        Assert.Equal(0.0, topo, Tolerance);
    }

    [Fact]
    public void SpacetimeBoost_PreservesMinkowskiInvariantInterval()
    {
        double ct = 5.0;
        double x = 3.0;
        double beta = 0.6; // 0.6 c

        var (ctPrime, xPrime, gamma, rapidity) = CliffordEngine.SpacetimeLorentzBoost(ct, x, beta);

        double s2Original = ct * ct - x * x; // 25 - 9 = 16
        double s2Prime = ctPrime * ctPrime - xPrime * xPrime;

        Assert.Equal(s2Original, s2Prime, 1e-7);
        Assert.Equal(1.25, gamma, 1e-7);
    }
}
