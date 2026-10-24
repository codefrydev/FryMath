using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Fluent;
using FryMath.UI.Views;
using FryMath.UI.ViewModels;

namespace UiSnapshots;

public class SnapshotApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new Material.Icons.Avalonia.MaterialIconStyles(null));
        Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://UiSnapshots"))
        {
            Source = new Uri("avares://FryMath.UI/Styles/MathStyles.axaml")
        });
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
    }
}

public static class Program
{
    private static string _activeFilter = string.Empty;

    public static void Main(string[] args)
    {
        AppBuilder.Configure<SnapshotApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false // Use Skia drawing backend
            })
            .UseSkia()
            .SetupWithoutStarting();

        string outDir = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../snapshots"));
        _activeFilter = args.Length > 1 ? args[1] : string.Empty;
        Directory.CreateDirectory(outDir);

        Console.WriteLine($"Capturing snapshots to: {outDir} (filter: '{_activeFilter}')");

        CaptureAll(outDir);

        Console.WriteLine("All snapshots captured successfully!");
    }

    private static void CaptureAll(string outDir, string filter = "")
    {
        CaptureLaboratory("Visual Calculus", Path.Combine(outDir, "lab_calculus.png"), vm =>
        {
            vm.ShowTaylorSeries = true;
            vm.TaylorDegree = 5;
        });
        CaptureLaboratory("Linear Algebra", Path.Combine(outDir, "lab_linear_algebra.png"));
        CaptureLaboratory("Fourier & Waves", Path.Combine(outDir, "lab_fourier.png"));
        CaptureLaboratory("Probability & Stats", Path.Combine(outDir, "lab_probability.png"));
        CaptureLaboratory("Dynamical Chaos", Path.Combine(outDir, "lab_chaos.png"));
        CaptureLaboratory("Vector Fields & ODEs", Path.Combine(outDir, "lab_vector_field.png"));
        CaptureLaboratory("Complex & Conformal", Path.Combine(outDir, "lab_complex.png"));
        CaptureLaboratory("Complex & Conformal", Path.Combine(outDir, "lab_complex_roots.png"), vm =>
        {
            vm.SetComplexPreset("roots");
        });
        CaptureLaboratory("Surfaces & 3D", Path.Combine(outDir, "lab_surface_3d.png"), vm =>
        {
            vm.SetSurfacePreset("paraboloid");
            vm.SurfaceProbeX = 1.0;
            vm.SurfaceProbeY = 1.0;
        });
        CaptureLaboratory("Surfaces & 3D", Path.Combine(outDir, "lab_surface_saddle.png"), vm =>
        {
            vm.SetSurfacePreset("saddle");
            vm.SurfaceProbeX = 0.8;
            vm.SurfaceProbeY = -0.5;
        });
        CaptureLaboratory("Fractals & Chaos", Path.Combine(outDir, "lab_fractal_mandelbrot.png"), vm =>
        {
            vm.SetFractalPreset("mandelbrot");
        });
        CaptureLaboratory("Fractals & Chaos", Path.Combine(outDir, "lab_fractal_julia.png"), vm =>
        {
            vm.SetFractalPreset("douady");
        });
        CaptureLaboratory("Fractals & Chaos", Path.Combine(outDir, "lab_fractal_newton.png"), vm =>
        {
            vm.SetFractalPreset("newton");
        });
        CaptureLaboratory("PDE & Waves", Path.Combine(outDir, "lab_wave_chladni.png"), vm =>
        {
            vm.SetWavePreset("rosette");
        });
        CaptureLaboratory("PDE & Waves", Path.Combine(outDir, "lab_wave_ripple.png"), vm =>
        {
            vm.SetWavePreset("ripple");
        });
        CaptureLaboratory("PDE & Waves", Path.Combine(outDir, "lab_wave_drum.png"), vm =>
        {
            vm.SetWavePreset("drum");
        });
        CaptureLaboratory("Space Curves 3D", Path.Combine(outDir, "lab_curve_helix.png"), vm =>
        {
            vm.SetSpaceCurvePreset("helix");
            vm.CurveParamT = 4.2;
        });
        CaptureLaboratory("Space Curves 3D", Path.Combine(outDir, "lab_curve_trefoil.png"), vm =>
        {
            vm.SetSpaceCurvePreset("trefoil");
            vm.CurveParamT = 2.1;
        });
        CaptureLaboratory("Space Curves 3D", Path.Combine(outDir, "lab_curve_viviani.png"), vm =>
        {
            vm.SetSpaceCurvePreset("viviani");
            vm.CurveParamT = 1.2;
        });
        CaptureLaboratory("Zeta & Primes", Path.Combine(outDir, "lab_zeta_critical.png"), vm =>
        {
            vm.SetZetaPreset("zero1");
        });
        CaptureLaboratory("Zeta & Primes", Path.Combine(outDir, "lab_zeta_zero2.png"), vm =>
        {
            vm.SetZetaPreset("zero2");
        });
        CaptureLaboratory("Zeta & Primes", Path.Combine(outDir, "lab_zeta_staircase.png"), vm =>
        {
            vm.SetZetaPreset("explicitprimes");
        });
        CaptureLaboratory("Heat & Diffusion", Path.Combine(outDir, "lab_heat_laplace.png"), vm =>
        {
            vm.SetHeatPreset("laplace");
        });
        CaptureLaboratory("Heat & Diffusion", Path.Combine(outDir, "lab_heat_dipole.png"), vm =>
        {
            vm.SetHeatPreset("dipole");
        });
        CaptureLaboratory("Heat & Diffusion", Path.Combine(outDir, "lab_heat_insulated.png"), vm =>
        {
            vm.SetHeatPreset("insulated");
        });
        CaptureLaboratory("Quantum & Waves", Path.Combine(outDir, "lab_quantum_tunneling.png"), vm =>
        {
            vm.SetQuantumPreset("tunneling");
        });
        CaptureLaboratory("Quantum & Waves", Path.Combine(outDir, "lab_quantum_harmonic.png"), vm =>
        {
            vm.SetQuantumPreset("harmonic");
        });
        CaptureLaboratory("Quantum & Waves", Path.Combine(outDir, "lab_quantum_doublewell.png"), vm =>
        {
            vm.SetQuantumPreset("doublewell");
        });
        CaptureLaboratory("Relativity & Spacetime", Path.Combine(outDir, "lab_relativity_boost.png"), vm =>
        {
            vm.SetRelativityPreset("boost");
        });
        CaptureLaboratory("Relativity & Spacetime", Path.Combine(outDir, "lab_relativity_twin.png"), vm =>
        {
            vm.SetRelativityPreset("twin");
        });
        CaptureLaboratory("Relativity & Spacetime", Path.Combine(outDir, "lab_relativity_muon.png"), vm =>
        {
            vm.SetRelativityPreset("muon");
        });
        CaptureLaboratory("Fluid Dynamics", Path.Combine(outDir, "lab_fluid_karman.png"), vm =>
        {
            vm.SetFluidPreset("karman");
        });
        CaptureLaboratory("Fluid Dynamics", Path.Combine(outDir, "lab_fluid_dipole.png"), vm =>
        {
            vm.SetFluidPreset("dipole");
        });
        CaptureLaboratory("Fluid Dynamics", Path.Combine(outDir, "lab_fluid_kelvin.png"), vm =>
        {
            vm.SetFluidPreset("kelvin");
        });
        CaptureLaboratory("Topology & 4D Manifolds", Path.Combine(outDir, "lab_topology_tesseract.png"), vm =>
        {
            vm.SetTopologyPreset("tesseract");
        });
        CaptureLaboratory("Topology & 4D Manifolds", Path.Combine(outDir, "lab_topology_clifford.png"), vm =>
        {
            vm.SetTopologyPreset("clifford");
        });
        CaptureLaboratory("Topology & 4D Manifolds", Path.Combine(outDir, "lab_topology_klein.png"), vm =>
        {
            vm.SetTopologyPreset("klein");
        });
        CaptureLaboratory("Celestial & N-Body", Path.Combine(outDir, "lab_celestial_figure8.png"), vm =>
        {
            vm.SetCelestialPreset("figure8");
        });
        CaptureLaboratory("Celestial & N-Body", Path.Combine(outDir, "lab_celestial_lagrange.png"), vm =>
        {
            vm.SetCelestialPreset("lagrange");
        });
        CaptureLaboratory("Celestial & N-Body", Path.Combine(outDir, "lab_celestial_galaxy.png"), vm =>
        {
            vm.SetCelestialPreset("galaxy");
        });
        CaptureLaboratory("Hyperbolic Geometry", Path.Combine(outDir, "lab_hyperbolic_heptagonal.png"), vm =>
        {
            vm.SetHyperbolicPreset("heptagonal");
        });
        CaptureLaboratory("Hyperbolic Geometry", Path.Combine(outDir, "lab_hyperbolic_ideal.png"), vm =>
        {
            vm.SetHyperbolicPreset("ideal");
        });
        CaptureLaboratory("Hyperbolic Geometry", Path.Combine(outDir, "lab_hyperbolic_klein.png"), vm =>
        {
            vm.SetHyperbolicPreset("octagonal");
            vm.SetHyperbolicModel("klein");
        });
        CaptureLaboratory("Information & Entropy", Path.Combine(outDir, "lab_info_huffman.png"), vm =>
        {
            vm.SetInformationPreset("huffman");
        });
        CaptureLaboratory("Information & Entropy", Path.Combine(outDir, "lab_info_qam16.png"), vm =>
        {
            vm.SetInformationPreset("qam16");
        });
        CaptureLaboratory("Information & Entropy", Path.Combine(outDir, "lab_info_bsc.png"), vm =>
        {
            vm.SetInformationPreset("bsc");
        });
        CaptureLaboratory("Classical Mechanics", Path.Combine(outDir, "lab_lagrangian_double.png"), vm =>
        {
            vm.SetLagrangianPreset("double");
        });
        CaptureLaboratory("Classical Mechanics", Path.Combine(outDir, "lab_lagrangian_poincare.png"), vm =>
        {
            vm.SetLagrangianPreset("poincare");
        });
        CaptureLaboratory("Classical Mechanics", Path.Combine(outDir, "lab_lagrangian_kapitza.png"), vm =>
        {
            vm.SetLagrangianPreset("kapitza");
        });
        CaptureLaboratory("Cryptography & Finite Fields", Path.Combine(outDir, "lab_crypto_secp256k1.png"), vm =>
        {
            vm.SetCryptoPreset("secp256k1");
        });
        CaptureLaboratory("Cryptography & Finite Fields", Path.Combine(outDir, "lab_crypto_twocomponents.png"), vm =>
        {
            vm.SetCryptoPreset("twocomponents");
        });
        CaptureLaboratory("Cryptography & Finite Fields", Path.Combine(outDir, "lab_crypto_ecdh.png"), vm =>
        {
            vm.SetCryptoPreset("ecdh");
        });
        CaptureLaboratory("Cryptography & Finite Fields", Path.Combine(outDir, "lab_crypto_ecdlp.png"), vm =>
        {
            vm.SetCryptoPreset("ecdlp");
        });
        CaptureLaboratory("Discrete Dynamics & Chaos", Path.Combine(outDir, "lab_dynamics_arnold.png"), vm =>
        {
            vm.SetDiscreteDynamicsPreset("arnold");
            vm.DiscreteDynamicsCatStep = 12;
        });
        CaptureLaboratory("Discrete Dynamics & Chaos", Path.Combine(outDir, "lab_dynamics_cobweb.png"), vm =>
        {
            vm.SetDiscreteDynamicsPreset("cobweb");
        });
        CaptureLaboratory("Discrete Dynamics & Chaos", Path.Combine(outDir, "lab_dynamics_bifurcation.png"), vm =>
        {
            vm.SetDiscreteDynamicsPreset("bifurcation");
        });
        CaptureLaboratory("Discrete Dynamics & Chaos", Path.Combine(outDir, "lab_dynamics_barnsley.png"), vm =>
        {
            vm.SetDiscreteDynamicsPreset("barnsley");
        });
        CaptureLaboratory("Bifurcation & Catastrophe Theory", Path.Combine(outDir, "lab_catastrophe_cusp.png"), vm =>
        {
            vm.SetCatastrophePreset("cusp");
            vm.CatastropheParamA = -2.5;
            vm.CatastropheParamB = 0.5;
            vm.CatastropheStateX = 1.45;
        });
        CaptureLaboratory("Bifurcation & Catastrophe Theory", Path.Combine(outDir, "lab_catastrophe_monostable.png"), vm =>
        {
            vm.SetCatastrophePreset("cusp");
            vm.CatastropheParamA = 0.8;
            vm.CatastropheParamB = 1.2;
            vm.CatastropheStateX = -0.85;
        });
        CaptureLaboratory("Bifurcation & Catastrophe Theory", Path.Combine(outDir, "lab_catastrophe_fold.png"), vm =>
        {
            vm.SetCatastrophePreset("fold");
        });
        CaptureLaboratory("Bifurcation & Catastrophe Theory", Path.Combine(outDir, "lab_catastrophe_butterfly.png"), vm =>
        {
            vm.SetCatastrophePreset("butterfly");
        });

        // 25. Clifford Geometric Algebra & Multivector Calculus
        CaptureLaboratory("Clifford Geometric Algebra", Path.Combine(outDir, "lab_clifford_wedge.png"), vm =>
        {
            vm.SetCliffordPreset("wedge");
        });
        CaptureLaboratory("Clifford Geometric Algebra", Path.Combine(outDir, "lab_clifford_rotor.png"), vm =>
        {
            vm.SetCliffordPreset("rotor");
            vm.CliffordRotorAngle = 1.25; // ~72 deg
        });
        CaptureLaboratory("Clifford Geometric Algebra", Path.Combine(outDir, "lab_clifford_reflection.png"), vm =>
        {
            vm.SetCliffordPreset("reflection");
        });
        CaptureLaboratory("Clifford Geometric Algebra", Path.Combine(outDir, "lab_clifford_maxwell.png"), vm =>
        {
            vm.SetCliffordPreset("maxwell");
        });
        CaptureLaboratory("Clifford Geometric Algebra", Path.Combine(outDir, "lab_clifford_spacetime.png"), vm =>
        {
            vm.SetCliffordPreset("spacetime");
        });

        // 26. Differential Geometry, Geodesics & Curvature on Curved Surfaces
        CaptureLaboratory("Differential Geometry", Path.Combine(outDir, "lab_diffgeom_torus.png"), vm =>
        {
            vm.SetDifferentialGeometryPreset("torus");
        });
        CaptureLaboratory("Differential Geometry", Path.Combine(outDir, "lab_diffgeom_sphere.png"), vm =>
        {
            vm.SetDifferentialGeometryPreset("sphere");
        });
        CaptureLaboratory("Differential Geometry", Path.Combine(outDir, "lab_diffgeom_pseudosphere.png"), vm =>
        {
            vm.SetDifferentialGeometryPreset("pseudosphere");
        });
        CaptureLaboratory("Differential Geometry", Path.Combine(outDir, "lab_diffgeom_enneper.png"), vm =>
        {
            vm.SetDifferentialGeometryPreset("enneper");
        });
        CaptureLaboratory("Differential Geometry", Path.Combine(outDir, "lab_diffgeom_bonnet.png"), vm =>
        {
            vm.SetDifferentialGeometryPreset("bonnet");
        });

        // 27. Knot Theory, Braids & 3D Topological Invariants
        CaptureLaboratory("Knot Theory & Invariants", Path.Combine(outDir, "lab_knot_trefoil.png"), vm =>
        {
            vm.SetKnotPreset("trefoil");
        });
        CaptureLaboratory("Knot Theory & Invariants", Path.Combine(outDir, "lab_knot_figure8.png"), vm =>
        {
            vm.SetKnotPreset("figure8");
        });
        CaptureLaboratory("Knot Theory & Invariants", Path.Combine(outDir, "lab_knot_cinquefoil.png"), vm =>
        {
            vm.SetKnotPreset("cinquefoil");
        });
        CaptureLaboratory("Knot Theory & Invariants", Path.Combine(outDir, "lab_knot_borromean.png"), vm =>
        {
            vm.SetKnotPreset("borromean");
        });
        CaptureLaboratory("Knot Theory & Invariants", Path.Combine(outDir, "lab_knot_whitehead.png"), vm =>
        {
            vm.SetKnotPreset("whitehead");
        });
        CaptureLaboratory("Knot Theory & Invariants", Path.Combine(outDir, "lab_knot_braid.png"), vm =>
        {
            vm.SetKnotPreset("braid");
        });

        // 28. Symplectic Geometry, Hamiltonian Mechanics & Phase Space Invariants
        CaptureLaboratory("Symplectic & Hamiltonian", Path.Combine(outDir, "lab_hamilton_henonheiles.png"), vm =>
        {
            vm.SetHamiltonianPreset("henonheiles");
        });
        CaptureLaboratory("Symplectic & Hamiltonian", Path.Combine(outDir, "lab_hamilton_duffing.png"), vm =>
        {
            vm.SetHamiltonianPreset("duffing");
        });
        CaptureLaboratory("Symplectic & Hamiltonian", Path.Combine(outDir, "lab_hamilton_pendulum.png"), vm =>
        {
            vm.SetHamiltonianPreset("pendulum");
        });
        CaptureLaboratory("Symplectic & Hamiltonian", Path.Combine(outDir, "lab_hamilton_liouville.png"), vm =>
        {
            vm.SetHamiltonianPreset("liouville");
        });
        CaptureLaboratory("Symplectic & Hamiltonian", Path.Combine(outDir, "lab_hamilton_yoshida.png"), vm =>
        {
            vm.SetHamiltonianPreset("yoshida");
        });

        // 29. Algebraic Topology, Simplicial Homology & Persistent Betti Numbers
        CaptureLaboratory("Algebraic Topology & Homology", Path.Combine(outDir, "lab_homology_torus.png"), vm =>
        {
            vm.SetHomologyPreset("torus");
        });
        CaptureLaboratory("Algebraic Topology & Homology", Path.Combine(outDir, "lab_homology_sphere.png"), vm =>
        {
            vm.SetHomologyPreset("sphere");
        });
        CaptureLaboratory("Algebraic Topology & Homology", Path.Combine(outDir, "lab_homology_klein.png"), vm =>
        {
            vm.SetHomologyPreset("klein");
        });
        CaptureLaboratory("Algebraic Topology & Homology", Path.Combine(outDir, "lab_homology_projective.png"), vm =>
        {
            vm.SetHomologyPreset("projective");
        });
        CaptureLaboratory("Algebraic Topology & Homology", Path.Combine(outDir, "lab_homology_mobius.png"), vm =>
        {
            vm.SetHomologyPreset("mobius");
        });
        CaptureLaboratory("Algebraic Topology & Homology", Path.Combine(outDir, "lab_homology_persistence.png"), vm =>
        {
            vm.SetHomologyPreset("persistence");
        });

        // 30. Lie Algebras, Root Systems & Dynkin Diagrams
        CaptureLaboratory("Lie Algebras & Dynkin", Path.Combine(outDir, "lab_lie_a2.png"), vm =>
        {
            vm.SetLieType("a2");
        });
        CaptureLaboratory("Lie Algebras & Dynkin", Path.Combine(outDir, "lab_lie_g2.png"), vm =>
        {
            vm.SetLieType("g2");
        });
        CaptureLaboratory("Lie Algebras & Dynkin", Path.Combine(outDir, "lab_lie_e8.png"), vm =>
        {
            vm.SetLieType("e8");
        });
        CaptureLaboratory("Lie Algebras & Dynkin", Path.Combine(outDir, "lab_lie_a3.png"), vm =>
        {
            vm.SetLieType("a3");
            vm.LieCameraYaw = 0.8;
            vm.LieCameraPitch = 0.5;
        });
        CaptureLaboratory("Lie Algebras & Dynkin", Path.Combine(outDir, "lab_lie_b3.png"), vm =>
        {
            vm.SetLieType("b3");
            vm.LieCameraYaw = -0.6;
            vm.LieCameraPitch = 0.4;
        });
        CaptureLaboratory("Lie Algebras & Dynkin", Path.Combine(outDir, "lab_lie_b2.png"), vm =>
        {
            vm.SetLieType("b2");
        });

        // Code-Free Inspector Builders Snapshots
        CaptureLaboratory("Complex & Conformal", Path.Combine(outDir, "lab_complex_mobius.png"), vm =>
        {
            vm.SetMobiusPreset("inversion");
        });
        CaptureLaboratory("Surfaces & 3D", Path.Combine(outDir, "lab_surface_quadric.png"), vm =>
        {
            vm.SurfaceParamA = 1.0;
            vm.SurfaceParamB = -1.0;
            vm.SurfaceParamC = 0.0;
            vm.SurfaceProbeX = 0.5;
            vm.SurfaceProbeY = 0.5;
        });
        CaptureLaboratory("Fractals & Chaos", Path.Combine(outDir, "lab_fractal_rabbit.png"), vm =>
        {
            vm.SetFamousJuliaPreset("rabbit");
            vm.FractalEscapeRadius = 5.0;
        });
        CaptureLaboratory("Cryptography & Finite Fields", Path.Combine(outDir, "lab_crypto_discriminant.png"), vm =>
        {
            vm.SetCryptoCurveCoeffs("bitcoin");
            vm.SetCryptoPrimeP(23);
        });
        CaptureLaboratory("Bifurcation & Catastrophe Theory", Path.Combine(outDir, "lab_catastrophe_bistable.png"), vm =>
        {
            vm.SetCatastropheMaxwellPoint();
        });
        CaptureLaboratory("Symplectic & Hamiltonian", Path.Combine(outDir, "lab_hamilton_duffing_builder.png"), vm =>
        {
            vm.HamiltonDuffingAlpha = -1.0;
            vm.HamiltonDuffingBeta = 0.5;
            vm.SetHamiltonianEnergyLevel(0.08);
        });
        CaptureLaboratory("Fourier & Waves", Path.Combine(outDir, "lab_fourier_builder.png"), vm =>
        {
            vm.SetHarmonicFilter("odd");
            vm.Harmonics = 15;
        });
        CaptureLaboratory("Probability & Stats", Path.Combine(outDir, "lab_probability_builder.png"), vm =>
        {
            vm.SetDistributionPreset("skew_right");
        });
        CaptureLaboratory("PDE & Waves", Path.Combine(outDir, "lab_wave_cfl_builder.png"), vm =>
        {
            vm.WavePropagationSpeedC = 1.4;
            vm.SetWaveExcitation("dipole");
        });
        CaptureLaboratory("Relativity & Spacetime", Path.Combine(outDir, "lab_relativity_builder.png"), vm =>
        {
            vm.SetRelativityBetaPreset("0.866");
        });
        CaptureLaboratory("Classical Mechanics", Path.Combine(outDir, "lab_lagrangian_builder.png"), vm =>
        {
            vm.SetLagrangianRegime("chaotic");
        });
        CaptureLaboratory("Differential Geometry", Path.Combine(outDir, "lab_diffgeom_builder.png"), vm =>
        {
            vm.SetDiffGeomGeometryPreset("pseudosphere");
        });
        CaptureLaboratory("Dynamical Chaos", Path.Combine(outDir, "lab_chaos_builder.png"), vm =>
        {
            vm.SetChaosAttractorPreset("butterfly");
            vm.ShowChaosDualTrajectories = true;
        });
        CaptureLaboratory("Vector Fields & ODEs", Path.Combine(outDir, "lab_vectorfield_builder.png"), vm =>
        {
            vm.SetVectorFieldTopologyPreset("vortex");
            vm.SetVectorFieldProbePoint("one");
        });
        CaptureLaboratory("Space Curves 3D", Path.Combine(outDir, "lab_spacecurve_builder.png"), vm =>
        {
            vm.SetSpaceCurveGeometry("helix");
            vm.CurveParamT = 3.5;
            vm.CurveShowFrenetFrame = true;
            vm.CurveShowOsculatingPlane = true;
            vm.CurveShowOsculatingCircle = true;
        });
        CaptureLaboratory("Zeta & Primes", Path.Combine(outDir, "lab_zeta_builder.png"), vm =>
        {
            vm.SetZetaZeroTeleport(1);
            vm.ZetaZeroCount = 10;
        });
        CaptureLaboratory("Heat & Diffusion", Path.Combine(outDir, "lab_heat_builder.png"), vm =>
        {
            vm.SetHeatBoundaryCondition("dirichlet");
            vm.HeatDiffusivity = 0.22;
        });
        CaptureLaboratory("Fluid Dynamics", Path.Combine(outDir, "lab_fluid_builder.png"), vm =>
        {
            vm.SetFluidReynoldsPreset("karman");
        });

        // 31. Clay Millennium Prize Problems & Solutions
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_poincare.png"), vm =>
        {
            vm.SelectMillenniumProblem("poincare");
            vm.MillenniumRicciTime = 1.6;
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_riemann.png"), vm =>
        {
            vm.SelectMillenniumProblem("riemann");
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_riemann_hilbert.png"), vm =>
        {
            vm.SelectMillenniumProblem("riemann");
            vm.SetRiemannAttack("hilbert");
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_riemann_debruijn.png"), vm =>
        {
            vm.SelectMillenniumProblem("riemann");
            vm.SetRiemannAttack("debruijn");
            vm.SetDeBruijnT(0.0);
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_riemann_li.png"), vm =>
        {
            vm.SelectMillenniumProblem("riemann");
            vm.SetRiemannAttack("li");
            vm.RiemannLiMaxN = 25;
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_riemann_hunter.png"), vm =>
        {
            vm.SelectMillenniumProblem("riemann");
            vm.SetRiemannAttack("hunter");
            vm.RiemannHunterTMin = 10.0;
            vm.RiemannHunterTMax = 45.0;
            vm.HuntRiemannZeros();
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_bsd.png"), vm =>
        {
            vm.SelectMillenniumProblem("bsd");
            vm.SetBsdCurvePreset("rank1");
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_navierstokes.png"), vm =>
        {
            vm.SelectMillenniumProblem("navierstokes");
            vm.SetNavierRegime("critical");
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_navierstokes_openai.png"), vm =>
        {
            vm.SelectMillenniumProblem("navierstokes");
            vm.SetNavierRegime("openai");
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_yangmills.png"), vm =>
        {
            vm.SelectMillenniumProblem("yangmills");
            vm.SetYangMillsCoupling("scaling");
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_pvsnp.png"), vm =>
        {
            vm.SelectMillenniumProblem("pvsnp");
            vm.SetSatClauseRatio("critical");
        });
        CaptureLaboratory("Millennium Problems", Path.Combine(outDir, "lab31_millennium_hodge.png"), vm =>
        {
            vm.SelectMillenniumProblem("hodge");
            vm.SetHodgeVariety("k3");
        });
    }

    private static void CaptureLaboratory(string labName, string filePath, Action<MainWindowViewModel>? configure = null)
    {
        if (!string.IsNullOrEmpty(_activeFilter) &&
            !filePath.Contains(_activeFilter, StringComparison.OrdinalIgnoreCase) &&
            !labName.Contains(_activeFilter, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var studio = new MathStudioView();
        var vm = new MainWindowViewModel();
        vm.SelectLaboratory(labName);
        configure?.Invoke(vm);
        studio.DataContext = vm;

        var window = new Window
        {
            Width = 1380,
            Height = 850,
            DataContext = vm,
            Content = studio
        };

        Console.WriteLine($"Starting capture: {labName} -> {filePath}");
        window.Show();

        var frame = window.CaptureRenderedFrame();
#pragma warning disable CS0618
        if (frame != null)
        {
            frame.Save(filePath);
            Console.WriteLine($"Saved snapshot: {filePath}");
        }
        else
        {
            var pixelSize = new PixelSize(1380, 850);
            using var rtb = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
            rtb.Render(window);
            rtb.Save(filePath);
            Console.WriteLine($"Saved snapshot (RTB): {filePath}");
        }
#pragma warning restore CS0618
        window.Close();
        vm.Dispose();
    }
}
