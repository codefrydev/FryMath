using System;
using System.Collections.Generic;

namespace FryMath.Core.Presets;

public sealed record MathPreset(
    string Id,
    string Title,
    string Category,
    string Description,
    string ExpressionOrMatrix,
    string IconKind,
    double DefaultParamA = 1.0,
    double DefaultParamB = 0.0,
    double DefaultParamC = 0.0
);

public static class MathPresetCatalog
{
    public static IReadOnlyList<MathPreset> AllPresets { get; } = new List<MathPreset>
    {
        // 1. Calculus & Analysis
        new(
            "calc.cubic.tangents",
            "Cubic Polynomial & Tangent Slope",
            "Visual Calculus",
            "Explore how secant lines collapse into tangents, find local extrema, and trace the osculating circle.",
            "x^3 - 3*x",
            "ChartBellCurve",
            1.0, 0.0, 0.0
        ),
        new(
            "calc.riemann.sine",
            "Riemann Sums & Area Under Sine",
            "Visual Calculus",
            "Adjust partition slider N to see lower and upper Darboux rectangles converge to exact definite integral.",
            "sin(x) + 1.2",
            "ChartAreaspline",
            1.0, 0.0, 0.0
        ),
        new(
            "calc.damped.oscillation",
            "Damped Harmonic Oscillation",
            "Visual Calculus",
            "Damped oscillator decaying exponentially with high-frequency oscillation.",
            "exp(-0.25*x) * sin(2*x)",
            "SineWave",
            0.25, 2.0, 0.0
        ),
        new(
            "calc.gaussian",
            "Gaussian Bell Curve & Error Function",
            "Visual Calculus",
            "The fundamental normal probability curve exp(-x²). Area under the entire real line is √π.",
            "exp(-x^2)",
            "ChartBellCurveCumulative",
            1.0, 0.0, 0.0
        ),

        // 2. Linear Algebra
        new(
            "linalg.identity",
            "Identity Basis & Standard Span",
            "Linear Algebra",
            "Standard unit basis vectors i-hat (1,0) and j-hat (0,1). Area = 1.0.",
            "[[1, 0], [0, 1]]",
            "VectorRectangle",
            1.0, 0.0, 0.0
        ),
        new(
            "linalg.rotation",
            "Pure 45° Coordinate Rotation",
            "Linear Algebra",
            "Isometry preserving lengths and angles. Determinant remains exactly 1.0.",
            "[[0.71, -0.71], [0.71, 0.71]]",
            "RotateRight",
            45.0, 0.0, 0.0
        ),
        new(
            "linalg.shear",
            "Horizontal Shear Transformation",
            "Linear Algebra",
            "Sliding parallel planes with invariant horizontal axis. Area is preserved.",
            "[[1, 1.2], [0, 1]]",
            "Skew",
            1.2, 0.0, 0.0
        ),
        new(
            "linalg.invert",
            "Diagonal Reflection (Negative Determinant)",
            "Linear Algebra",
            "Reflects across diagonal y=x. Swaps orientation of space, giving determinant -1.",
            "[[0, 1], [1, 0]]",
            "FlipHorizontal",
            -1.0, 0.0, 0.0
        ),
        new(
            "linalg.singular",
            "Singular Collapse (Determinant = 0)",
            "Linear Algebra",
            "2D space collapses onto a 1D line. Determinant drops to 0; matrix has no inverse.",
            "[[1, 2], [0.5, 1]]",
            "ArrowCollapseAll",
            0.0, 0.0, 0.0
        ),

        // 3. Fourier Waves
        new(
            "fourier.square",
            "Square Wave (Odd Harmonics)",
            "Fourier & Waves",
            "Reconstructs square pulses using odd harmonics: sin(x) + 1/3 sin(3x) + 1/5 sin(5x)...",
            "square",
            "Waveform",
            5.0, 220.0, 0.0
        ),
        new(
            "fourier.sawtooth",
            "Sawtooth Ramp Wave",
            "Fourier & Waves",
            "Full harmonic spectrum with alternating signs creating a sharp linear ramp.",
            "sawtooth",
            "VectorLine",
            7.0, 220.0, 0.0
        ),
        new(
            "fourier.heart",
            "Cardioid Heart Epicycles",
            "Fourier & Waves",
            "Parametric 2D heart curve decomposed into rotating phasor arms.",
            "heart",
            "HeartOutline",
            15.0, 0.0, 0.0
        ),

        // 4. Probability
        new(
            "prob.galton.fair",
            "Galton Board Fair Plinko (p = 0.5)",
            "Probability & Stats",
            "Unbiased binomial trials generating the symmetrical Gaussian bell curve.",
            "p=0.5",
            "DotsGrid",
            0.5, 12.0, 0.0
        ),
        new(
            "prob.galton.skewed",
            "Skewed Galton Board (p = 0.7)",
            "Probability & Stats",
            "Biased rightward deflects demonstrating asymmetrical distribution.",
            "p=0.7",
            "ChartTimelineVariant",
            0.7, 12.0, 0.0
        ),

        // 5. Chaos & Dynamics
        new(
            "chaos.lorenz.butterfly",
            "Lorenz Strange Attractor (Chaos)",
            "Dynamical Chaos",
            "Atmospheric convection model exhibiting deterministic chaos and fractal dimension.",
            "sigma=10, rho=28, beta=8/3",
            "WeatherWindy",
            10.0, 28.0, 2.667
        ),

        // 6. Vector Fields & Autonomous ODEs
        new(
            "ode.vortex",
            "Harmonic Vortex Flow",
            "Vector Fields & ODEs",
            "Pure rotational vector field with zero divergence and constant curl.",
            "-y, x",
            "WeatherHurricane"
        ),
        new(
            "ode.pendulum",
            "Nonlinear Damped Pendulum",
            "Vector Fields & ODEs",
            "Phase plane showing stable spiral attractor and unstable saddle separatrix.",
            "y, -sin(x) - 0.25*y",
            "VectorPolyline"
        ),
        new(
            "ode.predator",
            "Lotka-Volterra Predator-Prey",
            "Vector Fields & ODEs",
            "Closed ecological limit orbits between prey x and predator y populations.",
            "x*(1 - 0.5*y), -y*(0.75 - 0.5*x)",
            "ChartLine"
        ),
        new(
            "ode.vanderpol",
            "Van der Pol Limit Cycle",
            "Vector Fields & ODEs",
            "Self-oscillating dynamical system exhibiting relaxation oscillations.",
            "y, 1.5*(1 - x^2)*y - x",
            "Sync"
        ),

        // 7. Complex Analysis & Conformal Mapping
        new(
            "complex.z2",
            "Complex Quadratic Map (z²)",
            "Complex & Conformal",
            "Doubles polar phase argument and squares modulus. Second-order branch point at origin.",
            "z^2",
            "CircleDouble"
        ),
        new(
            "complex.roots",
            "Roots of Unity (z³ - 1)",
            "Complex & Conformal",
            "Fundamental Theorem of Algebra: 3 symmetric zeros on unit circle separated by 120°.",
            "z^3 - 1",
            "ShieldStarOutline"
        ),
        new(
            "complex.exp",
            "Euler Exponential Map (eᶻ)",
            "Complex & Conformal",
            "e^(x+iy) = e^x(cos y + i sin y). Maps orthogonal coordinate grid into polar rays and circles.",
            "exp(z)",
            "Waveform"
        ),
        new(
            "complex.pole",
            "Simple Inversion Pole (1/z)",
            "Complex & Conformal",
            "Complex inversion mapping interior of unit disk to exterior and origin to infinity.",
            "1/z",
            "MagnetOn"
        ),
        new(
            "complex.mobius",
            "Möbius Conformal Map",
            "Complex & Conformal",
            "Fractional linear automorphism (z-1)/(z+1) mapping imaginary axis to unit circle.",
            "(z - 1)/(z + 1)",
            "Sphere"
        ),
        new(
            "complex.joukowsky",
            "Joukowsky Aerodynamic Airfoil",
            "Complex & Conformal",
            "Conformal transformation 0.5*(z + 1/z) mapping circles into aerodynamic wings.",
            "0.5 * (z + 1/z)",
            "AirplaneTakeoff"
        ),

        // 8. 3D Multivariable Calculus & Surface Geometry
        new(
            "surface.paraboloid",
            "Elliptic Paraboloid",
            "Surfaces & 3D",
            "Smooth convex bowl with global minimum at origin. Positive definite Hessian D > 0.",
            "0.5 * (x^2 + y^2)",
            "BowlOutline"
        ),
        new(
            "surface.saddle",
            "Hyperbolic Saddle",
            "Surfaces & 3D",
            "Classic mountain pass geometry with negative Gaussian curvature. Hessian D = -1 < 0.",
            "0.5 * (x^2 - y^2)",
            "PerspectiveLess"
        ),
        new(
            "surface.sombrero",
            "Sombrero Radial Ripple",
            "Surfaces & 3D",
            "Circularly symmetric sinc wave radiating outward from central global peak.",
            "2.5 * sin(2*r) / (2*r)",
            "Waveform"
        ),
        new(
            "surface.gaussian",
            "2D Gaussian Hill",
            "Surfaces & 3D",
            "Bivariate normal distribution surface with radial exponential decay.",
            "3 * exp(-0.4*(x^2 + y^2))",
            "ChartBellCurve"
        ),
        new(
            "surface.monkey",
            "Monkey Saddle",
            "Surfaces & 3D",
            "Three downward depressions (for legs and tail) with degenerate Hessian D = 0.",
            "0.25 * (x^3 - 3*x*y^2)",
            "VectorTriangle"
        ),
        new(
            "surface.wave",
            "Orthogonal Wave Ripple",
            "Surfaces & 3D",
            "Periodic interference lattice of alternating crests, troughs, and saddles.",
            "1.5 * sin(x) * cos(y)",
            "Waves"
        ),

        // 9. Fractal Geometry & Complex Dynamics
        new(
            "fractal.mandelbrot",
            "Mandelbrot Connectedness Locus",
            "Fractals & Chaos",
            "The dictionary of all Julia sets. Boundary exhibits infinite self-similar filament geometry.",
            "z_{n+1} = z_n^2 + c",
            "ShapePlus"
        ),
        new(
            "fractal.seahorse",
            "Seahorse Valley (Deep Zoom)",
            "Fractals & Chaos",
            "Intricate logarithmic spirals and miniature Mandelbrots between period-1 and period-2 bulbs.",
            "c = -0.7436 + 0.1318i",
            "Spiral"
        ),
        new(
            "fractal.elephant",
            "Elephant Valley",
            "Fractals & Chaos",
            "Trunks and scrolls along the positive real boundary of the main cardioid.",
            "c = 0.274 + 0.006i",
            "TextureBox"
        ),
        new(
            "fractal.douady",
            "Douady's Rabbit Julia Set",
            "Fractals & Chaos",
            "3-fold rotational pinch-point symmetry corresponding to period-3 bulb parameter.",
            "c = -0.123 + 0.745i",
            "Rabbit"
        ),
        new(
            "fractal.sanmarco",
            "San Marco Basilica Julia Set",
            "Fractals & Chaos",
            "Named by Mandelbrot after the Venetian basilica's arches and spires.",
            "c = -0.75 + 0.0i",
            "Church"
        ),
        new(
            "fractal.newton",
            "Newton-Raphson Basins (z³ - 1 = 0)",
            "Fractals & Chaos",
            "Basins of attraction partitioned by fractal boundaries where root selection is chaotic.",
            "z_{n+1} = (2z + 1/z^2)/3",
            "Atom"
        ),
        new(
            "fractal.burningship",
            "The Burning Ship Fractal",
            "Fractals & Chaos",
            "Non-analytic variant with absolute values (|Re(z)| + i|Im(z)|)^2 + c producing maritime mast shapes.",
            "(|Re| + i|Im|)^2 + c",
            "SailBoat"
        ),

        // 10. Wave Mechanics & Chladni Resonance (PDEs)
        new(
            "wave.chladni35",
            "Chladni Intricate Rosette",
            "PDE & Waves",
            "Acoustic 2D square plate eigenmode (n=3, m=5). Golden sand particles collect strictly on zero-vibration nodal lines.",
            "cos(3x)cos(5y) - cos(5x)cos(3y)",
            "Flare"
        ),
        new(
            "wave.chladni13",
            "Chladni Ring & Central Cross",
            "PDE & Waves",
            "Fundamental symmetric acoustic resonance (n=1, m=3) forming an inner nodal loop and cross.",
            "cos(x)cos(3y) - cos(3x)cos(y)",
            "CrosshairsGps"
        ),
        new(
            "wave.chladni24",
            "Chladni Clover Lattice",
            "PDE & Waves",
            "Fourfold cloverleaf nodal partitions with checkerboard antinode phases (n=2, m=4).",
            "cos(2x)cos(4y) - cos(4x)cos(2y)",
            "Clover"
        ),
        new(
            "wave.drum",
            "Circular Drum Bessel Mode",
            "PDE & Waves",
            "Clamped circular membrane vibrating in Bessel mode J_m(α r)cos(m θ) with zero boundary displacement.",
            "J_m(α r) cos(m θ)",
            "Disc"
        ),
        new(
            "wave.ripple",
            "2D Wave Tank Leapfrog PDE",
            "PDE & Waves",
            "Interactive finite-difference 2D wave equation simulation. Click/drag to drop wave droplets and observe interference.",
            "u_{tt} = c^2 ∇^2 u - γ u_t",
            "Water"
        ),

        // 11. Differential Geometry & 3D Space Curves
        new(
            "curve.helix",
            "Circular Helix & Frenet Frame",
            "Space Curves 3D",
            "Spatial spiral with constant curvature κ = a/(a²+b²) and constant torsion τ = b/(a²+b²).",
            "⟨2 cos t, 2 sin t, 0.6 t⟩",
            "Spiral"
        ),
        new(
            "curve.trefoil",
            "Trefoil Knot (3₁)",
            "Space Curves 3D",
            "Simplest non-trivial mathematical knot, smoothly knotted space curve embedded in 3D.",
            "⟨sin t + 2 sin 2t, cos t - 2 cos 2t, -sin 3t⟩",
            "VectorPolyline"
        ),
        new(
            "curve.viviani",
            "Viviani's Window Curve",
            "Space Curves 3D",
            "Figure-eight curve formed by the intersection of a sphere with a tangent cylinder.",
            "⟨1 + cos t, sin t, 2 sin(t/2)⟩",
            "Infinity"
        ),
        new(
            "curve.torusknot",
            "Torus Knot (2, 3)",
            "Space Curves 3D",
            "Closed loop winding 2 times azimuthally and 3 times poloidally around a doughnut torus.",
            "⟨(2+cos 3t)cos 2t, (2+cos 3t)sin 2t, sin 3t⟩",
            "ChartDonut"
        ),
        new(
            "curve.twistedcubic",
            "Twisted Rational Cubic",
            "Space Curves 3D",
            "Smooth rational cubic space curve r(t) = ⟨t, t², t³⟩ with non-vanishing torsion.",
            "⟨t, t², t³⟩",
            "TrendingUp"
        ),
        new(
            "curve.conical",
            "Conical Expanding Spiral",
            "Space Curves 3D",
            "Spatial helix winding on a right circular cone with linearly expanding radius r(t) = ⟨t cos t, t sin t, t⟩.",
            "⟨t cos t, t sin t, t⟩",
            "Cone"
        ),

        // 12. Riemann Zeta Function & Prime Distribution
        new(
            "zeta.critical",
            "Riemann-Siegel Z(t) Critical Line",
            "Zeta & Primes",
            "Real-valued oscillation Z(t) = e^(i θ(t)) ζ(1/2 + it) on the critical strip whose roots are the non-trivial zeros.",
            "Z(t) on Re(s)=1/2",
            "SineWave"
        ),
        new(
            "zeta.zero1",
            "First Riemann Zero (γ₁ ≈ 14.13)",
            "Zeta & Primes",
            "The lowest non-trivial zero of the Riemann Zeta Function where the complex phasor passes through the origin.",
            "γ₁ = 14.134725",
            "Numeric1Circle"
        ),
        new(
            "zeta.zero2",
            "Second Riemann Zero (γ₂ ≈ 21.02)",
            "Zeta & Primes",
            "Second non-trivial zero of ζ(s) displaying consecutive GUE level repulsion.",
            "γ₂ = 21.022040",
            "Numeric2Circle"
        ),
        new(
            "zeta.staircase",
            "Prime Staircase π(x) vs R(x)",
            "Zeta & Primes",
            "Exact prime-counting step function compared with Gauss's smooth density R(x) = 1 + Σ (ln x)^n / (n! n ζ(n+1)).",
            "π(x) vs R(x)",
            "FormatListNumbered"
        ),
        new(
            "zeta.explicit",
            "Riemann's Explicit Superposition",
            "Zeta & Primes",
            "Reconstruction of prime steps by summing interference waves over 18 non-trivial Riemann zeros.",
            "π₀(x) ≈ R(x) - Σ 2Re(x^ρ / (ρ ln x))",
            "Waveform"
        ),

        // 13. Heat Diffusion & Laplace PDE
        new(
            "heat.laplace",
            "Laplace Steady-State Equilibrium",
            "Heat & Diffusion",
            "Dirichlet boundary equilibrium satisfying ∇²T = 0 with temperature gradient contours and flux vectors.",
            "∇²T = 0",
            "Fire"
        ),
        new(
            "heat.dipole",
            "Thermal Dipole (Source & Sink)",
            "Heat & Diffusion",
            "Coupled hot flame source (+100°C) and cold ice sink (-50°C) demonstrating Fourier heat flux stream lines.",
            "q = -k ∇T",
            "MagnifyScan"
        ),
        new(
            "heat.insulated",
            "Adiabatic Cavity with Central Heater",
            "Heat & Diffusion",
            "Insulated boundaries (∂T/∂n = 0) with a localized heat source conserving total thermal energy.",
            "∂T/∂n = 0",
            "ShieldOutline"
        ),
        new(
            "heat.corners",
            "Four-Corner Heat Exchanger",
            "Heat & Diffusion",
            "Chamber with opposing hot and cold corner reservoirs exhibiting quadrupolar thermal isotherms.",
            "T(x,y,t)",
            "Grid"
        ),

        // 14. Quantum Wave Mechanics & Schrödinger Equation
        new(
            "quantum.tunneling",
            "Quantum Barrier Tunneling",
            "Quantum & Waves",
            "Incident Gaussian wavepacket tunneling through a classically forbidden potential barrier (V₀ > E) with evanescent decay.",
            "T = e^(-2κL)",
            "Atom"
        ),
        new(
            "quantum.harmonic",
            "Harmonic Oscillator Coherent State",
            "Quantum & Waves",
            "Parabolic potential well V(x) = ½kx² confining a coherent state oscillating back and forth at frequency ω.",
            "V(x) = ½kx²",
            "SineWave"
        ),
        new(
            "quantum.doublewell",
            "Double-Well Quantum Tunneling Beats",
            "Quantum & Waves",
            "Symmetric quartic double-well V(x) = a(x² - b)² with macroscopic quantum tunneling and Rabi oscillations between wells.",
            "V(x) = a(x²-9)²",
            "ChartBellCurveCumulative"
        ),
        new(
            "quantum.step",
            "Quantum Step Potential Reflection",
            "Quantum & Waves",
            "Potential step V(x) = V₀ Θ(x) exhibiting wave interference fringes and partial quantum transmission/reflection.",
            "V(x) = V₀ Θ(x)",
            "RayEnd"
        ),
        new(
            "quantum.free",
            "Free Wavepacket Quantum Dispersion",
            "Quantum & Waves",
            "Free Gaussian wavepacket spreading in space over time due to Heisenberg's uncertainty principle σ(t) = σ₀√(1 + (ħt/2mσ₀²)²).",
            "Δx · Δp ≥ ħ/2",
            "WeatherWindy"
        ),

        // 15. Special Relativity & Minkowski Spacetime
        new(
            "relativity.boost",
            "Standard Lorentz Boost (β = 0.6c)",
            "Relativity & Spacetime",
            "Oblique coordinate transformation of Minkowski spacetime showing skewed simultaneity lines and invariant light cone.",
            "ct' = γ(ct - βx)",
            "Flash"
        ),
        new(
            "relativity.twin",
            "The Twin Paradox & Turnaround Gap",
            "Relativity & Spacetime",
            "Traveling twin accelerating and turning around at 0.8c accumulates less proper time (τ = 6.0) than Earth twin (t = 10.0).",
            "Δτ = ∫√(1 - v²/c²) dt",
            "AccountMultiple"
        ),
        new(
            "relativity.muon",
            "Relativistic Muon Atmospheric Penetration",
            "Relativity & Spacetime",
            "Cosmic muons moving at 0.98c survive to sea level due to time dilation (or atmosphere length contraction in muon frame).",
            "t = γ τ₀",
            "ArrowDownBoldCircleOutline"
        ),
        new(
            "relativity.bell",
            "Bell's Spaceship Paradox",
            "Relativity & Spacetime",
            "Two identically accelerating rockets maintain fixed coordinate distance in launch frame, but the connecting thread snaps!",
            "L' = γ L",
            "RocketLaunch"
        ),
        new(
            "relativity.doppler",
            "Relativistic Doppler Effect",
            "Relativity & Spacetime",
            "Periodic light pulses shifted toward red (receding source) or blue (approaching source): f' = f √((1-β)/(1+β)).",
            "f' = f √((1-β)/(1+β))",
            "RadioTower"
        ),

        // 16. Fluid Dynamics & Navier-Stokes Vorticity
        new(
            "fluid.karman",
            "Von Kármán Vortex Street Shedding",
            "Fluid Dynamics",
            "Submerged cylinder obstacle shedding an alternating periodic street of clockwise and counter-clockwise swirling vortices.",
            "∂ω/∂t + (u·∇)ω = ν∇²ω",
            "WeatherWindy"
        ),
        new(
            "fluid.dipole",
            "Vortex Dipole Collision",
            "Fluid Dynamics",
            "Counter-rotating vortex pair propagating forward by mutual induction and colliding against boundary walls.",
            "ω(x,y) = exp(-d₁²/2r₀²) - exp(-d₂²/2r₀²)",
            "DotsHorizontal"
        ),
        new(
            "fluid.kelvin",
            "Kelvin-Helmholtz Shear Instability",
            "Fluid Dynamics",
            "Opposing parallel shear streams rolling up into periodic spiral billows and cat's-eye vortex whorls.",
            "u(y) = tanh(y/δ)",
            "SineWave"
        ),
        new(
            "fluid.taylor",
            "Taylor-Green Vortex Dissipation",
            "Fluid Dynamics",
            "2x2 array of swirling vortices decaying exponentially over time due to viscous dissipation e^(-2νk²t).",
            "ψ = sin(kx)sin(ky)",
            "Grid"
        ),
        new(
            "fluid.cavity",
            "Lid-Driven Cavity Recirculation",
            "Fluid Dynamics",
            "Square fluid chamber with top sliding lid inducing primary central circulation and corner secondary eddies.",
            "∇²ψ = -ω",
            "SquareOutline"
        ),

        // 17. Topology & 4D Hypersurface Manifolds
        new(
            "topology.tesseract",
            "Tesseract (8-Cell / 4D Hypercube)",
            "Topology & 4D Manifolds",
            "The 4D analogue of the cube. 16 vertices, 32 edges, 24 square faces, and 8 bounding cubic cells undergoing SO(4) hyper-rotation.",
            "χ = V - E + F - C = 0",
            "CubeOutline"
        ),
        new(
            "topology.clifford",
            "Clifford Flat Torus in S³",
            "Topology & 4D Manifolds",
            "A Riemannian flat torus embedded without distortion in the 3-sphere: x₁² + x₂² = 1/2, x₃² + x₄² = 1/2.",
            "S¹(1/√2) × S¹(1/√2) ⊂ S³",
            "Donut"
        ),
        new(
            "topology.klein",
            "4D Non-Self-Intersecting Klein Bottle",
            "Topology & 4D Manifolds",
            "The true embedding of the non-orientable Klein bottle in ℝ⁴ without the self-intersection figure-8 pinch required in 3D.",
            "χ = 2 - 2g = 0 (Non-Orientable)",
            "FlaskEmpty"
        ),
        new(
            "topology.hopf",
            "Hopf Fibration (S³ → S²)",
            "Topology & 4D Manifolds",
            "The quintessential fiber bundle map π: S³ → S² whose fibers are pairwise linked great circles in the 3-sphere.",
            "S¹ ↪ S³ → S²",
            "ChartTimelineVariant"
        ),
        new(
            "topology.cell24",
            "24-Cell Regular 4-Polytope",
            "Topology & 4D Manifolds",
            "A unique regular 4-polytope with no 3D analogue. 24 octahedral cells, 96 triangular faces, 96 edges, and 24 vertices.",
            "F = (24, 96, 96, 24)",
            "ShapePolygonPlus"
        ),

        // 18. Celestial Mechanics & Gravitational N-Body Chaos
        new(
            "celestial.figure8",
            "Figure-8 Three-Body Choreography",
            "Celestial & N-Body",
            "Chenciner-Montgomery exact periodic solution where 3 equal masses chase each other indefinitely along a planar figure-8 curve.",
            "a_i = Σ G m_j (r_j - r_i) / |r_j - r_i|³",
            "Infinity"
        ),
        new(
            "celestial.lagrange",
            "Lagrange Points & Trojan Asteroids",
            "Celestial & N-Body",
            "Circular restricted 3-body system with stable triangular Lagrange points L4 and L5 hosting swarms of oscillating Trojan asteroids.",
            "∇Φ_eff = 0 at L₁...L₅",
            "Orbit"
        ),
        new(
            "celestial.pythagorean",
            "Pythagorean 3-Body Problem (Burrau's Chaos)",
            "Celestial & N-Body",
            "Three masses (3, 4, 5) released from rest at vertices of a 3-4-5 right triangle undergoing violent chaotic close encounters and ejection.",
            "m₁=3, m₂=4, m₃=5",
            "AtomVariant"
        ),
        new(
            "celestial.sunearthmoon",
            "Hierarchical Sun-Earth-Moon System",
            "Celestial & N-Body",
            "Stable hierarchical gravitational resonance exhibiting lunar orbital nodal precession and Kozai-Lidov tidal stability.",
            "M_☉ >> M_⊕ >> M_☾",
            "Earth"
        ),
        new(
            "celestial.galaxy",
            "Galactic Collision & Tidal Bridges",
            "Celestial & N-Body",
            "Two colliding disk galaxies with 90 orbiting stars ripped into sweeping tidal tails and bridges by gravitational tidal forces.",
            "F_tide ∝ 2 G M r / R³",
            "WeatherSunny"
        ),

        // 19. Non-Euclidean Hyperbolic & Elliptic Geometry
        new(
            "hyperbolic.heptagonal",
            "Heptagonal Tiling {7, 3} (Circle Limit)",
            "Hyperbolic Geometry",
            "Regular non-Euclidean tessellation where 3 regular heptagons meet at each vertex. The basis for M.C. Escher's iconic Circle Limit prints.",
            "ds² = 4 (dx² + dy²) / (1 - r²)²",
            "ShapePolygonPlus"
        ),
        new(
            "hyperbolic.octagonal",
            "Octagonal Tiling {8, 3}",
            "Hyperbolic Geometry",
            "Hyperbolic regular tessellation with 3 regular octagons meeting at each vertex in the Poincaré disk.",
            "cosh(d) = 1 + 2|z₁-z₂|² / ((1-|z₁|²)(1-|z₂|²))",
            "OctagonOutline"
        ),
        new(
            "hyperbolic.square5",
            "Order-5 Square Tiling {4, 5}",
            "Hyperbolic Geometry",
            "Regular tessellation where 5 right angles (90°) meet at every vertex, impossible in Euclidean space but natural in negative curvature K = -1.",
            "(p-2)(q-2) = (4-2)(5-2) = 6 > 4",
            "SquareOutline"
        ),
        new(
            "hyperbolic.ideal",
            "Ideal Hyperbolic Triangle",
            "Hyperbolic Geometry",
            "A triangle whose 3 vertices lie on the boundary horizon circle at infinity. All interior angles are 0°, producing maximal hyperbolic area π.",
            "Area(Δ_ideal) = π - (0 + 0 + 0) = π",
            "TriangleOutline"
        ),
        new(
            "hyperbolic.holonomy",
            "Gauss-Bonnet Angle Defect & Holonomy",
            "Hyperbolic Geometry",
            "Interactive triangle demonstrating that the sum of angles α + β + γ < 180°. The angular defect is exactly equal to the hyperbolic surface area.",
            "Area(Δ) = π - (α + β + γ)",
            "AngleAcute"
        ),

        // 20. Information Theory, Entropy & Signal Encoding
        new(
            "info.huffman",
            "Shannon Entropy & Huffman Tree",
            "Information & Entropy",
            "Claude Shannon's fundamental source coding limit H(X) = -Σ pᵢ log₂(pᵢ) with an optimal prefix-free binary Huffman tree.",
            "H(X) = -Σ pᵢ log₂(pᵢ)",
            "Binary"
        ),
        new(
            "info.qam16",
            "16-QAM Constellation & AWGN Channel",
            "Information & Entropy",
            "16 quadrature amplitude states in IQ phase space with Additive White Gaussian Noise clouds and Shannon-Hartley channel capacity.",
            "C = B · log₂(1 + S/N)",
            "Grid"
        ),
        new(
            "info.qpsk",
            "QPSK Modulation & BER Waterfall",
            "Information & Entropy",
            "4-quadrant phase-shift constellation in IQ space with decision boundaries and theoretical bit error rate waterfall curve.",
            "P_b = ½ erfc(√(E_b/N₀))",
            "CompassOutline"
        ),
        new(
            "info.bsc",
            "Binary Symmetric Channel & Capacity",
            "Information & Entropy",
            "Bit-flip channel with crossover noise p. Channel capacity C = 1 - H_b(p) drops monotonically from 1.0 to 0.0 bits/use.",
            "C = 1 - H_b(p)",
            "SwapHorizontal"
        ),
        new(
            "info.kldiv",
            "Kullback-Leibler Relative Entropy",
            "Information & Entropy",
            "Relative entropy D_KL(P || Q) measuring information inefficiency and demonstrating fundamental asymmetry D_KL(P || Q) ≠ D_KL(Q || P).",
            "D_KL(P ‖ Q) = Σ pᵢ log₂(pᵢ/qᵢ)",
            "ChartBar"
        ),

        // 21. Classical Mechanics, Lagrangian & Double Pendulum Chaos
        new(
            "lagrangian.double",
            "Double Pendulum Chaos",
            "Classical Mechanics",
            "Coupled nonlinear double pendulum exhibiting sensitive dependence on initial conditions (Butterfly Effect) and positive Lyapunov exponent.",
            "ℒ = T - V",
            "AxisArrow"
        ),
        new(
            "lagrangian.poincare",
            "Poincaré Section & KAM Torus",
            "Classical Mechanics",
            "Stroboscopic phase space cross-section demonstrating regular Kolmogorov-Arnold-Moser invariant tori and transition to chaos.",
            "H(θ, p) = E",
            "ChartScatterPlot"
        ),
        new(
            "lagrangian.kapitza",
            "Kapitza Inverted Pendulum",
            "Classical Mechanics",
            "High-frequency vertical base oscillation dynamically stabilizes the inverted upright pendulum position via effective potential V_eff.",
            "V_eff = mgL cos θ + ¼ m A² ω² sin² θ",
            "ArrowUpBoldCircleOutline"
        ),
        new(
            "lagrangian.spring",
            "Spring Pendulum (Fermi Resonance)",
            "Classical Mechanics",
            "Coupled 2D elastic pendulum showing non-linear energy transfer between radial stretch bounce and angular swing.",
            "r'' = r θ̇² - (k/m)(r - r₀) + g cos θ",
            "SineWave"
        ),
        new(
            "lagrangian.foucault",
            "Foucault Pendulum Precession",
            "Classical Mechanics",
            "Spherical pendulum in Earth's rotating frame exhibiting Coriolis force precession rate dψ/dt = -Ω sin φ.",
            "dψ/dt = -Ω_⊕ sin φ",
            "Compass"
        ),

        // 22. Modern Cryptography, Elliptic Curves & Finite Fields
        new(
            "crypto.secp256k1",
            "secp256k1 (Bitcoin Curve)",
            "Cryptography & Finite Fields",
            "Koblitz elliptic curve y² = x³ + 7 over real numbers and finite fields, powering Bitcoin and Ethereum public-key cryptography.",
            "y² = x³ + 7",
            "KeyOutline"
        ),
        new(
            "crypto.twocomponents",
            "Two-Component Curve (Δ > 0)",
            "Cryptography & Finite Fields",
            "Discriminant Δ = -16(4a³ + 27b²) > 0 creates two disconnected components: an isolated compact egg and an infinite open branch.",
            "y² = x³ - 3x + 1",
            "CircleDouble"
        ),
        new(
            "crypto.nodal",
            "Singular Nodal Curve (Δ = 0)",
            "Cryptography & Finite Fields",
            "Singular curve with vanishing discriminant (Δ = 0), producing an ordinary double point (self-intersection node) at (1, 0).",
            "y² = x³ - 3x + 2",
            "CloseCircleOutline"
        ),
        new(
            "crypto.finite",
            "Finite Field Torus Grid E(F_p)",
            "Cryptography & Finite Fields",
            "Discrete solutions to y² ≡ x³ + ax + b (mod p). Hasse's Theorem bounds group order |#E(F_p) - (p+1)| ≤ 2√p on the discrete torus.",
            "y² ≡ x³ - 3x + 7 (mod 89)",
            "Grid"
        ),
        new(
            "crypto.ecdh",
            "ECDH Key Exchange Protocol",
            "Cryptography & Finite Fields",
            "Alice and Bob establish an identical shared secret S = d_A(d_B G) = d_B(d_A G) over an insecure channel without revealing private keys.",
            "S = d_A(d_B G) = d_B(d_A G)",
            "ShieldCheck"
        ),
        new(
            "crypto.ecdlp",
            "ECDLP Scalar Hopping Trajectory",
            "Cryptography & Finite Fields",
            "Visualizes 1G → kG hopping trajectory across F_p, demonstrating why the discrete logarithm problem is a computationally one-way trapdoor function.",
            "Q = k · G (mod p)",
            "ChartScatterPlot"
        ),

        // 23. Discrete Dynamical Systems, Arnold's Cat Map & IFS
        new(
            "dynamics.arnold",
            "Arnold's Cat Map (Toral Chaos)",
            "Discrete Dynamics & Chaos",
            "Ergodic toral automorphism [[2, 1], [1, 1]] mod 1. Discrete 64x64 lattice undergoes hyperbolic stretching, phase mixing, and exact reconstitution at step 48.",
            "⟨x', y'⟩ = ⟨2x + y, x + y⟩ mod 1",
            "Cat"
        ),
        new(
            "dynamics.cobweb",
            "Logistic Map Cobweb Trajectory",
            "Discrete Dynamics & Chaos",
            "Iterated 1D quadratic map x_{n+1} = r x_n (1 - x_n). Cobweb diagrams illustrate convergence to fixed points, limit 2-cycles, and chaotic wandering.",
            "x_{n+1} = r x_n (1 - x_n)",
            "SpiderWeb"
        ),
        new(
            "dynamics.bifurcation",
            "Feigenbaum Period-Doubling Cascade",
            "Discrete Dynamics & Chaos",
            "Bifurcation tree showing period-doubling route to chaos governed by universal Feigenbaum constant δ ≈ 4.6692016 and α ≈ 2.5029078.",
            "lim (r_n - r_{n-1})/(r_{n+1} - r_n) = 4.6692",
            "ChartTimelineVariant"
        ),
        new(
            "dynamics.barnsley",
            "Barnsley Fern (Affine IFS)",
            "Discrete Dynamics & Chaos",
            "Self-similar botanical fractal generated via Iterated Function System with 4 contractive affine transformations (stem, leaflet, left, right).",
            "W(E) = ⋃ w_i(E)",
            "PineTree"
        ),
        new(
            "dynamics.sierpinski",
            "Sierpinski Gasket (Chaos Game)",
            "Discrete Dynamics & Chaos",
            "Equilateral triangular fractal with Hausdorff dimension D = log 3 / log 2 ≈ 1.585 generated via 3 midpoint contraction mappings.",
            "D = ln 3 / ln 2 ≈ 1.585",
            "TriangleOutline"
        ),

        // 24. Bifurcation & Catastrophe Theory (René Thom's Singularity Theory)
        new(
            "catastrophe.cusp",
            "Cusp Catastrophe & Hysteresis Jump (A₃)",
            "Bifurcation & Catastrophe Theory",
            "Codimension-2 singularity V(x) = x⁴/4 + ax²/2 + bx. Semicubical parabola 4a³ + 27b² = 0 separates monostable and bistable sheets, producing catastrophic jumps.",
            "V(x) = ¼x⁴ + ½ax² + bx",
            "ChartBellCurveCumulative"
        ),
        new(
            "catastrophe.fold",
            "Fold Catastrophe (Saddle-Node Annihilation, A₂)",
            "Bifurcation & Catastrophe Theory",
            "Codimension-1 singularity V(x) = x³/3 + ax. Two equilibria (stable node and unstable saddle) collide at a = 0 and annihilate.",
            "V(x) = ⅓x³ + ax",
            "ArrowCollapseVertical"
        ),
        new(
            "catastrophe.butterfly",
            "Butterfly Catastrophe Trimodality (A₅)",
            "Bifurcation & Catastrophe Theory",
            "Codimension-4 singularity V(x) = x⁶/6 + ax⁴/4 + bx³/3 + cx²/2 + dx with up to 3 coexisting stable potential wells (trimodal equilibrium).",
            "V(x) = ⅙x⁶ + ¼ax⁴ + ½cx²",
            "ButterFly"
        ),
        new(
            "catastrophe.zeeman",
            "Zeeman's Catastrophe Machine",
            "Bifurcation & Catastrophe Theory",
            "Mechanical wheel with opposing elastic springs whose smooth control handle motion induces sudden snapping angular jumps.",
            "V(θ) = ½(d₁ - r₀)² + ½(d₂ - r₀)²",
            "CogOutline"
        ),
        new(
            "catastrophe.swallowtail",
            "Swallowtail Caustic & Bifurcation (A₄)",
            "Bifurcation & Catastrophe Theory",
            "Codimension-3 singularity V(x) = x⁵/5 + ax³/3 + bx²/2 + cx creating the classical optical caustic swallowtail bifurcation geometry.",
            "V(x) = ⅕x⁵ + ⅓ax³ + ½bx² + cx",
            "RayStartVertex"
        ),

        // 25. Clifford Geometric Algebra & Multivector Calculus (G₃)
        new(
            "clifford.wedge",
            "Exterior Wedge Product & Oriented Plane (a ∧ b)",
            "Clifford Geometric Algebra",
            "Geometric product ab = a·b + a∧b decomposing into symmetric scalar inner product and antisymmetric bivector oriented area parallelogram.",
            "ab = a·b + a∧b",
            "VectorPolygon"
        ),
        new(
            "clifford.rotor",
            "Rotor Rotation & Sandwich Product (R v R†)",
            "Clifford Geometric Algebra",
            "Gimbal-lock-free rotation in an arbitrary oriented plane B via sandwich product v' = R v R† where R = cos(θ/2) - B sin(θ/2).",
            "v' = R v R† = e^(-B θ/2) v e^(B θ/2)",
            "AxisZRotateClockwise"
        ),
        new(
            "clifford.reflection",
            "Double Reflection Composition (Cartan-Dieudonné)",
            "Clifford Geometric Algebra",
            "Any spatial rotation is the composition of reflections across two planes: v'' = n₂ n₁ v n₁ n₂ = R v R† with rotor R = n₂ n₁.",
            "v'' = -n₂(-n₁ v n₁)n₂ = (n₂ n₁) v (n₁ n₂)",
            "Mirror"
        ),
        new(
            "clifford.maxwell",
            "Riemann-Silberstein Electromagnetic Bivector",
            "Clifford Geometric Algebra",
            "Unification of Electric and Magnetic fields into a single bivector F = E + IcB, condensing all 4 Maxwell equations into ∇F = J.",
            "F = E + I c B,  ∇F = J",
            "LightningBoltOutline"
        ),
        new(
            "clifford.spacetime",
            "Spacetime Boost Hyperbolic Rotor (STA G₁,₃)",
            "Clifford Geometric Algebra",
            "Lorentz boost as a hyperbolic rotor L = cosh(ζ/2) + e₀₁ sinh(ζ/2) where e₀₁² = +1 and rapidity ζ = artanh(v/c).",
            "L = cosh(ζ/2) + e₀₁ sinh(ζ/2)",
            "ClockFast"
        ),

        // 26. Differential Geometry, Geodesics & Curvature on Curved Surfaces (Gauss's Theorema Egregium)
        new(
            "diffgeom.torus",
            "Torus & Geodesic Winding Chaos",
            "Differential Geometry",
            "Gaussian curvature K = cos(v) / (r(R + r cos v)) transitioning from positive on outer equator to zero on crown to negative on inner equator, creating intricate geodesic trajectories.",
            "K = cos(v) / (r(R + r cos v))",
            "Torus"
        ),
        new(
            "diffgeom.sphere",
            "Sphere & Great Circle Geodesics",
            "Differential Geometry",
            "Constant positive Gaussian curvature K = 1/R² > 0. All geodesics are closed Great Circles with shortest path metric distances.",
            "K = 1/R² = const > 0",
            "Earth"
        ),
        new(
            "diffgeom.pseudosphere",
            "Beltrami Pseudosphere & Hyperbolic Geodesics",
            "Differential Geometry",
            "Surface of revolution of a tractrix with constant negative curvature K = -1, exhibiting exponential geodesic ray divergence.",
            "K = -1 = const < 0",
            "Cone"
        ),
        new(
            "diffgeom.enneper",
            "Enneper Minimal Surface (H = 0)",
            "Differential Geometry",
            "Self-intersecting minimal soap film surface satisfying Mean Curvature H = 0 and Gaussian curvature K = -4/(1 + u² + v²)⁴ ≤ 0 everywhere.",
            "H = 0,  K ≤ 0",
            "ShapePolygonPlus"
        ),
        new(
            "diffgeom.bonnet",
            "Catenoid-to-Helicoid Bonnet Isometric Family",
            "Differential Geometry",
            "Smooth isometric deformation morphing a catenoid into a helicoid preserving the metric tensor g_ij and Gaussian curvature K at every point with zero stretching.",
            "ds² = cosh²(v)(du² + dv²)",
            "AxisArrow"
        ),

        // 27. Knot Theory, Braids & 3D Topological Invariants (Vaughan Jones, Kurt Reidemeister, Emil Artin)
        new(
            "knot.trefoil",
            "Trefoil Knot 3₁ & Skein Invariant",
            "Knot Theory & Invariants",
            "The simplest non-trivial prime knot with 3 crossings and writhe w = -3. Characterized by Jones polynomial V(t) = -t⁻⁴ + t⁻³ + t⁻¹.",
            "V(t) = -t⁻⁴ + t⁻³ + t⁻¹",
            "VectorPolyline"
        ),
        new(
            "knot.figure8",
            "Figure-Eight Knot 4₁ (Amphichiral)",
            "Knot Theory & Invariants",
            "The unique prime knot with 4 crossings. Amphichiral (congruent to its mirror image), resulting in a palindromic Jones polynomial V(t) = V(t⁻¹) and zero writhe.",
            "V(t) = t⁻² - t⁻¹ + 1 - t + t²",
            "Infinity"
        ),
        new(
            "knot.cinquefoil",
            "Cinquefoil Knot 5₁ (Solomon's Seal)",
            "Knot Theory & Invariants",
            "The (5, 2) torus knot with 5 alternating crossings, high chiral asymmetry, and Seifert genus g = 2.",
            "V(t) = -t⁻⁷ + t⁻⁶ - t⁻⁵ + t⁻⁴ + t⁻²",
            "ShapePolygonPlus"
        ),
        new(
            "knot.borromean",
            "Borromean Rings (Brunnian Link)",
            "Knot Theory & Invariants",
            "Three closed loops with pairwise linking numbers Lk(K_i, K_j) = 0, yet inextricably topologically intertwined. Cutting any single ring frees the remaining two completely.",
            "Lk(i, j) = 0,  Brunnian Link",
            "CircleDouble"
        ),
        new(
            "knot.whitehead",
            "Whitehead Link (Zero Linking Number)",
            "Knot Theory & Invariants",
            "Two-component link with 5 crossings and vanishing Gauss linking number Lk = 0, demonstrating that linking number alone cannot detect link separation.",
            "c(L) = 5,  Lk(K₁, K₂) = 0",
            "LinkVariant"
        ),
        new(
            "knot.braid",
            "Artin Braid Group B₃ & Yang-Baxter",
            "Knot Theory & Invariants",
            "3-strand Artin braid satisfying the braid equation σ₁ σ₂ σ₁ = σ₂ σ₁ σ₂. Braid closure produces knots and links via Alexander's and Markov's theorems.",
            "σ₁ σ₂ σ₁ = σ₂ σ₁ σ₂",
            "ChartTimelineVariant"
        ),

        // 28. Symplectic Geometry, Hamiltonian Mechanics & Phase Space Invariants (William Rowan Hamilton, Joseph Liouville, Haruo Yoshida)
        new(
            "hamilton.henonheiles",
            "Hénon-Heiles Anharmonic Chaos & KAM Tori",
            "Symplectic & Hamiltonian",
            "Canonical 2D galactic potential transitioning from regular invariant KAM tori at low energy E < 0.11 to global ergodic chaos as E approaches escape energy Ec = 1/6.",
            "H = ½(p_x² + p_y²) + ½(x² + y²) + x²y - ⅓y³",
            "WeatherHurricane"
        ),
        new(
            "hamilton.duffing",
            "Duffing Double-Well Separatrix & Phase Flow",
            "Symplectic & Hamiltonian",
            "Bistable quartic potential with twin ground states at q = ±1 (V = -0.25) and a figure-8 separatrix at H = 0 dividing bounded libration from inter-well transit.",
            "H = ½p² - ½q² + ¼q⁴",
            "Infinity"
        ),
        new(
            "hamilton.pendulum",
            "Nonlinear Pendulum Phase Space Libration",
            "Symplectic & Hamiltonian",
            "Fundamental nonlinear Hamiltonian exhibiting closed elliptical librations inside the separatrix and continuous rotations outside with exact elliptic integral periods.",
            "H = ½p² - cos(q)",
            "Orbit"
        ),
        new(
            "hamilton.liouville",
            "Liouville Phase Space Volume Invariance (dq ∧ dp)",
            "Symplectic & Hamiltonian",
            "Demonstration of Liouville's theorem: Hamiltonian flow deforms a circular phase space droplet into filamentary tendrils while strictly preserving enclosed symplectic area.",
            "d/dt ∬_D dq dp = 0",
            "ShapePolygonPlus"
        ),
        new(
            "hamilton.yoshida",
            "Yoshida 4th-Order Symplectic vs Non-Symplectic RK4",
            "Symplectic & Hamiltonian",
            "Comparison of symplectic integration preserving the exact shadow Hamiltonian with zero long-term energy drift versus catastrophic RK4 orbit degradation.",
            "exp(dt(A + B)) = S₄(dt) + O(dt⁵)",
            "ChartLine"
        ),

        // 29. Algebraic Topology, Simplicial Homology & Persistent Betti Numbers (Henri Poincaré, Emmy Noether, Leopold Vietoris)
        new(
            "homology.torus",
            "2-Torus T² Simplicial Triangulation",
            "Algebraic Topology & Homology",
            "Canonical 9-vertex 18-triangle triangulation with glued opposite edges. Betti numbers β = (1, 2, 1), Euler characteristic χ = 0.",
            "H₁(T²; ℤ) ≅ ℤ ⊕ ℤ,  β = (1, 2, 1)",
            "Donut"
        ),
        new(
            "homology.sphere",
            "2-Sphere S² Octahedron Triangulation",
            "Algebraic Topology & Homology",
            "Canonical spherical triangulation with 6 vertices and 8 faces. Simply connected: β = (1, 0, 1), Euler characteristic χ = 2.",
            "H₁(S²; ℤ) = 0,  β = (1, 0, 1),  χ = 2",
            "Earth"
        ),
        new(
            "homology.klein",
            "Klein Bottle K² Non-Orientable Surface",
            "Algebraic Topology & Homology",
            "Triangulation with twisted edge identification. Non-orientable surface with torsion group H₁(K²; ℤ) ≅ ℤ ⊕ ℤ₂ and Betti numbers over ℚ: β = (1, 1, 0).",
            "H₁(K²; ℤ) ≅ ℤ ⊕ ℤ₂,  χ = 0",
            "BottleTonicOutline"
        ),
        new(
            "homology.projective",
            "Real Projective Plane ℝP² Minimal Triangulation",
            "Algebraic Topology & Homology",
            "Minimal 6-vertex 10-triangle triangulation. Non-orientable 2-manifold with Euler characteristic χ = 1 and 2-torsion H₁(ℝP²; ℤ) ≅ ℤ₂.",
            "H₁(ℝP²; ℤ) ≅ ℤ₂,  χ = 1",
            "ShapePolygonPlus"
        ),
        new(
            "homology.mobius",
            "Möbius Strip with Single Boundary ∂M ≅ S¹",
            "Algebraic Topology & Homology",
            "Canonical non-orientable 2D manifold with boundary. Retracts onto a central circle S¹, yielding β = (1, 1, 0) and Euler characteristic χ = 0.",
            "∂M ≅ S¹,  H₁(M; ℤ) ≅ ℤ,  χ = 0",
            "VectorPolyline"
        ),
        new(
            "homology.persistence",
            "Vietoris-Rips Point Cloud Filtration & Barcodes",
            "Algebraic Topology & Homology",
            "Topological data analysis tracking birth and death of Betti cycles [ε_birth, ε_death] across growing filtration radius ε.",
            "β_k(K_ε) = dim H_k(K_ε)",
            "ChartBar"
        ),

        // 30. Lie Algebras, Root Systems & Dynkin Diagrams (Sophus Lie, Wilhelm Killing, Élie Cartan)
        new(
            "lie.a2.su3",
            "Special Unitary Algebra su(3)",
            "Lie Algebras & Dynkin",
            "Hexagonal A₂ root lattice. Symmetries of quarks and gluons in Gell-Mann's Eightfold Way.",
            "A₂ ≅ 𝔰𝔲(3),  dim 8,  |Φ| = 6",
            "HexagonOutline"
        ),
        new(
            "lie.g2",
            "Exceptional Lie Algebra G₂",
            "Lie Algebras & Dynkin",
            "Smallest exceptional simple Lie algebra. Preserves the octonion algebra cross-product with 12 roots forming a dual star.",
            "G₂,  dim 14,  |Φ| = 12",
            "StarFourPoints"
        ),
        new(
            "lie.b2.so5",
            "Orthogonal Algebra so(5)",
            "Lie Algebras & Dynkin",
            "Octagonal B₂ root lattice with short roots (length 1) and long roots (length √2). Symplectic dual C₂ ≅ sp(4).",
            "B₂ ≅ 𝔰𝔬(5),  dim 10,  |Φ| = 8",
            "OctagonOutline"
        ),
        new(
            "lie.a3.su4",
            "Special Unitary Algebra su(4)",
            "Lie Algebras & Dynkin",
            "Rotatable 3D root polytope: cuboctahedron with 12 vertices and rank 3 Cartan matrix. Drag mouse to rotate.",
            "A₃ ≅ 𝔰𝔲(4),  dim 15,  |Φ| = 12",
            "CubeOutline"
        ),
        new(
            "lie.b3.so7",
            "Orthogonal Algebra so(7)",
            "Lie Algebras & Dynkin",
            "Rotatable 3D root polytope: rhombic dodecahedron with 18 vertices (6 short + 12 long roots). Drag mouse to rotate.",
            "B₃ ≅ 𝔰𝔬(7),  dim 21,  |Φ| = 18",
            "DecagramOutline"
        ),
        new(
            "lie.e8",
            "Exceptional Lie Algebra E₈",
            "Lie Algebras & Dynkin",
            "The crown jewel of modern mathematics: 240 roots in 8D projected onto the Coxeter plane, forming 8 concentric 30-gons.",
            "E₈,  dim 248,  |Φ| = 240,  h = 30",
            "Atom"
        ),

        // 31. Clay Millennium Prize Problems & Solutions
        new(
            "millennium.poincare",
            "Poincaré Conjecture (Solved by Perelman)",
            "Millennium Problems",
            "Hamilton's Ricci flow ∂_t g = -2 Ric(g) with Perelman W-entropy surgery and spherical extinction uniformizing simply connected closed 3-manifolds into S³.",
            "∂_t g = -2 Ric(g)  |  W(g, f, τ)",
            "GlobeModel",
            0.0, 0.35, 3.0
        ),
        new(
            "millennium.riemann",
            "Riemann Hypothesis & GUE Spectral Chaos",
            "Millennium Problems",
            "Critical line zeros of ζ(s) with Montgomery-Odlyzko GUE random matrix pair correlation R₂(s) and Hilbert-Pólya operator conjecture.",
            "ζ(1/2 + it) = 0  |  R₂(s) = 1 - (sin πs / πs)²",
            "Sigma",
            14.13, 16.0, 0.0
        ),
        new(
            "millennium.bsd",
            "Birch & Swinnerton-Dyer Conjecture",
            "Millennium Problems",
            "Equates the Mordell-Weil rank r of elliptic curve E/ℚ to the vanishing order ord_{s=1} L(E, s) and leading Taylor coefficient with regulator and Tate-Shafarevich group.",
            "ord_{s=1} L(E, s) = rank(E(ℚ))",
            "ChartLineStacked",
            -1.0, 1.0, 0.0
        ),
        new(
            "millennium.navierstokes",
            "Navier-Stokes Singularity Blow-Up (Solved by OpenAI)",
            "Millennium Problems",
            "3D incompressible Navier-Stokes finite-time blow-up proof formalized in Lean 4 by OpenAI, with self-similar collapsing profiles and Beale-Kato-Majda enstrophy divergence.",
            "∂_t u + (u · ∇)u = ν Δu - ∇p + f  |  Lean 4 Blow-Up Proved",
            "WeatherWindy",
            0.00001, 1.84, 2.5
        ),
        new(
            "millennium.yangmills",
            "Yang-Mills Existence & Mass Gap",
            "Millennium Problems",
            "Axiomatic 4D quantum gauge field theory, Wilson lattice plaquettes, string tension area law quark confinement, and glueball mass gap Δ > 0.",
            "S[A] = 1/(4g²) ∫ Tr(F²) d⁴x  |  Δ = E₁ - E₀ > 0",
            "AtomVariant",
            2.4, 16.0, 0.0
        ),
        new(
            "millennium.pvsnp",
            "P vs NP 3-SAT Phase Transition & Barriers",
            "Millennium Problems",
            "Cook-Levin NP-completeness, sharp 3-SAT satisfiability threshold at α_c ≈ 4.267, exponential complexity peak, and Relativization/Natural Proof barriers.",
            "P = NP ⟺ SAT ∈ P  |  α_c ≈ 4.267",
            "CodeBraces",
            4.267, 24.0, 0.0
        ),
        new(
            "millennium.hodge",
            "Hodge Conjecture & Algebraic Cycles",
            "Millennium Problems",
            "Cohomology decomposition H^k(X, ℂ) = ⨁ H^{p,q}, Hodge diamond symmetry, rational Hodge classes, and Lefschetz (1, 1)-theorem for algebraic subvarieties.",
            "H^{2p}(X, ℚ) ∩ H^{p,p}(X) = ⟨[Z]⟩_ℚ",
            "ShapePolygonPlus",
            1.0, 2.0, 0.0
        )
    };
}



