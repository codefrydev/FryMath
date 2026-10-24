using System;
using System.Linq;
using System.Threading.Tasks;
using FryMath.Core.LinearAlgebra;
using FryMath.Core.Presets;
using FryMath.Plugin;
using FryMath.UI.Theming;
using FryMath.UI.ViewModels;
using Xunit;

namespace FryMath.UI.Tests;

public class UiAndViewModelTests
{
    [Fact]
    public void MainWindowViewModel_Initializes_WithCalculusDefaults()
    {
        using var vm = new MainWindowViewModel();

        Assert.Equal("Visual Calculus", vm.ActiveLaboratory);
        Assert.Equal("x^3 - 3*x", vm.Expression);
        Assert.NotEmpty(vm.AnalyticalDerivative);
        Assert.NotEmpty(vm.DefiniteIntegralResult);
        Assert.NotEmpty(vm.Presets);
    }

    [Fact]
    public void MainWindowViewModel_LoadPreset_SwitchesLaboratoryAndState()
    {
        using var vm = new MainWindowViewModel();

        var rotPreset = MathPresetCatalog.AllPresets.First(p => p.Id == "linalg.rotation");
        vm.LoadPreset(rotPreset);

        Assert.Equal("Linear Algebra", vm.ActiveLaboratory);
        Assert.Equal(1.0, vm.Determinant, 2);

        var fourierPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Fourier & Waves");
        vm.LoadPreset(fourierPreset);

        Assert.Equal("Fourier & Waves", vm.ActiveLaboratory);
        Assert.Equal(fourierPreset.ExpressionOrMatrix, vm.FourierPreset);
    }

    [Fact]
    public void MainWindowViewModel_OmnibarExecution_ComputesNewExpression()
    {
        using var vm = new MainWindowViewModel();

        vm.OmnibarQuery = "sin(x)";
        vm.ExecuteOmnibar();

        Assert.Equal("sin(x)", vm.Expression);
        Assert.Equal("Visual Calculus", vm.ActiveLaboratory);
        Assert.Contains("cos", vm.AnalyticalDerivative);
    }

    [Fact]
    public void MathThemeService_SwitchesDarkAndLightTokens()
    {
        var service = MathThemeService.Instance;

        service.SetTheme("Dark");
        Assert.True(service.Tokens.IsDark);

        service.SetTheme("Light Paper");
        Assert.False(service.Tokens.IsDark);

        // Reset to dark
        service.SetTheme("Dark Electric");
        Assert.True(service.Tokens.IsDark);
    }

    [Fact]
    public void MathPlugin_ManifestAndMetadata_AreValid()
    {
        var plugin = new MathPlugin();

        Assert.Equal("com.fryworld.math", plugin.Id);
        Assert.Equal("Math Studio", plugin.Name);
        Assert.Equal(new Version(1, 0, 0), plugin.Version);
        Assert.NotNull(plugin.SettingsSchema);
        Assert.True(plugin.SettingsSchema.ContainsKey("DefaultLaboratory"));
    }

    [Fact]
    public void MainWindowViewModel_Omnibar_RoutesToSurfacesFractalsAndWaves()
    {
        using var vm = new MainWindowViewModel();

        // 1. 3D Surface
        vm.OmnibarQuery = "z = x^2 - y^2";
        vm.ExecuteOmnibar();
        Assert.Equal("Surfaces & 3D", vm.ActiveLaboratory);
        Assert.Equal("x^2 - y^2", vm.SurfaceFormula);

        // 2. Fractals
        vm.OmnibarQuery = "mandelbrot";
        vm.ExecuteOmnibar();
        Assert.Equal("Fractals & Chaos", vm.ActiveLaboratory);
        Assert.Equal(FryMath.Core.Fractals.FractalType.Mandelbrot, vm.SelectedFractalType);

        // 3. PDE & Waves
        vm.OmnibarQuery = "chladni";
        vm.ExecuteOmnibar();
        Assert.Equal("PDE & Waves", vm.ActiveLaboratory);
        Assert.Equal(FryMath.Core.Waves.WaveSimulationMode.ChladniPlate, vm.SelectedWaveMode);

        // 4. Space Curves 3D
        vm.OmnibarQuery = "helix";
        vm.ExecuteOmnibar();
        Assert.Equal("Space Curves 3D", vm.ActiveLaboratory);
        Assert.Equal(FryMath.Core.Geometry.SpaceCurvePreset.CircularHelix, vm.SelectedSpaceCurvePreset);

        vm.OmnibarQuery = "trefoil";
        vm.ExecuteOmnibar();
        Assert.Equal("Space Curves 3D", vm.ActiveLaboratory);
        Assert.Equal(FryMath.Core.Geometry.SpaceCurvePreset.TrefoilKnot, vm.SelectedSpaceCurvePreset);

        // 5. Zeta & Primes
        vm.OmnibarQuery = "zeta";
        vm.ExecuteOmnibar();
        Assert.Equal("Zeta & Primes", vm.ActiveLaboratory);

        // 6. Heat & Diffusion
        vm.OmnibarQuery = "heat";
        vm.ExecuteOmnibar();
        Assert.Equal("Heat & Diffusion", vm.ActiveLaboratory);
        Assert.Equal("laplace", vm.HeatPreset);

        vm.OmnibarQuery = "thermal dipole";
        vm.ExecuteOmnibar();
        // 7. Quantum & Waves
        vm.OmnibarQuery = "quantum";
        vm.ExecuteOmnibar();
        Assert.Equal("Quantum & Waves", vm.ActiveLaboratory);
        Assert.Equal("tunneling", vm.QuantumPreset);

        vm.OmnibarQuery = "harmonic coherent";
        vm.OmnibarQuery = "harmonic coherent";
        vm.ExecuteOmnibar();
        Assert.Equal("Quantum & Waves", vm.ActiveLaboratory);
        Assert.Equal("harmonic", vm.QuantumPreset);

        // 8. Relativity & Spacetime
        vm.OmnibarQuery = "relativity boost";
        vm.ExecuteOmnibar();
        Assert.Equal("Relativity & Spacetime", vm.ActiveLaboratory);
        Assert.Equal("boost", vm.RelativityPreset);

        vm.OmnibarQuery = "twin paradox";
        vm.ExecuteOmnibar();
        Assert.Equal("Relativity & Spacetime", vm.ActiveLaboratory);
        Assert.Equal("twin", vm.RelativityPreset);

        // 9. Fluid Dynamics
        vm.OmnibarQuery = "navier stokes karman";
        vm.ExecuteOmnibar();
        Assert.Equal("Fluid Dynamics", vm.ActiveLaboratory);
        Assert.Equal("karman", vm.FluidPreset);

        vm.OmnibarQuery = "vortex dipole";
        vm.ExecuteOmnibar();
        Assert.Equal("Fluid Dynamics", vm.ActiveLaboratory);
        Assert.Equal("dipole", vm.FluidPreset);

        // 10. Topology & 4D Manifolds
        vm.OmnibarQuery = "tesseract";
        vm.ExecuteOmnibar();
        Assert.Equal("Topology & 4D Manifolds", vm.ActiveLaboratory);
        Assert.Equal("tesseract", vm.TopologyPreset);

        vm.OmnibarQuery = "clifford torus";
        vm.ExecuteOmnibar();
        Assert.Equal("Topology & 4D Manifolds", vm.ActiveLaboratory);
        Assert.Equal("clifford", vm.TopologyPreset);

        vm.OmnibarQuery = "4d klein bottle";
        vm.ExecuteOmnibar();
        Assert.Equal("Topology & 4D Manifolds", vm.ActiveLaboratory);
        Assert.Equal("klein", vm.TopologyPreset);

        // 11. Celestial & N-Body
        vm.OmnibarQuery = "figure8 choreography";
        vm.ExecuteOmnibar();
        Assert.Equal("Celestial & N-Body", vm.ActiveLaboratory);
        Assert.Equal("figure8", vm.CelestialPreset);

        vm.OmnibarQuery = "lagrange trojan asteroids";
        vm.ExecuteOmnibar();
        Assert.Equal("Celestial & N-Body", vm.ActiveLaboratory);
        Assert.Equal("lagrange", vm.CelestialPreset);

        vm.OmnibarQuery = "galactic collision tidal tails";
        vm.ExecuteOmnibar();
        Assert.Equal("Celestial & N-Body", vm.ActiveLaboratory);
        Assert.Equal("galaxy", vm.CelestialPreset);

        // 12. Hyperbolic Geometry
        vm.OmnibarQuery = "heptagonal tiling";
        vm.ExecuteOmnibar();
        Assert.Equal("Hyperbolic Geometry", vm.ActiveLaboratory);
        Assert.Equal("heptagonal", vm.HyperbolicPreset);

        vm.OmnibarQuery = "ideal triangle";
        vm.ExecuteOmnibar();
        Assert.Equal("Hyperbolic Geometry", vm.ActiveLaboratory);
        Assert.Equal("ideal", vm.HyperbolicPreset);

        vm.OmnibarQuery = "gauss bonnet defect";
        vm.ExecuteOmnibar();
        Assert.Equal("Hyperbolic Geometry", vm.ActiveLaboratory);
        Assert.Equal("holonomy", vm.HyperbolicPreset);

        // 13. Information Theory & Entropy
        vm.OmnibarQuery = "shannon entropy huffman";
        vm.ExecuteOmnibar();
        Assert.Equal("Information & Entropy", vm.ActiveLaboratory);
        Assert.Equal("huffman", vm.InformationPreset);

        vm.OmnibarQuery = "16-qam constellation capacity";
        vm.ExecuteOmnibar();
        Assert.Equal("Information & Entropy", vm.ActiveLaboratory);
        Assert.Equal("qam16", vm.InformationPreset);

        vm.OmnibarQuery = "qpsk modulation ber";
        vm.ExecuteOmnibar();
        Assert.Equal("Information & Entropy", vm.ActiveLaboratory);
        Assert.Equal("qpsk", vm.InformationPreset);

        vm.OmnibarQuery = "binary symmetric channel bsc";
        vm.ExecuteOmnibar();
        Assert.Equal("Information & Entropy", vm.ActiveLaboratory);
        Assert.Equal("bsc", vm.InformationPreset);

        vm.OmnibarQuery = "kullback leibler divergence kldiv";
        vm.ExecuteOmnibar();
        Assert.Equal("Information & Entropy", vm.ActiveLaboratory);
        Assert.Equal("kldiv", vm.InformationPreset);

        // 14. Classical Mechanics & Lagrangian Dynamics
        vm.OmnibarQuery = "double pendulum chaos";
        vm.ExecuteOmnibar();
        Assert.Equal("Classical Mechanics", vm.ActiveLaboratory);
        Assert.Equal("double", vm.LagrangianPreset);

        vm.OmnibarQuery = "poincare section kam torus";
        vm.ExecuteOmnibar();
        Assert.Equal("Classical Mechanics", vm.ActiveLaboratory);
        Assert.Equal("poincare", vm.LagrangianPreset);

        vm.OmnibarQuery = "kapitza inverted pendulum";
        vm.ExecuteOmnibar();
        Assert.Equal("Classical Mechanics", vm.ActiveLaboratory);
        Assert.Equal("kapitza", vm.LagrangianPreset);

        vm.OmnibarQuery = "spring pendulum fermi resonance";
        vm.ExecuteOmnibar();
        Assert.Equal("Classical Mechanics", vm.ActiveLaboratory);
        Assert.Equal("spring", vm.LagrangianPreset);

        vm.OmnibarQuery = "foucault pendulum precession";
        vm.ExecuteOmnibar();
        Assert.Equal("Classical Mechanics", vm.ActiveLaboratory);
        Assert.Equal("foucault", vm.LagrangianPreset);

        // 15. Modern Cryptography, Elliptic Curves & Finite Fields
        vm.OmnibarQuery = "secp256k1 bitcoin elliptic curve";
        vm.ExecuteOmnibar();
        Assert.Equal("Cryptography & Finite Fields", vm.ActiveLaboratory);
        Assert.Equal("secp256k1", vm.CryptoPreset);
        Assert.Equal("real", vm.CryptoDisplayMode);

        vm.OmnibarQuery = "ecdh key exchange";
        vm.ExecuteOmnibar();
        Assert.Equal("Cryptography & Finite Fields", vm.ActiveLaboratory);
        Assert.Equal("ecdh", vm.CryptoPreset);
        Assert.Equal("ecdh", vm.CryptoDisplayMode);

        vm.OmnibarQuery = "ecdlp scalar trapdoor hop";
        vm.ExecuteOmnibar();
        Assert.Equal("Cryptography & Finite Fields", vm.ActiveLaboratory);
        Assert.Equal("ecdlp", vm.CryptoPreset);
        Assert.Equal("ecdlp", vm.CryptoDisplayMode);

        vm.OmnibarQuery = "finite field hasse bound";
        vm.ExecuteOmnibar();
        Assert.Equal("Cryptography & Finite Fields", vm.ActiveLaboratory);
        Assert.Equal("finite", vm.CryptoPreset);
        Assert.Equal("finite", vm.CryptoDisplayMode);

        // 16. Discrete Dynamics, Arnold's Cat Map & IFS
        vm.OmnibarQuery = "arnold cat map";
        vm.ExecuteOmnibar();
        Assert.Equal("Discrete Dynamics & Chaos", vm.ActiveLaboratory);
        Assert.Equal("arnold", vm.DiscreteDynamicsPreset);
        Assert.Equal("arnold", vm.DiscreteDynamicsDisplayMode);

        vm.OmnibarQuery = "logistic cobweb";
        vm.ExecuteOmnibar();
        Assert.Equal("Discrete Dynamics & Chaos", vm.ActiveLaboratory);
        Assert.Equal("cobweb", vm.DiscreteDynamicsPreset);
        Assert.Equal("cobweb", vm.DiscreteDynamicsDisplayMode);

        vm.OmnibarQuery = "feigenbaum bifurcation cascade";
        vm.ExecuteOmnibar();
        Assert.Equal("Discrete Dynamics & Chaos", vm.ActiveLaboratory);
        Assert.Equal("bifurcation", vm.DiscreteDynamicsPreset);
        Assert.Equal("bifurcation", vm.DiscreteDynamicsDisplayMode);

        vm.OmnibarQuery = "barnsley fern ifs";
        vm.ExecuteOmnibar();
        Assert.Equal("Discrete Dynamics & Chaos", vm.ActiveLaboratory);
        Assert.Equal("barnsley", vm.DiscreteDynamicsPreset);
        Assert.Equal("ifs", vm.DiscreteDynamicsDisplayMode);

        vm.OmnibarQuery = "sierpinski gasket triangle";
        vm.ExecuteOmnibar();
        Assert.Equal("Discrete Dynamics & Chaos", vm.ActiveLaboratory);
        Assert.Equal("sierpinski", vm.DiscreteDynamicsPreset);
        Assert.Equal("ifs", vm.DiscreteDynamicsDisplayMode);

        // 17. Bifurcation & Catastrophe Theory
        vm.OmnibarQuery = "cusp catastrophe hysteresis";
        vm.ExecuteOmnibar();
        Assert.Equal("Bifurcation & Catastrophe Theory", vm.ActiveLaboratory);
        Assert.Equal("cusp", vm.CatastrophePreset);
        Assert.Equal("cusp", vm.CatastropheMode);

        vm.OmnibarQuery = "fold catastrophe saddlenode";
        vm.ExecuteOmnibar();
        Assert.Equal("Bifurcation & Catastrophe Theory", vm.ActiveLaboratory);
        Assert.Equal("fold", vm.CatastrophePreset);
        Assert.Equal("fold", vm.CatastropheMode);

        vm.OmnibarQuery = "butterfly catastrophe trimodal";
        vm.ExecuteOmnibar();
        Assert.Equal("Bifurcation & Catastrophe Theory", vm.ActiveLaboratory);
        Assert.Equal("butterfly", vm.CatastrophePreset);
        Assert.Equal("butterfly", vm.CatastropheMode);

        vm.OmnibarQuery = "zeeman catastrophe machine";
        vm.ExecuteOmnibar();
        Assert.Equal("Bifurcation & Catastrophe Theory", vm.ActiveLaboratory);
        Assert.Equal("zeeman", vm.CatastrophePreset);
        Assert.Equal("zeeman", vm.CatastropheMode);

        vm.OmnibarQuery = "swallowtail optical caustic";
        vm.ExecuteOmnibar();
        Assert.Equal("Bifurcation & Catastrophe Theory", vm.ActiveLaboratory);
        Assert.Equal("swallowtail", vm.CatastrophePreset);
        Assert.Equal("swallowtail", vm.CatastropheMode);
    }

    [Fact]
    public void MainWindowViewModel_LoadPreset_SwitchesAll19Laboratories()
    {
        using var vm = new MainWindowViewModel();

        var surfacePreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Surfaces & 3D");
        vm.LoadPreset(surfacePreset);
        Assert.Equal("Surfaces & 3D", vm.ActiveLaboratory);

        var fractalPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Fractals & Chaos");
        vm.LoadPreset(fractalPreset);
        Assert.Equal("Fractals & Chaos", vm.ActiveLaboratory);

        var wavePreset = MathPresetCatalog.AllPresets.First(p => p.Category == "PDE & Waves");
        vm.LoadPreset(wavePreset);
        Assert.Equal("PDE & Waves", vm.ActiveLaboratory);

        var curvePreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Space Curves 3D");
        vm.LoadPreset(curvePreset);
        Assert.Equal("Space Curves 3D", vm.ActiveLaboratory);
        Assert.Equal(FryMath.Core.Geometry.SpaceCurvePreset.CircularHelix, vm.SelectedSpaceCurvePreset);

        var zetaPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Zeta & Primes");
        vm.LoadPreset(zetaPreset);
        Assert.Equal("Zeta & Primes", vm.ActiveLaboratory);

        var heatPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Heat & Diffusion");
        vm.LoadPreset(heatPreset);
        Assert.Equal("Heat & Diffusion", vm.ActiveLaboratory);
        Assert.Equal("laplace", vm.HeatPreset);

        var quantumPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Quantum & Waves");
        vm.LoadPreset(quantumPreset);
        Assert.Equal("Quantum & Waves", vm.ActiveLaboratory);
        Assert.Equal("tunneling", vm.QuantumPreset);

        var relativityPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Relativity & Spacetime");
        vm.LoadPreset(relativityPreset);
        Assert.Equal("Relativity & Spacetime", vm.ActiveLaboratory);
        Assert.Equal("boost", vm.RelativityPreset);

        var fluidPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Fluid Dynamics");
        vm.LoadPreset(fluidPreset);
        Assert.Equal("Fluid Dynamics", vm.ActiveLaboratory);
        Assert.Equal("karman", vm.FluidPreset);

        var topoPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Topology & 4D Manifolds");
        vm.LoadPreset(topoPreset);
        Assert.Equal("Topology & 4D Manifolds", vm.ActiveLaboratory);
        Assert.Equal("tesseract", vm.TopologyPreset);

        var celestialPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Celestial & N-Body");
        vm.LoadPreset(celestialPreset);
        Assert.Equal("Celestial & N-Body", vm.ActiveLaboratory);
        Assert.Equal("figure8", vm.CelestialPreset);

        var hypPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Hyperbolic Geometry");
        vm.LoadPreset(hypPreset);
        Assert.Equal("Hyperbolic Geometry", vm.ActiveLaboratory);
        Assert.Equal("heptagonal", vm.HyperbolicPreset);

        var infoPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Information & Entropy");
        vm.LoadPreset(infoPreset);
        Assert.Equal("Information & Entropy", vm.ActiveLaboratory);
        Assert.Equal("huffman", vm.InformationPreset);

        var lagrangianPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Classical Mechanics");
        vm.LoadPreset(lagrangianPreset);
        Assert.Equal("Classical Mechanics", vm.ActiveLaboratory);
        Assert.Equal("double", vm.LagrangianPreset);

        var cryptoPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Cryptography & Finite Fields");
        vm.LoadPreset(cryptoPreset);
        Assert.Equal("Cryptography & Finite Fields", vm.ActiveLaboratory);
        Assert.Equal("secp256k1", vm.CryptoPreset);

        var dynamicsPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Discrete Dynamics & Chaos");
        vm.LoadPreset(dynamicsPreset);
        Assert.Equal("Discrete Dynamics & Chaos", vm.ActiveLaboratory);
        Assert.Equal("arnold", vm.DiscreteDynamicsPreset);

        vm.StepArnoldCat();
        Assert.Equal(1, vm.DiscreteDynamicsCatStep);
        vm.ResetArnoldCat();
        Assert.Equal(0, vm.DiscreteDynamicsCatStep);

        var catastrophePreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Bifurcation & Catastrophe Theory");
        vm.LoadPreset(catastrophePreset);
        Assert.Equal("Bifurcation & Catastrophe Theory", vm.ActiveLaboratory);
        Assert.Equal("cusp", vm.CatastrophePreset);

        var cliffordPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Clifford Geometric Algebra");
        vm.LoadPreset(cliffordPreset);
        Assert.Equal("Clifford Geometric Algebra", vm.ActiveLaboratory);
        Assert.Equal("wedge", vm.CliffordPreset);

        vm.SetCliffordPreset("rotor");
        Assert.Equal("rotor", vm.CliffordMode);
        Assert.True(vm.CliffordRotorAngle > 0);

        vm.OmnibarQuery = "clifford rotor sandwich";
        vm.ExecuteOmnibar();
        Assert.Equal("Clifford Geometric Algebra", vm.ActiveLaboratory);
        Assert.Equal("rotor", vm.CliffordMode);

        var diffGeomPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Differential Geometry");
        vm.LoadPreset(diffGeomPreset);
        Assert.Equal("Differential Geometry", vm.ActiveLaboratory);
        Assert.Equal("torus", vm.DiffGeomSurfaceType);

        vm.SetDifferentialGeometryPreset("sphere");
        Assert.Equal("sphere", vm.DiffGeomSurfaceType);
        Assert.True(vm.DiffGeomHeadingAngle > 0);

        vm.OmnibarQuery = "geodesic christoffel theorema egregium";
        vm.ExecuteOmnibar();
        Assert.Equal("Differential Geometry", vm.ActiveLaboratory);

        vm.OmnibarQuery = "pseudosphere tractrix hyperbolic";
        vm.ExecuteOmnibar();
        Assert.Equal("Differential Geometry", vm.ActiveLaboratory);
        Assert.Equal("pseudosphere", vm.DiffGeomSurfaceType);

        vm.OmnibarQuery = "enneper minimal surface";
        vm.ExecuteOmnibar();
        Assert.Equal("Differential Geometry", vm.ActiveLaboratory);
        Assert.Equal("enneper", vm.DiffGeomSurfaceType);

        vm.OmnibarQuery = "bonnet helicoid catenoid";
        vm.ExecuteOmnibar();
        Assert.Equal("Differential Geometry", vm.ActiveLaboratory);
        Assert.Equal("bonnet", vm.DiffGeomSurfaceType);

        var knotPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Knot Theory & Invariants");
        vm.LoadPreset(knotPreset);
        Assert.Equal("Knot Theory & Invariants", vm.ActiveLaboratory);
        Assert.Equal("trefoil", vm.KnotType);

        vm.SetKnotPreset("figure8");
        Assert.Equal("figure8", vm.KnotType);
        Assert.Equal("s1 s2^-1 s1 s2^-1", vm.KnotBraidWord);

        vm.OmnibarQuery = "knot trefoil skein jones polynomial";
        vm.ExecuteOmnibar();
        Assert.Equal("Knot Theory & Invariants", vm.ActiveLaboratory);
        Assert.Equal("trefoil", vm.KnotType);

        vm.OmnibarQuery = "borromean rings brunnian link";
        vm.ExecuteOmnibar();
        Assert.Equal("Knot Theory & Invariants", vm.ActiveLaboratory);
        Assert.Equal("borromean", vm.KnotType);

        vm.OmnibarQuery = "cinquefoil solomon torus knot";
        vm.ExecuteOmnibar();
        Assert.Equal("Knot Theory & Invariants", vm.ActiveLaboratory);
        Assert.Equal("cinquefoil", vm.KnotType);

        vm.OmnibarQuery = "artin braid yang baxter";
        vm.ExecuteOmnibar();
        Assert.Equal("Knot Theory & Invariants", vm.ActiveLaboratory);

        // 28. Symplectic Geometry & Hamiltonian Mechanics
        var hamiltonPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Symplectic & Hamiltonian");
        vm.LoadPreset(hamiltonPreset);
        Assert.Equal("Symplectic & Hamiltonian", vm.ActiveLaboratory);
        Assert.Equal("henonheiles", vm.HamiltonSystemType);

        vm.SetHamiltonianPreset("duffing");
        Assert.Equal("duffing", vm.HamiltonSystemType);
        Assert.Equal(0.5, vm.HamiltonEnergyLevel);

        vm.OmnibarQuery = "henon heiles symplectic kam tori";
        vm.ExecuteOmnibar();
        Assert.Equal("Symplectic & Hamiltonian", vm.ActiveLaboratory);
        Assert.Equal("henonheiles", vm.HamiltonSystemType);

        vm.OmnibarQuery = "duffing potential double well";
        vm.ExecuteOmnibar();
        Assert.Equal("Symplectic & Hamiltonian", vm.ActiveLaboratory);
        Assert.Equal("duffing", vm.HamiltonSystemType);

        vm.OmnibarQuery = "liouville phase space volume conservation";
        vm.ExecuteOmnibar();
        Assert.Equal("Symplectic & Hamiltonian", vm.ActiveLaboratory);
        Assert.Equal("liouville", vm.HamiltonSystemType);

        vm.OmnibarQuery = "yoshida 4th order symplectic integrator";
        vm.ExecuteOmnibar();
        Assert.Equal("Symplectic & Hamiltonian", vm.ActiveLaboratory);
        Assert.Equal("yoshida", vm.HamiltonSystemType);
        Assert.Equal("yoshida", vm.HamiltonIntegratorMode);

        vm.OmnibarQuery = "nonlinear pendulum separatrix libration";
        vm.ExecuteOmnibar();
        Assert.Equal("Symplectic & Hamiltonian", vm.ActiveLaboratory);
        Assert.Equal("pendulum", vm.HamiltonSystemType);

        // 29. Algebraic Topology, Simplicial Homology & Persistent Betti Numbers
        var homologyPreset = MathPresetCatalog.AllPresets.First(p => p.Category == "Algebraic Topology & Homology");
        vm.LoadPreset(homologyPreset);
        Assert.Equal("Algebraic Topology & Homology", vm.ActiveLaboratory);
        Assert.Equal("torus", vm.HomologyComplexType);

        vm.SetHomologyPreset("sphere");
        Assert.Equal("sphere", vm.HomologyComplexType);

        vm.OmnibarQuery = "simplicial homology betti numbers euler-poincare";
        vm.ExecuteOmnibar();
        Assert.Equal("Algebraic Topology & Homology", vm.ActiveLaboratory);
        Assert.Equal("torus", vm.HomologyComplexType);

        vm.OmnibarQuery = "klein bottle nonorientable torsion";
        vm.ExecuteOmnibar();
        Assert.Equal("Algebraic Topology & Homology", vm.ActiveLaboratory);
        Assert.Equal("klein", vm.HomologyComplexType);

        vm.OmnibarQuery = "real projective plane rp2 minimal complex";
        vm.ExecuteOmnibar();
        Assert.Equal("Algebraic Topology & Homology", vm.ActiveLaboratory);
        Assert.Equal("projective", vm.HomologyComplexType);

        vm.OmnibarQuery = "mobius strip single boundary circle";
        vm.ExecuteOmnibar();
        Assert.Equal("Algebraic Topology & Homology", vm.ActiveLaboratory);
        Assert.Equal("mobius", vm.HomologyComplexType);

        vm.OmnibarQuery = "vietoris rips point cloud persistence barcode";
        vm.ExecuteOmnibar();
        Assert.Equal("Algebraic Topology & Homology", vm.ActiveLaboratory);
        Assert.Equal("persistence", vm.HomologyComplexType);
    }

    [Fact]
    public void InspectorBuilderFeatures_WorkSeamlesslyWithoutCode()
    {
        using var vm = new MainWindowViewModel();

        // 1. Visual Calculus Dynamic Parameters
        vm.DynamicParamA = 2.5;
        vm.DynamicParamB = -1.2;
        vm.AddPresetCurve("a * sin(b * x)");
        Assert.True(vm.Functions.Count >= 2);
        var customFn = vm.Functions.Last();
        Assert.NotNull(customFn.CompiledFunc);
        double val = customFn.CompiledFunc(1.0);
        Assert.Equal(2.5 * Math.Sin(-1.2 * 1.0), val, precision: 4);

        // 2. Linear Algebra Interactive Matrix Grid Inputs & Vector Transform
        vm.MatrixM11 = 2.0;
        vm.MatrixM12 = -1.0;
        vm.MatrixM21 = 0.5;
        vm.MatrixM22 = 3.0;
        Assert.Equal(2.0, vm.Matrix.M11);
        Assert.Equal(-1.0, vm.Matrix.M12);
        Assert.Equal(0.5, vm.Matrix.M21);
        Assert.Equal(3.0, vm.Matrix.M22);
        Assert.Equal(6.5, vm.Matrix.Determinant);

        vm.TestVectorU1 = 2.0;
        vm.TestVectorU2 = 1.0;
        // v = [2*2 + (-1)*1, 0.5*2 + 3*1] = [3.0, 4.0]
        Assert.Equal(3.0, vm.TransformedVectorV1);
        Assert.Equal(4.0, vm.TransformedVectorV2);

        vm.TransposeMatrix();
        Assert.Equal(-1.0, vm.Matrix.M21);
        Assert.Equal(0.5, vm.Matrix.M12);

        vm.NegateMatrix();
        Assert.Equal(-2.0, vm.Matrix.M11);
        Assert.Equal(-3.0, vm.Matrix.M22);

        vm.ResetIdentityMatrix();
        Assert.Equal(1.0, vm.Matrix.M11);
        Assert.Equal(0.0, vm.Matrix.M12);
        Assert.Equal(1.0, vm.Matrix.M22);

        // 3. Vector Field ODE Custom Builder
        vm.InsertVectorFieldP("-2*y");
        vm.InsertVectorFieldQ("3*x");
        Assert.Contains("-2*y", vm.VectorFieldP);
        Assert.Contains("3*x", vm.VectorFieldQ);
        Assert.Contains("Active ODE", vm.VectorFieldSystemStatus);

        // 4. Knot Theory Artin Braid Word Builder
        vm.ClearBraidWord();
        vm.AppendBraidGenerator("s1");
        vm.AppendBraidGenerator("s2^-1");
        vm.AppendBraidGenerator("s1");
        Assert.Equal("s1 s2^-1 s1", vm.KnotBraidWord.Trim());

        vm.SetTorusKnotParams("5,2");
        Assert.Equal(5, vm.KnotTorusP);
        Assert.Equal(2, vm.KnotTorusQ);
        Assert.Equal("torus", vm.KnotType);

        // 5. Algebraic Topology Filtration Quick Chips
        vm.SetHomologyFiltration("0.85");
        Assert.Equal(0.85, vm.HomologyFiltrationRadius, precision: 2);

        // 6. Complex Analysis Möbius Transformation Builder
        vm.SetMobiusPreset("inversion");
        Assert.Equal(0.0, vm.MobiusParamA);
        Assert.Equal(1.0, vm.MobiusParamB);
        Assert.Equal(1.0, vm.MobiusParamC);
        Assert.Equal(0.0, vm.MobiusParamD);
        Assert.False(vm.MobiusIsSingular);
        Assert.Contains("Non-singular", vm.MobiusDeterminantStatus);

        vm.MobiusParamA = 1.0;
        vm.MobiusParamB = 2.0;
        vm.MobiusParamC = 2.0;
        vm.MobiusParamD = 4.0;
        Assert.True(vm.MobiusIsSingular);
        Assert.Contains("Singular", vm.MobiusDeterminantStatus);

        vm.InsertComplexSymbol("eᶻ");
        Assert.Contains("eᶻ", vm.ComplexCustomFormula);

        // 7. Multivariable Calculus Quadric Surface Constructor
        vm.SurfaceParamA = 1.0;
        vm.SurfaceParamB = -1.0;
        vm.SurfaceParamC = 0.0;
        vm.SurfaceProbeX = 0.0;
        vm.SurfaceProbeY = 0.0;
        Assert.Equal(0.0, vm.SurfaceEvalZ);
        Assert.Equal(0.0, vm.SurfaceGradX);
        Assert.Equal(0.0, vm.SurfaceGradY);
        Assert.True(vm.SurfaceHessianD < 0);
        Assert.Contains("Saddle Point", vm.SurfaceCurvatureStatus);

        vm.SurfaceParamB = 1.0;
        Assert.True(vm.SurfaceHessianD > 0);
        Assert.Contains("Elliptic Bowl", vm.SurfaceCurvatureStatus);

        // 8. Chaos & Fractals Julia Presets & Bailout
        vm.SetFamousJuliaPreset("rabbit");
        Assert.Equal(-0.123, vm.FractalJuliaCr, precision: 3);
        Assert.Equal(0.745, vm.FractalJuliaCi, precision: 3);
        vm.FractalEscapeRadius = 5.0;
        Assert.Equal(5.0, vm.FractalEscapeRadius);

        // 9. Quantum Wavepacket Architect
        vm.QuantumBarrierHeight = 18.0;
        vm.QuantumBarrierWidth = 0.45;
        vm.QuantumWavepacketK0 = 12.0;
        vm.QuantumWavepacketSigma = 0.25;
        vm.InjectCustomWavepacket();
        Assert.Equal("free", vm.QuantumPreset);

        // 10. Cryptography Weierstrass Discriminant & Modulus
        vm.SetCryptoCurveCoeffs("bitcoin");
        Assert.Equal(0.0, vm.CryptoParamA);
        Assert.Equal(7.0, vm.CryptoParamB);
        // Δ = -16(4*0 + 27*49) = -16 * 1323 = -21168
        Assert.True(vm.CryptoDiscriminant < 0);
        Assert.Contains("Single Connected Branch", vm.CryptoCurveTopologyBadge);

        vm.SetCryptoCurveCoeffs("twocomp");
        Assert.True(vm.CryptoDiscriminant > 0);
        Assert.Contains("Two Components", vm.CryptoCurveTopologyBadge);

        vm.SetCryptoPrimeP(23);
        Assert.Equal(23, vm.CryptoPrimeP);

        // 11. Catastrophe Cusp Discriminant & Hysteresis
        vm.SetCatastrophePreset("cusp");
        vm.SetCatastropheMaxwellPoint();
        Assert.Equal(-2.5, vm.CatastropheParamA);
        Assert.Equal(0.0, vm.CatastropheParamB);
        Assert.True(vm.CatastropheCuspDiscriminant < 0);
        Assert.Contains("Bistable", vm.CatastropheCuspStatus);

        vm.CatastropheParamB = 1.5;
        vm.TriggerHysteresisSweep();
        Assert.Equal(-1.5, vm.CatastropheParamB);

        vm.SetCatastropheCriticalPoint();
        Assert.Equal(0.0, vm.CatastropheParamA);
        Assert.Equal(0.0, vm.CatastropheParamB);
        Assert.Contains("Fold Bifurcation", vm.CatastropheCuspStatus);

        // 12. Symplectic Duffing Parameters & Energy Levels
        vm.SetHamiltonianEnergyLevel(0.08);
        Assert.Equal(0.08, vm.HamiltonEnergyLevel, precision: 3);
        vm.HamiltonDuffingAlpha = -1.0;
        vm.HamiltonDuffingBeta = 1.0;
        Assert.Equal(-1.0, vm.HamiltonDuffingAlpha);
        Assert.Equal(1.0, vm.HamiltonDuffingBeta);
    }

    [Fact]
    public void Laboratory30_LieAlgebrasAndDynkinDiagrams_StateAndPresets()
    {
        using var vm = new MainWindowViewModel();

        // Preset loading
        var presets = MathPresetCatalog.AllPresets.Where(p => p.Category == "Lie Algebras & Dynkin" || p.Id.StartsWith("lie.")).ToList();
        Assert.True(presets.Count >= 6);

        var a2Preset = presets.First(p => p.Id == "lie.a2.su3");
        vm.LoadPreset(a2Preset);
        Assert.Equal("Lie Algebras & Dynkin", vm.ActiveLaboratory);
        Assert.Equal("A2", vm.LieType);
        Assert.Contains("su(3)", vm.LieFormulaTitle);

        var g2Preset = presets.First(p => p.Id == "lie.g2");
        vm.LoadPreset(g2Preset);
        Assert.Equal("G2", vm.LieType);
        Assert.Contains("G₂", vm.LieFormulaTitle);

        var e8Preset = presets.First(p => p.Id == "lie.e8");
        vm.LoadPreset(e8Preset);
        Assert.Equal("E8", vm.LieType);
        Assert.Contains("E₈", vm.LieFormulaTitle);

        // Command setting
        vm.SetLieType("b2");
        Assert.Equal("B2", vm.LieType);
        Assert.Contains("so(5)", vm.LieFormulaTitle);

        vm.SetLieType("a3");
        Assert.Equal("A3", vm.LieType);
        Assert.Contains("su(4)", vm.LieFormulaTitle);

        vm.SetLieType("b3");
        Assert.Equal("B3", vm.LieType);
        Assert.Contains("so(7)", vm.LieFormulaTitle);

        // Overlays and camera controls
        vm.LieShowDynkinDiagram = false;
        Assert.False(vm.LieShowDynkinDiagram);
        vm.LieShowDynkinDiagram = true;

        vm.LieShowCartanMatrix = false;
        Assert.False(vm.LieShowCartanMatrix);

        vm.LieCameraYaw = 1.25;
        vm.LieCameraPitch = -0.5;
        Assert.Equal(1.25, vm.LieCameraYaw, precision: 2);
        Assert.Equal(-0.5, vm.LieCameraPitch, precision: 2);

        // Omnibar routing
        vm.OmnibarQuery = "e8 exceptional simple lie algebra heterotic string";
        vm.ExecuteOmnibar();
        Assert.Equal("Lie Algebras & Dynkin", vm.ActiveLaboratory);
        Assert.Equal("E8", vm.LieType);

        vm.OmnibarQuery = "g2 octonions exceptional simple lie algebra";
        vm.ExecuteOmnibar();
        Assert.Equal("Lie Algebras & Dynkin", vm.ActiveLaboratory);
        Assert.Equal("G2", vm.LieType);

        vm.OmnibarQuery = "so(5) orthogonal lie algebra b2 root system";
        vm.ExecuteOmnibar();
        Assert.Equal("Lie Algebras & Dynkin", vm.ActiveLaboratory);
        Assert.Equal("B2", vm.LieType);

        vm.OmnibarQuery = "su(4) special unitary cuboctahedron a3";
        vm.ExecuteOmnibar();
        Assert.Equal("Lie Algebras & Dynkin", vm.ActiveLaboratory);
        Assert.Equal("A3", vm.LieType);
    }

    [Fact]
    public void Drawer3_FourierHarmonicSeriesBuilder_InvariantsAndFilters()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Fourier & Waves");

        // Filter: Odd harmonics (Square)
        vm.SetHarmonicFilter("odd");
        Assert.Equal("square", vm.FourierPreset);
        Assert.Contains("sin((2k-1)ωt)", vm.FourierWaveformSynthesis);
        Assert.Contains("48.3%", vm.FourierTotalHarmonicDistortion);
        Assert.Contains("Gibbs", vm.FourierThdStatus);

        // Filter: All harmonics (Sawtooth)
        vm.SetHarmonicFilter("even");
        Assert.Equal("sawtooth", vm.FourierPreset);
        Assert.Contains("sin(kωt)", vm.FourierWaveformSynthesis);
        Assert.Contains("80.3%", vm.FourierTotalHarmonicDistortion);

        // Filter: Triangle
        vm.SetHarmonicFilter("triangle");
        Assert.Equal("triangle", vm.FourierPreset);
        Assert.Contains("12.1%", vm.FourierTotalHarmonicDistortion);

        // Filter: Low-pass
        vm.SetHarmonicFilter("lowpass");
        Assert.Equal(5, vm.Harmonics);
        Assert.Contains("Parseval Energy", vm.FourierSpectralEnergy);
    }

    [Fact]
    public void Drawer4_ProbabilityDistributionBuilder_ExpectationVarianceEntropy()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Probability & Stats");

        // Fair coin preset
        vm.SetDistributionPreset("symmetric");
        Assert.Equal(0.50, vm.ProbabilityP);
        Assert.Equal(6.0, vm.StatDistMean);
        Assert.Equal(3.0, vm.StatVariance);
        Assert.Contains("Symmetric", vm.StatSkewnessStatus);
        Assert.Contains("1.000 bits", vm.StatEntropyNats);

        // Skewed presets
        vm.SetDistributionPreset("skew_right");
        Assert.Equal(0.20, vm.ProbabilityP);
        Assert.Equal(2.4, vm.StatDistMean, 2);
        Assert.Contains("Right-Skewed", vm.StatSkewnessStatus);

        vm.SetDistributionPreset("skew_left");
        Assert.Equal(0.80, vm.ProbabilityP);
        Assert.Equal(9.6, vm.StatDistMean, 2);
        Assert.Contains("Left-Skewed", vm.StatSkewnessStatus);
    }

    [Fact]
    public void Drawer10_WaveMediumCflStabilityBuilder_Parameters()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("PDE & Waves");

        vm.WavePropagationSpeedC = 1.0;
        Assert.True(vm.WaveCflNumber < 0.707);
        Assert.Contains("Stable", vm.WaveCflStatus);

        vm.SetWaveExcitation("gaussian");
        Assert.Equal("gaussian", vm.WaveExcitationType);
        Assert.Contains("Gaussian", vm.WaveFormulaTitle);

        vm.SetWaveExcitation("dipole");
        Assert.Equal("dipole", vm.WaveExcitationType);
        Assert.Contains("Dipole", vm.WaveFormulaTitle);
    }

    [Fact]
    public void Drawer15_SpecialRelativityKinematicsBuilder_Invariants()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Relativity & Spacetime");

        vm.SetRelativityBetaPreset("0.866");
        Assert.Equal(0.866, vm.RelativityBeta, precision: 3);
        Assert.Contains("2.00", vm.RelativityLorentzGamma);
        Assert.Contains("Slow Clock", vm.RelativityTimeDilationFactor);
        Assert.Contains("Lorentz Shrink", vm.RelativityLengthContraction);
        Assert.Contains("Timelike", vm.RelativitySpacetimeIntervalStatus);

        vm.SetRelativityBetaPreset("0.0");
        Assert.Equal(0.0, vm.RelativityBeta);
        Assert.Contains("Static", vm.RelativitySpacetimeIntervalStatus);
    }

    [Fact]
    public void Drawer21_ClassicalMechanicsLagrangianBuilder_EnergyAndRegime()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Classical Mechanics");

        vm.SetLagrangianRegime("chaotic");
        Assert.Equal("double", vm.LagrangianPreset);
        Assert.NotEqual(0.0, vm.LagrangianTotalEnergy);
        Assert.Contains("Chaos", vm.LagrangianMotionRegime);

        vm.SetLagrangianRegime("inverted");
        Assert.Equal("kapitza", vm.LagrangianPreset);
        Assert.Contains("Kapitza", vm.LagrangianMotionRegime);

        vm.SetLagrangianRegime("regular");
        Assert.Equal("poincare", vm.LagrangianPreset);
        Assert.Contains("KAM", vm.LagrangianMotionRegime);
    }

    [Fact]
    public void Drawer26_DifferentialGeometryCurvatureBuilder_Invariants()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Differential Geometry");

        vm.SetDiffGeomGeometryPreset("sphere");
        Assert.Equal("sphere", vm.DiffGeomSurfaceType);
        Assert.Contains("+1.000", vm.DiffGeomGaussianCurvatureK);
        Assert.Contains("Elliptic", vm.DiffGeomCurvatureStatus);

        vm.SetDiffGeomGeometryPreset("pseudosphere");
        Assert.Equal("pseudosphere", vm.DiffGeomSurfaceType);
        Assert.Contains("-1.000", vm.DiffGeomGaussianCurvatureK);
        Assert.Contains("Hyperbolic", vm.DiffGeomCurvatureStatus);

        vm.SetDiffGeomGeometryPreset("enneper");
        Assert.Equal("enneper", vm.DiffGeomSurfaceType);
        Assert.Contains("Minimal", vm.DiffGeomCurvatureStatus);

        vm.SetDiffGeomGeometryPreset("torus_outer");
        Assert.Equal("torus", vm.DiffGeomSurfaceType);
        Assert.Contains("Outer Equator", vm.DiffGeomGaussianCurvatureK);

        vm.SetDiffGeomGeometryPreset("torus_inner");
        Assert.Equal("torus", vm.DiffGeomSurfaceType);
        Assert.Contains("Inner Throat", vm.DiffGeomGaussianCurvatureK);
    }

    [Fact]
    public void Drawer5_DynamicalChaos_AttractorParametersAndLyapunov()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Dynamical Chaos");

        vm.SetChaosAttractorPreset("butterfly");
        Assert.Equal(10.0, vm.ChaosSigma);
        Assert.Equal(28.0, vm.ChaosRho);
        Assert.Contains("Chaotic", vm.ChaosLyapunovStatus);
        Assert.Contains("div(F) = -13.67", vm.ChaosContractionRate);
        Assert.Contains("1.13×", vm.ChaosRayleighRatio);

        vm.SetChaosAttractorPreset("subcritical");
        Assert.Equal(22.0, vm.ChaosRho);
        Assert.Contains("Pitchfork", vm.ChaosLyapunovStatus);

        vm.ChaosRho = 0.5;
        Assert.Contains("Stable Origin", vm.ChaosLyapunovStatus);

        vm.PerturbChaosTrajectory();
        Assert.True(vm.ShowChaosDualTrajectories);
    }

    [Fact]
    public void Drawer6_VectorFields_TopologyAndFlowInvariants()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Vector Fields & ODEs");

        vm.SetVectorFieldTopologyPreset("vortex");
        Assert.Equal("-y", vm.VectorFieldP);
        Assert.Equal("x", vm.VectorFieldQ);
        Assert.Contains("Incompressible", vm.VectorFieldDivergenceStatus);
        Assert.Contains("+2.000", vm.VectorFieldCurlStatus);
        Assert.Contains("Center", vm.VectorFieldFixedPointType);

        vm.SetVectorFieldTopologyPreset("saddle");
        Assert.Equal("x", vm.VectorFieldP);
        Assert.Equal("-y", vm.VectorFieldQ);
        Assert.Contains("Saddle", vm.VectorFieldFixedPointType);

        vm.SetVectorFieldProbePoint("origin");
        Assert.Equal(0.0, vm.VectorFieldSeedX);
        Assert.Equal(0.0, vm.VectorFieldSeedY);

        vm.SetVectorFieldProbePoint("two_zero");
        Assert.Equal(2.0, vm.VectorFieldSeedX);
        Assert.Equal(0.0, vm.VectorFieldSeedY);
    }

    [Fact]
    public void Drawer11_SpaceCurves_FrenetSerretInvariants()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Space Curves 3D");

        vm.SetSpaceCurveGeometry("helix");
        Assert.Equal(FryMath.Core.Geometry.SpaceCurvePreset.CircularHelix, vm.SelectedSpaceCurvePreset);
        Assert.Contains("κ =", vm.CurveCurvatureKappa);
        Assert.Contains("τ =", vm.CurveTorsionTau);
        Assert.Contains("R =", vm.CurveRadiusOfCurvature);
        Assert.Contains("Lancret Helix", vm.CurveFrenetRegimeStatus);

        vm.SetSpaceCurveGeometry("trefoil");
        Assert.Equal(FryMath.Core.Geometry.SpaceCurvePreset.TrefoilKnot, vm.SelectedSpaceCurvePreset);
        Assert.Contains("General 3D Space Curve", vm.CurveFrenetRegimeStatus);
    }

    [Fact]
    public void Drawer12_RiemannZeta_ZerosAndExplicitPrimeError()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Zeta & Primes");

        vm.SetZetaZeroTeleport(1);
        Assert.Equal(14.1347, vm.ZetaProbeT, precision: 3);
        Assert.Contains("γ_1", vm.ZetaNearestZeroOrdinal);
        Assert.Contains("14.1347", vm.ZetaNearestZeroHeight);
        Assert.Contains("Zero Hit", vm.ZetaGramIntervalStatus);

        vm.SetZetaZeroTeleport(2);
        Assert.Equal(21.0220, vm.ZetaProbeT, precision: 3);
        Assert.Contains("γ_2", vm.ZetaNearestZeroOrdinal);

        Assert.Contains("RH Bound", vm.ZetaPrimeLiResidual);
    }

    [Fact]
    public void Drawer13_HeatDiffusion_BoundaryConditionsAndCfl()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Heat & Diffusion");

        vm.SetHeatBoundaryCondition("dirichlet");
        Assert.Equal("Dirichlet", vm.HeatBoundaryType);
        Assert.Contains("Dirichlet", vm.HeatBoundaryStatus);
        Assert.Contains("Max Principle Active", vm.HeatMaximumPrincipleStatus);
        Assert.Contains("Stable", vm.HeatStabilityCflStatus);

        vm.SetHeatBoundaryCondition("neumann");
        Assert.Equal("Neumann", vm.HeatBoundaryType);
        Assert.Equal("insulated", vm.HeatPreset);
    }

    [Fact]
    public void Drawer16_FluidDynamics_ReynoldsAndFlowRegimes()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Fluid Dynamics");

        vm.SetFluidReynoldsPreset("karman");
        Assert.Equal(0.0006, vm.FluidViscosity);
        Assert.Equal("karman", vm.FluidPreset);
        Assert.Contains("Re = 1667", vm.FluidReynoldsNumber);
        Assert.Contains("Transitional Wake", vm.FluidFlowRegimeStatus);
        Assert.Contains("St ≈", vm.FluidStrouhalNumber);

        vm.SetFluidReynoldsPreset("creeping");
        Assert.Equal(0.003, vm.FluidViscosity);
        Assert.Contains("Re = 333", vm.FluidReynoldsNumber);

        vm.SetFluidReynoldsPreset("turbulent");
        Assert.Equal(0.00015, vm.FluidViscosity);
        Assert.Contains("Turbulent Cascade", vm.FluidFlowRegimeStatus);
    }

    [Fact]
    public void Laboratory31_MillenniumProblems_StateAndSimulation()
    {
        using var vm = new MainWindowViewModel();

        // 1. Omnibar Navigation
        vm.OmnibarQuery = "poincare conjecture";
        vm.ExecuteOmnibar();
        Assert.Equal("Millennium Problems", vm.ActiveLaboratory);
        Assert.Equal(FryMath.Core.Millennium.MillenniumProblemType.PoincareConjecture, vm.SelectedMillenniumProblem);
        Assert.Contains("Perelman", vm.ActiveMillenniumProblemInfo.ProposerOrSolver);

        // 2. Preset loading
        var presets = MathPresetCatalog.AllPresets.Where(p => p.Category == "Millennium Problems" || p.Id.StartsWith("millennium.")).ToList();
        Assert.Equal(7, presets.Count);

        var riemannPreset = presets.First(p => p.Id == "millennium.riemann");
        vm.LoadPreset(riemannPreset);
        Assert.Equal("Millennium Problems", vm.ActiveLaboratory);
        Assert.Equal(FryMath.Core.Millennium.MillenniumProblemType.RiemannHypothesis, vm.SelectedMillenniumProblem);
        Assert.Contains("Montgomery", vm.ActiveMillenniumProblemInfo.SolutionOrCurrentAttack);
        Assert.True(vm.ActiveRiemannGueState.GueRepulsionIndex > 0.0);

        // 3. Problem switching commands
        vm.SelectMillenniumProblem("bsd");
        Assert.Equal(FryMath.Core.Millennium.MillenniumProblemType.BirchSwinnertonDyer, vm.SelectedMillenniumProblem);
        vm.SetBsdCurvePreset("rank1");
        Assert.Equal(-2.0, vm.MillenniumBsdParamA);
        Assert.Equal(0.0, vm.MillenniumBsdParamB);
        Assert.Equal(1, vm.ActiveBsdEllipticState.AnalyticRank);

        vm.SelectMillenniumProblem("navierstokes");
        Assert.Equal(FryMath.Core.Millennium.MillenniumProblemType.NavierStokesSmoothness, vm.SelectedMillenniumProblem);
        vm.SetNavierRegime("turbulent_blowup");
        Assert.Equal(0.00005, vm.MillenniumNavierViscosity);
        Assert.Equal(2.6, vm.MillenniumNavierTime);
        Assert.True(vm.ActiveNavierStokesBlowupState.Enstrophy > 0.0);

        vm.SelectMillenniumProblem("yangmills");
        Assert.Equal(FryMath.Core.Millennium.MillenniumProblemType.YangMillsMassGap, vm.SelectedMillenniumProblem);
        vm.SetYangMillsCoupling("strong");
        Assert.Equal(1.2, vm.MillenniumYangMillsBeta);
        Assert.True(vm.ActiveYangMillsLatticeState.MassGapDelta > 0.0);
        Assert.Contains("Confinement", vm.ActiveYangMillsLatticeState.ConfinementRegime);

        vm.SelectMillenniumProblem("pvsnp");
        Assert.Equal(FryMath.Core.Millennium.MillenniumProblemType.PversusNP, vm.SelectedMillenniumProblem);
        vm.SetSatClauseRatio("critical");
        Assert.Equal(4.267, vm.MillenniumSatAlpha);
        Assert.InRange(vm.ActivePvsNpSatState.SatisfiabilityProbability, 0.4, 0.6);

        vm.SelectMillenniumProblem("hodge");
        Assert.Equal(FryMath.Core.Millennium.MillenniumProblemType.HodgeConjecture, vm.SelectedMillenniumProblem);
        vm.SetHodgeVariety("k3");
        Assert.Equal("k3", vm.MillenniumHodgeVariety);
        Assert.Equal(20, vm.ActiveHodgeDiamondState.RationalHodgeClassesCount);
        Assert.True(vm.ActiveHodgeDiamondState.IsLefschetzApplicable);

        // 4. Ricci Flow stepping & surgery
        vm.SelectMillenniumProblem("poincare");
        vm.MillenniumRicciTime = 0.0;
        vm.StepRicciFlow(0.5);
        Assert.Equal(0.5, vm.MillenniumRicciTime);
        vm.TriggerRicciSurgery();
        Assert.Equal(2.4, vm.MillenniumRicciTime);
        Assert.True(vm.ActiveRicciFlowState.SurgeryCount > 0);
    }

    [Fact]
    public void Drawer31_MillenniumContinuousAnimations_AndAudioSonification()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Millennium Problems");

        // 1. Ricci flow animation toggle
        Assert.False(vm.IsRicciFlowAnimating);
        vm.ToggleRicciFlowAnimation();
        Assert.True(vm.IsRicciFlowAnimating);
        Assert.Equal(FryMath.Core.Millennium.MillenniumProblemType.PoincareConjecture, vm.SelectedMillenniumProblem);

        // 2. Navier animation toggle
        Assert.False(vm.IsNavierAnimating);
        vm.ToggleNavierAnimation();
        Assert.True(vm.IsNavierAnimating);
        Assert.Equal(FryMath.Core.Millennium.MillenniumProblemType.NavierStokesSmoothness, vm.SelectedMillenniumProblem);
        // Switching away from Poincare stopped Ricci animation
        Assert.False(vm.IsRicciFlowAnimating);

        // 3. Switching to BSD resets both animations
        vm.SelectMillenniumProblem("bsd");
        Assert.False(vm.IsRicciFlowAnimating);
        Assert.False(vm.IsNavierAnimating);

        // 4. Riemann zero audio sonification
        vm.SelectLaboratory("Zeta & Primes");
        vm.ZetaProbeT = 14.1347;
        var ex1 = Record.Exception(() => vm.PlayRiemannZero(null));
        Assert.Null(ex1);

        var ex2 = Record.Exception(() => vm.PlayRiemannZero(1));
        Assert.Null(ex2);

        var ex3 = Record.Exception(() => vm.PlayRiemannZero(2));
        Assert.Null(ex3);

        var ex4 = Record.Exception(() => vm.PlayRiemannZero("25.01"));
        Assert.Null(ex4);

        // 5. Quick chip navigation via InsertSymbol
        vm.SelectLaboratory("Visual Calculus");
        vm.InsertSymbol("millennium");
        Assert.Equal("Millennium Problems", vm.ActiveLaboratory);
    }

    [Fact]
    public void MainWindowViewModel_AnalyticalAntiderivative_And_IntegralCard()
    {
        using var vm = new MainWindowViewModel();

        // Default expression: x^3 - 3*x
        Assert.NotEmpty(vm.AnalyticalAntiderivative);
        Assert.Contains("+ C", vm.AnalyticalAntiderivative);
        Assert.NotNull(vm.IntegralInfo);
        Assert.Equal(vm.AnalyticalAntiderivative, vm.IntegralInfo.AntiderivativeText);

        // Update expression
        vm.OmnibarQuery = "sin(x)";
        vm.ExecuteOmnibar();

        Assert.Contains("cos", vm.AnalyticalAntiderivative);
        Assert.Contains("+ C", vm.AnalyticalAntiderivative);
        Assert.Equal(vm.AnalyticalAntiderivative, vm.IntegralInfo.AntiderivativeText);
    }

    [Fact]
    public void MainWindowViewModel_BsdGroupLaw_PointStepping()
    {
        using var vm = new MainWindowViewModel();

        vm.SelectMillenniumProblem("bsd");
        vm.SetBsdCurvePreset("rank1");

        Assert.Equal(1, vm.BsdSelectedMultiplier);
        var p1 = vm.ActiveBsdEllipticPoint;
        Assert.NotNull(p1);
        Assert.Equal(1, p1.Multiplier);
        Assert.False(p1.IsInfinity);

        // Step generator 1P -> 2P
        vm.StepBsdPointAddition();
        Assert.Equal(2, vm.BsdSelectedMultiplier);
        var p2 = vm.ActiveBsdEllipticPoint;
        Assert.NotNull(p2);
        Assert.Equal(2, p2.Multiplier);

        // Reset
        vm.ResetBsdPointAddition();
        Assert.Equal(1, vm.BsdSelectedMultiplier);
        Assert.Equal(1, vm.ActiveBsdEllipticPoint!.Multiplier);
    }

    [Fact]
    public void Laboratory31_MillenniumProblems_OpenAiNavierSolution_AndRiemannAttackFrameworks()
    {
        using var vm = new MainWindowViewModel();
        vm.SelectLaboratory("Millennium Problems");

        // 1. Navier-Stokes with OpenAI Lean 4 Formal Blowup Solution
        vm.SelectMillenniumProblem("navierstokes");
        Assert.True(vm.ActiveMillenniumProblemInfo.IsSolved);
        Assert.Contains("OpenAI", vm.ActiveMillenniumProblemInfo.ProposerOrSolver);

        vm.SetNavierRegime("openai");
        Assert.Equal(0.00001, vm.MillenniumNavierViscosity);
        Assert.Equal(1.84, vm.MillenniumNavierTime);
        Assert.True(vm.ActiveNavierStokesBlowupState.IsOpenAiBlowupVerified);
        Assert.True(vm.ActiveNavierStokesBlowupState.IsBlowupImminent);
        Assert.Contains("Lean 4", vm.ActiveNavierStokesBlowupState.ProofFormalism);
        Assert.Equal(1.84, vm.ActiveNavierStokesBlowupState.SingularityTimeTStar);

        // 2. Riemann Hypothesis: Hilbert-Pólya Operator Attack
        vm.SelectMillenniumProblem("riemann");
        vm.SetRiemannAttack("hilbert_polya");
        Assert.Equal(FryMath.Core.Millennium.RiemannAttackFramework.HilbertPolya, vm.SelectedRiemannAttack);
        Assert.NotNull(vm.ActiveHilbertPolyaState);
        Assert.True(vm.ActiveHilbertPolyaState.QuantumEigenvalues.Count > 0);
        Assert.Contains("H = -i", vm.ActiveHilbertPolyaState.OperatorFormula);
        Assert.Contains("Connes", vm.ActiveHilbertPolyaState.SpectralProofStatus);

        // 3. Riemann Hypothesis: de Bruijn-Newman Flow Attack
        vm.SetRiemannAttack("debruijn_newman");
        Assert.Equal(FryMath.Core.Millennium.RiemannAttackFramework.DeBruijnNewman, vm.SelectedRiemannAttack);
        vm.SetDeBruijnT(0.0);
        Assert.Equal(0.0, vm.RiemannDeBruijnT);
        Assert.NotNull(vm.ActiveDeBruijnNewmanState);
        Assert.Equal(0.0, vm.ActiveDeBruijnNewmanState.ParameterT);
        Assert.True(vm.ActiveDeBruijnNewmanState.AllRootsReal);

        // 4. Riemann Hypothesis: Xian-Jin Li Criterion Attack
        vm.SetRiemannAttack("li_criterion");
        Assert.Equal(FryMath.Core.Millennium.RiemannAttackFramework.LiCriterion, vm.SelectedRiemannAttack);
        Assert.NotNull(vm.ActiveLiCriterionState);
        Assert.True(vm.ActiveLiCriterionState.AllPositive);
        Assert.All(vm.ActiveLiCriterionState.Coefficients, c => Assert.True(c.IsPositive));

        // 5. Riemann Hypothesis: Brent-Dekker Dynamic Zero Hunter
        vm.SetRiemannAttack("zero_hunter");
        Assert.Equal(FryMath.Core.Millennium.RiemannAttackFramework.ZeroHunter, vm.SelectedRiemannAttack);
        vm.RiemannHunterTMin = 10.0;
        vm.RiemannHunterTMax = 32.0;
        vm.HuntRiemannZeros();
        Assert.NotNull(vm.ActiveZeroHunterState);
        Assert.True(vm.ActiveZeroHunterState.FoundZeros.Count >= 3);
        Assert.True(vm.ActiveZeroHunterState.GramPoints.Count > 0);

        // 6. Riemann Chord Sonification
        var ex = Record.Exception(() => vm.PlayRiemannChord());
        Assert.Null(ex);
    }
}






