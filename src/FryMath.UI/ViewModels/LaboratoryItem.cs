using System;
using System.Collections.Generic;

namespace FryMath.UI.ViewModels;

public sealed record LaboratoryItem(
    string Name,
    string ShortName,
    string Domain,
    string IconKind,
    string Description
);

public static class LaboratoryCatalog
{
    public static IReadOnlyList<string> Domains { get; } = new[]
    {
        "Calculus & Analysis",
        "Algebra & Geometry",
        "Physics & Dynamics",
        "Chaos & Fractals",
        "Probability & Codes",
        "Topology & Frontiers"
    };

    public static IReadOnlyList<LaboratoryItem> AllLaboratories { get; } = new List<LaboratoryItem>
    {
        // 1. Calculus & Analysis
        new("Visual Calculus", "Calculus", "Calculus & Analysis", "ChartBellCurve", "Multi-curve graphing, derivatives, integrals, Taylor series & osculating circle"),
        new("Fourier & Waves", "Waves", "Calculus & Analysis", "Waveform", "DFT phasors, epicycles, harmonic audio synthesis & frequency spectra"),
        new("Vector Fields & ODEs", "Flow & ODEs", "Calculus & Analysis", "WeatherWindy", "Dynamic phase flows, streamlines, divergence & curl fields"),
        new("Complex & Conformal", "Complex", "Calculus & Analysis", "CircleDouble", "Domain coloring, Möbius transformations & conformal mappings"),
        new("Zeta & Primes", "Zeta & Primes", "Calculus & Analysis", "Sigma", "Riemann zeta critical strip, Gram points & non-trivial zeros"),

        // 2. Algebra & Geometry
        new("Linear Algebra", "Matrices", "Algebra & Geometry", "VectorRectangle", "2D spatial warping, basis vectors, determinants & eigenvectors"),
        new("Hyperbolic Geometry", "Hyperbolic", "Algebra & Geometry", "AngleAcute", "Poincaré disk & Klein projections with regular {p,q} tilings"),
        new("Clifford Geometric Algebra", "Clifford", "Algebra & Geometry", "VectorPolygon", "Multivectors, rotors, reflections & Maxwell spacetime algebra"),
        new("Differential Geometry", "Diff Geometry", "Algebra & Geometry", "Earth", "Curved surfaces, geodesics, Christoffel symbols & Gaussian curvature"),
        new("Lie Algebras & Dynkin", "Lie Algebras", "Algebra & Geometry", "GraphOutline", "Root systems, Weyl chambers, Cartan matrices & E8 exceptional algebra"),

        // 3. Physics & Dynamics
        new("PDE & Waves", "PDE Waves", "Physics & Dynamics", "Waves", "2D wave equation, Chladni nodal sand patterns & Bessel drum membranes"),
        new("Heat & Diffusion", "Heat Diffusion", "Physics & Dynamics", "Fire", "2D heat equation, thermal flux vectors & isothermal contour lines"),
        new("Quantum & Waves", "Quantum", "Physics & Dynamics", "Atom", "Schrödinger split-operator solver, tunneling & harmonic wavepackets"),
        new("Relativity & Spacetime", "Spacetime", "Physics & Dynamics", "ClockOutline", "Minkowski spacetime, light cones, twin paradox & Schwarzschild geodesics"),
        new("Fluid Dynamics", "Fluids", "Physics & Dynamics", "WaterOutline", "Navier-Stokes vorticity, pressure Poisson & Kármán vortex streets"),
        new("Celestial & N-Body", "Celestial", "Physics & Dynamics", "Orbit", "Symplectic gravitational orbits, Lagrange points & figure-8 3-body choreographies"),
        new("Classical Mechanics", "Lagrangian", "Physics & Dynamics", "AxisArrow", "Euler-Lagrange variational action & chaotic double pendulum"),
        new("Symplectic & Hamiltonian", "Hamiltonian", "Physics & Dynamics", "CompassRose", "Canonical phase space (q,p), Liouville volume & Hénon-Heiles chaos"),

        // 4. Chaos & Fractals
        new("Dynamical Chaos", "Chaos", "Chaos & Fractals", "WeatherHurricane", "3D Lorenz attractor, butterfly effect & dual-orbit divergence"),
        new("Fractals & Chaos", "Fractals", "Chaos & Fractals", "ShapePlus", "Mandelbrot & Julia set deep zoom with continuous potential rendering"),
        new("Discrete Dynamics & Chaos", "Dynamics", "Chaos & Fractals", "ChartTimelineVariant", "Arnold's cat map, logistic cobwebs & Barnsley fern IFS"),
        new("Bifurcation & Catastrophe Theory", "Catastrophe", "Chaos & Fractals", "ChartBellCurveCumulative", "Thom's cusp, fold & butterfly catastrophe surfaces"),

        // 5. Probability & Codes
        new("Probability & Stats", "Plinko", "Probability & Codes", "DotsGrid", "Galton pin board physics, central limit theorem & binomial Gaussian fit"),
        new("Information & Entropy", "Information", "Probability & Codes", "InformationOutline", "Shannon entropy, Huffman encoding & binary symmetric channels"),
        new("Cryptography & Finite Fields", "Crypto", "Probability & Codes", "KeyOutline", "Elliptic curves y²=x³+ax+b over ℝ and 𝔽ₚ, ECDH key exchange"),

        // 6. Topology & Frontiers
        new("Surfaces & 3D", "3D Surfaces", "Topology & Frontiers", "CubeOutline", "Interactive 3D quad meshes, tangent planes & Hessian extrema"),
        new("Space Curves 3D", "Space Curves", "Topology & Frontiers", "VectorPolyline", "3D space curves, Frenet-Serret frame & osculating circles"),
        new("Topology & 4D Manifolds", "4D Manifolds", "Topology & Frontiers", "AxisSquare", "Tesseract & Clifford torus projected via 4D double rotations"),
        new("Knot Theory & Invariants", "Knot Theory", "Topology & Frontiers", "Gesture", "3D knots, braids, Alexander polynomials & Reidemeister moves"),
        new("Algebraic Topology & Homology", "Homology", "Topology & Frontiers", "ShapePolygonPlus", "Simplicial complexes, boundary operators ∂²=0 & Betti numbers"),
        new("Millennium Problems", "👑 $7M Clay", "Topology & Frontiers", "CrownOutline", "The 7 Clay Millennium Prize Problems & Clay Mathematics Institute challenges")
    };

    public static string GetDomain(string labName)
    {
        foreach (var item in AllLaboratories)
        {
            if (string.Equals(item.Name, labName, StringComparison.OrdinalIgnoreCase))
                return item.Domain;
        }
        return "Calculus & Analysis";
    }
}
