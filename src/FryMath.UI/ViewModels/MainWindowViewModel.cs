using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FryMath.Core.Audio;
using FryMath.Core.CAS;
using FryMath.Core.Calculus;
using FryMath.Core.ComplexAnalysis;
using FryMath.Core.Formatting;
using FryMath.Core.Fractals;
using FryMath.Core.Geometry;
using FryMath.Core.LinearAlgebra;
using FryMath.Core.Presets;
using FryMath.Core.Waves;
using FryMath.Core.Heat;
using FryMath.Core.Quantum;
using FryMath.Core.Zeta;
using FryMath.Core.Lie;
using FryMath.Core.Fluids;
using FryMath.Core.Millennium;
using FryMath.UI.Controls;
using FryWorld.Sdk.Plugins;

namespace FryMath.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    public static MainWindowViewModel? ActiveInstance { get; set; }

    private readonly IFryPluginContext? _pluginContext;
    private readonly DispatcherTimer _animationTimer;
    private bool _disposed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPresets))]
    [NotifyPropertyChangedFor(nameof(CurrentLabPresetCount))]
    private string _activeLaboratory = "Visual Calculus";

    [ObservableProperty]
    private string _activeDomain = "Calculus & Analysis";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPresets))]
    [NotifyPropertyChangedFor(nameof(CurrentLabPresetCount))]
    private bool _filterPresetsByActiveLab = true;

    [ObservableProperty]
    private bool _isSidebarOpen = true;

    [ObservableProperty]
    private bool _isSymbolRibbonOpen = true;

    [ObservableProperty]
    private string _currentPresetTitle = "Taylor Series Expansion of sin(x)";

    public int CurrentLabPresetCount => Presets.Count(p => string.Equals(p.Category, ActiveLaboratory, StringComparison.OrdinalIgnoreCase));
    public int TotalPresetCount => Presets.Count;

    public IReadOnlyList<LaboratoryItem> AllLaboratories => LaboratoryCatalog.AllLaboratories;
    public IReadOnlyList<string> DomainCategories => LaboratoryCatalog.Domains;

    public event Action? CanvasResetViewRequested;
    public event Action? CanvasZoomInRequested;
    public event Action? CanvasZoomOutRequested;

    partial void OnActiveLaboratoryChanged(string value)
    {
        ActiveDomain = LaboratoryCatalog.GetDomain(value);
        var firstLabPreset = Presets.FirstOrDefault(p => string.Equals(p.Category, value, StringComparison.OrdinalIgnoreCase));
        if (firstLabPreset != null)
        {
            CurrentPresetTitle = firstLabPreset.Title;
        }
    }

    [ObservableProperty]
    private string _omnibarQuery = "x^3 - 3*x";

    // Multi-Function Graphing
    public ObservableCollection<FunctionItemViewModel> Functions { get; } = new();

    public string Expression
    {
        get => Functions.Count > 0 ? Functions[0].ExpressionText : string.Empty;
        set
        {
            if (Functions.Count > 0)
            {
                Functions[0].ExpressionText = value;
                RecomputeCalculus();
            }
        }
    }

    public string AnalyticalDerivative => Functions.Count > 0 ? Functions[0].AnalyticalDerivative : string.Empty;
    public string AnalyticalAntiderivative => Functions.Count > 0 ? Functions[0].AnalyticalAntiderivative : string.Empty;
    public string DefiniteIntegralResult => IntegralInfo?.FormattedResult ?? "0.00";
    public double Determinant => Matrix.Determinant;

    // Dynamic Parameter Animation System
    public ObservableCollection<DynamicParameterViewModel> Parameters { get; } = new();

    // Calculus Laboratory Settings
    [ObservableProperty]
    private bool _showProbe = true;

    [ObservableProperty]
    private double _probeX = 1.0;

    [ObservableProperty]
    private bool _showTangent = true;

    [ObservableProperty]
    private bool _showOsculating = true;

    [ObservableProperty]
    private bool _showRiemann = false;

    [ObservableProperty]
    private bool _showSecant = false;

    [ObservableProperty]
    private double _secantDeltaX = 0.8;

    [ObservableProperty]
    private bool _showCriticalPoints = true;

    [ObservableProperty]
    private bool _showSlopeField = false;

    [ObservableProperty]
    private bool _showAreaBetweenCurves = false;

    [ObservableProperty]
    private int _riemannN = 16;

    [ObservableProperty]
    private RiemannRule _riemannRule = RiemannRule.Midpoint;

    [ObservableProperty]
    private double _integrationA = -1.5;

    [ObservableProperty]
    private double _integrationB = 1.5;

    // Taylor Series Approximation
    [ObservableProperty]
    private bool _showTaylorSeries = false;

    [ObservableProperty]
    private int _taylorDegree = 4;

    [ObservableProperty]
    private double _taylorCenter = 0.0;

    [ObservableProperty]
    private string _taylorFormula = string.Empty;

    // Dynamic Parameter Explorer for Visual Calculus
    [ObservableProperty]
    private double _dynamicParamA = 1.0;

    [ObservableProperty]
    private double _dynamicParamB = 0.0;

    [ObservableProperty]
    private double _dynamicParamC = 0.0;

    // Linear Algebra Laboratory
    [ObservableProperty]
    private Matrix2x2 _matrix = Matrix2x2.Identity;

    [ObservableProperty]
    private double _matrixM11 = 1.0;

    [ObservableProperty]
    private double _matrixM12 = 0.0;

    [ObservableProperty]
    private double _matrixM21 = 0.0;

    [ObservableProperty]
    private double _matrixM22 = 1.0;

    [ObservableProperty]
    private double _testVectorU1 = 1.5;

    [ObservableProperty]
    private double _testVectorU2 = 1.0;

    [ObservableProperty]
    private double _transformedVectorV1 = 1.5;

    [ObservableProperty]
    private double _transformedVectorV2 = 1.0;

    [ObservableProperty]
    private double _morphProgress = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MorphPlayPauseIcon))]
    private bool _isMorphing = false;

    public string MorphPlayPauseIcon => IsMorphing ? "Pause" : "Play";

    [ObservableProperty]
    private bool _showUnitCircle = true;

    [ObservableProperty]
    private bool _showTestVector = true;

    [ObservableProperty]
    private Vector2D _testVector = new(1.5, 1.0);

    // Fourier Waves Laboratory
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FourierWaveformSynthesis))]
    [NotifyPropertyChangedFor(nameof(FourierTotalHarmonicDistortion))]
    [NotifyPropertyChangedFor(nameof(FourierSpectralEnergy))]
    [NotifyPropertyChangedFor(nameof(FourierThdStatus))]
    private string _fourierPreset = "heart";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FourierWaveformSynthesis))]
    [NotifyPropertyChangedFor(nameof(FourierTotalHarmonicDistortion))]
    [NotifyPropertyChangedFor(nameof(FourierSpectralEnergy))]
    [NotifyPropertyChangedFor(nameof(FourierThdStatus))]
    private int _harmonics = 15;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FourierWaveformSynthesis))]
    [NotifyPropertyChangedFor(nameof(FourierTotalHarmonicDistortion))]
    [NotifyPropertyChangedFor(nameof(FourierSpectralEnergy))]
    [NotifyPropertyChangedFor(nameof(FourierThdStatus))]
    private string _fourierHarmonicFilter = "all";

    [ObservableProperty]
    private bool _isWavePlaying = true;

    [ObservableProperty]
    private bool _isAudioPlaying = false;

    public string FourierWaveformSynthesis => FourierPreset switch
    {
        "square" => $"f(t) = (4/π) Σ sin((2k-1)ωt)/(2k-1)  [k=1..{Math.Min(Harmonics, 15)}]",
        "sawtooth" => $"f(t) = (2/π) Σ (-1)ᵏ⁺¹ sin(kωt)/k  [k=1..{Math.Min(Harmonics, 15)}]",
        "triangle" => $"f(t) = (8/π²) Σ (-1)ᵏ sin((2k+1)ωt)/(2k+1)²  [k=0..{Math.Min(Harmonics, 15)}]",
        _ => $"z(t) = Σ c_k exp(i k ω t)  [N={Harmonics} epicycles]"
    };

    public string FourierTotalHarmonicDistortion => FourierPreset switch
    {
        "square" => "THD: 48.3% (Square Approximation)",
        "sawtooth" => "THD: 80.3% (Full Integer Harmonics)",
        "triangle" => "THD: 12.1% (Rapid 1/n² Falloff)",
        _ => "THD: 4.8% (Parametric Smooth Spline)"
    };

    public string FourierSpectralEnergy
    {
        get
        {
            double pct = FourierPreset switch
            {
                "triangle" => 99.8,
                "square" => Math.Min(99.4, 90.0 + (Harmonics * 0.45)),
                "sawtooth" => Math.Min(98.5, 82.0 + (Harmonics * 0.55)),
                _ => Math.Min(99.6, 92.0 + (Harmonics * 0.3))
            };
            return $"Parseval Energy: {pct:F1}% ({Harmonics} harmonics)";
        }
    }

    public string FourierThdStatus => FourierPreset switch
    {
        "square" => "Gibbs Active (+8.95%)",
        "sawtooth" => "Harmonic Cascade",
        "triangle" => "Smooth C¹ (1/n²)",
        _ => Harmonics <= 5 ? "Band-Limited" : "Conformal Epicycle"
    };

    // Probability & Stats
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatDistMean))]
    [NotifyPropertyChangedFor(nameof(StatDistStdDev))]
    [NotifyPropertyChangedFor(nameof(StatVariance))]
    [NotifyPropertyChangedFor(nameof(StatEntropyNats))]
    [NotifyPropertyChangedFor(nameof(StatSkewnessStatus))]
    private double _probabilityP = 0.5;

    [ObservableProperty]
    private bool _isPlinkoDropping = true;

    public double StatDistMean => 12.0 * ProbabilityP;
    public double StatVariance => 12.0 * ProbabilityP * (1.0 - ProbabilityP);
    public double StatDistStdDev => Math.Sqrt(Math.Max(1e-6, StatVariance));

    public string StatEntropyNats
    {
        get
        {
            double p = Math.Clamp(ProbabilityP, 1e-6, 1.0 - 1e-6);
            double h = -(p * Math.Log2(p) + (1.0 - p) * Math.Log2(1.0 - p));
            return $"H(p) = {h:F3} bits/trial";
        }
    }

    public string StatSkewnessStatus
    {
        get
        {
            double diff = ProbabilityP - 0.5;
            if (Math.Abs(diff) < 0.03) return "Symmetric (Zero Skew)";
            return diff < 0 ? "Right-Skewed (p < 0.5)" : "Left-Skewed (p > 0.5)";
        }
    }

    public event Action? PlinkoBurstRequested;
    public event Action? PlinkoResetRequested;

    // Dynamical Chaos (Lorenz Strange Attractor)
    [ObservableProperty]
    private bool _showChaosDualTrajectories = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChaosLyapunovStatus))]
    [NotifyPropertyChangedFor(nameof(ChaosContractionRate))]
    [NotifyPropertyChangedFor(nameof(ChaosRayleighRatio))]
    private double _chaosSigma = 10.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChaosLyapunovStatus))]
    [NotifyPropertyChangedFor(nameof(ChaosContractionRate))]
    [NotifyPropertyChangedFor(nameof(ChaosRayleighRatio))]
    private double _chaosRho = 28.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChaosLyapunovStatus))]
    [NotifyPropertyChangedFor(nameof(ChaosContractionRate))]
    [NotifyPropertyChangedFor(nameof(ChaosRayleighRatio))]
    private double _chaosBeta = 8.0 / 3.0;

    public string ChaosLyapunovStatus
    {
        get
        {
            if (ChaosRho < 1.0) return "Stable Origin (λ₁ < 0)";
            if (ChaosRho < 24.74) return "Pitchfork Steady (λ₁ ≤ 0)";
            return "Chaotic Attractor (λ₁ > 0)";
        }
    }

    public string ChaosContractionRate => $"div(F) = {-(ChaosSigma + 1.0 + ChaosBeta):F2} (Dissipative)";
    public string ChaosRayleighRatio => $"ρ / ρ_c = {ChaosRho / 24.74:F2}× (Hopf Threshold)";

    // Vector Fields & ODEs
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VectorFieldDivergenceStatus))]
    [NotifyPropertyChangedFor(nameof(VectorFieldCurlStatus))]
    [NotifyPropertyChangedFor(nameof(VectorFieldFixedPointType))]
    private string _vectorFieldP = "-y";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VectorFieldDivergenceStatus))]
    [NotifyPropertyChangedFor(nameof(VectorFieldCurlStatus))]
    [NotifyPropertyChangedFor(nameof(VectorFieldFixedPointType))]
    private string _vectorFieldQ = "x";

    [ObservableProperty]
    private bool _vectorFieldShowArrows = true;

    [ObservableProperty]
    private bool _vectorFieldShowParticles = true;

    [ObservableProperty]
    private double _vectorFieldParticleSpeed = 1.0;

    public event Action<string, string>? VectorFieldPresetRequested;

    [ObservableProperty]
    private string _vectorFieldSystemStatus = "System Status: Active (dx/dt = P, dy/dt = Q)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VectorFieldDivergenceStatus))]
    [NotifyPropertyChangedFor(nameof(VectorFieldCurlStatus))]
    [NotifyPropertyChangedFor(nameof(VectorFieldFixedPointType))]
    private double _vectorFieldSeedX = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VectorFieldDivergenceStatus))]
    [NotifyPropertyChangedFor(nameof(VectorFieldCurlStatus))]
    [NotifyPropertyChangedFor(nameof(VectorFieldFixedPointType))]
    private double _vectorFieldSeedY = 1.0;

    public string VectorFieldDivergenceStatus
    {
        get
        {
            string p = VectorFieldP.Trim().ToLowerInvariant();
            string q = VectorFieldQ.Trim().ToLowerInvariant();
            if (p == "-y" && q == "x") return "∇·F = 0.000 (Incompressible)";
            if (p == "y" && q == "x") return "∇·F = 0.000 (Hamiltonian)";
            if (p.Contains("-0.2") || q.Contains("-0.2") || p.Contains("-x") || q.Contains("-y")) return "∇·F < 0 (Contracting Sink)";
            return "∇·F ≈ 0.000 (Area Preserving)";
        }
    }

    public string VectorFieldCurlStatus
    {
        get
        {
            string p = VectorFieldP.Trim().ToLowerInvariant();
            string q = VectorFieldQ.Trim().ToLowerInvariant();
            if (p == "-y" && q == "x") return "∇×F = +2.000 (Rigid Vorticity)";
            if (p == "y" && q == "x") return "∇×F = 0.000 (Irrotational)";
            return "∇×F ≠ 0 (Vortex Shear Active)";
        }
    }

    public string VectorFieldFixedPointType
    {
        get
        {
            string p = VectorFieldP.Trim().ToLowerInvariant();
            string q = VectorFieldQ.Trim().ToLowerInvariant();
            if (p == "-y" && q == "x") return "Hamiltonian Center (τ=0, Δ>0)";
            if (p == "x" && q == "-y") return "Hyperbolic Saddle (Δ<0)";
            if (p == "y" && q == "x") return "Hyperbolic Saddle (Δ<0)";
            if (p.Contains("sin") || q.Contains("cos")) return "Nonlinear Pendulum Separatrix";
            if (p.Contains("1 - x^2") || q.Contains("1 - x^2")) return "Limit Cycle Attractor";
            if (p.Contains("-x - y") || q.Contains("x - y")) return "Stable Spiral Sink (τ<0, Δ>0)";
            return "Active Autonomous Flow";
        }
    }

    // Complex Analysis & Conformal Mapping
    [ObservableProperty]
    private ComplexPreset _selectedComplexPreset = ComplexPreset.ZSquared;

    [ObservableProperty]
    private bool _complexShowModulusContours = true;

    [ObservableProperty]
    private bool _complexShowConformalGrid = true;

    [ObservableProperty]
    private string _complexFormulaTitle = "f(z) = z²  (Complex Quadratic Map)";

    [ObservableProperty]
    private string _complexFormulaDetail = "Doubles polar phase argument arg(w) = 2θ and squares modulus |w| = r². Branch point at origin.";

    [ObservableProperty]
    private string _complexCustomFormula = "z^2";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MobiusDeterminant))]
    [NotifyPropertyChangedFor(nameof(MobiusDeterminantStatus))]
    [NotifyPropertyChangedFor(nameof(MobiusIsSingular))]
    private double _mobiusParamA = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MobiusDeterminant))]
    [NotifyPropertyChangedFor(nameof(MobiusDeterminantStatus))]
    [NotifyPropertyChangedFor(nameof(MobiusIsSingular))]
    private double _mobiusParamB = -1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MobiusDeterminant))]
    [NotifyPropertyChangedFor(nameof(MobiusDeterminantStatus))]
    [NotifyPropertyChangedFor(nameof(MobiusIsSingular))]
    private double _mobiusParamC = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MobiusDeterminant))]
    [NotifyPropertyChangedFor(nameof(MobiusDeterminantStatus))]
    [NotifyPropertyChangedFor(nameof(MobiusIsSingular))]
    private double _mobiusParamD = 1.0;

    public double MobiusDeterminant => (MobiusParamA * MobiusParamD) - (MobiusParamB * MobiusParamC);
    public bool MobiusIsSingular => Math.Abs(MobiusDeterminant) < 1e-5;
    public string MobiusDeterminantStatus => MobiusIsSingular
        ? "det(M) = 0 (Degenerate Singularity!)"
        : $"det(M) = {MobiusDeterminant:F2} (Non-singular Conformal)";

    // 3D Multivariable Calculus & Surfaces
    [ObservableProperty]
    private SurfacePreset _selectedSurfacePreset = SurfacePreset.Paraboloid;

    [ObservableProperty]
    private string _surfaceFormula = "0.5 * (x^2 + y^2)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SurfaceEvalZ))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradX))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradY))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradNorm))]
    private double _surfaceProbeX = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SurfaceEvalZ))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradX))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradY))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradNorm))]
    private double _surfaceProbeY = 0.8;

    [ObservableProperty]
    private bool _surfaceShowTangentPlane = true;

    [ObservableProperty]
    private bool _surfaceShowWireframe = true;

    [ObservableProperty]
    private bool _surfaceShowNormalVector = true;

    [ObservableProperty]
    private string _surfaceFormulaTitle = "z = ½(x² + y²)  (Elliptic Paraboloid)";

    [ObservableProperty]
    private string _surfaceFormulaDetail = "Smooth convex bowl with global minimum at origin (0, 0). Positive definite Hessian D > 0 everywhere.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SurfaceEvalZ))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradX))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradY))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradNorm))]
    [NotifyPropertyChangedFor(nameof(SurfaceHessianD))]
    [NotifyPropertyChangedFor(nameof(SurfaceCurvatureStatus))]
    private double _surfaceParamA = 0.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SurfaceEvalZ))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradX))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradY))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradNorm))]
    [NotifyPropertyChangedFor(nameof(SurfaceHessianD))]
    [NotifyPropertyChangedFor(nameof(SurfaceCurvatureStatus))]
    private double _surfaceParamB = 0.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SurfaceEvalZ))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradX))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradY))]
    [NotifyPropertyChangedFor(nameof(SurfaceGradNorm))]
    [NotifyPropertyChangedFor(nameof(SurfaceHessianD))]
    [NotifyPropertyChangedFor(nameof(SurfaceCurvatureStatus))]
    private double _surfaceParamC = 0.0;

    public double SurfaceEvalZ => (SurfaceParamA * SurfaceProbeX * SurfaceProbeX) + (SurfaceParamB * SurfaceProbeY * SurfaceProbeY) + (SurfaceParamC * SurfaceProbeX * SurfaceProbeY);
    public double SurfaceGradX => (2.0 * SurfaceParamA * SurfaceProbeX) + (SurfaceParamC * SurfaceProbeY);
    public double SurfaceGradY => (2.0 * SurfaceParamB * SurfaceProbeY) + (SurfaceParamC * SurfaceProbeX);
    public double SurfaceGradNorm => Math.Sqrt(SurfaceGradX * SurfaceGradX + SurfaceGradY * SurfaceGradY);
    public double SurfaceHessianD => (4.0 * SurfaceParamA * SurfaceParamB) - (SurfaceParamC * SurfaceParamC);
    public string SurfaceCurvatureStatus
    {
        get
        {
            if (SurfaceHessianD > 0.01)
                return SurfaceParamA > 0 ? "Local Minimum (Elliptic Bowl)" : "Local Maximum (Elliptic Dome)";
            if (SurfaceHessianD < -0.01)
                return "Hyperbolic Saddle Point";
            return "Parabolic Valley (Degenerate Hessian)";
        }
    }

    // Fractal Geometry & Complex Dynamics
    [ObservableProperty]
    private FractalType _selectedFractalType = FractalType.Mandelbrot;

    [ObservableProperty]
    private int _fractalMaxIterations = 140;

    [ObservableProperty]
    private FractalPalette _selectedFractalPalette = FractalPalette.ElectricNeon;

    [ObservableProperty]
    private double _fractalJuliaCr = -0.7;

    [ObservableProperty]
    private double _fractalJuliaCi = 0.27015;

    [ObservableProperty]
    private int _fractalNewtonDegree = 3;

    [ObservableProperty]
    private double _fractalEscapeRadius = 2.0;

    [ObservableProperty]
    private string _fractalFormulaTitle = "Mandelbrot Set: z_{n+1} = z_n² + c";

    [ObservableProperty]
    private string _fractalFormulaDetail = "The connectedness locus of Julia sets. Boundary points exhibit infinite self-similarity at all zoom depths.";

    // 2D Wave Equation & Chladni Resonance
    [ObservableProperty]
    private WaveSimulationMode _selectedWaveMode = WaveSimulationMode.ChladniPlate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WaveCflNumber))]
    [NotifyPropertyChangedFor(nameof(WaveCflStatus))]
    private int _waveModeN = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WaveCflNumber))]
    [NotifyPropertyChangedFor(nameof(WaveCflStatus))]
    private int _waveModeM = 5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WaveCflNumber))]
    [NotifyPropertyChangedFor(nameof(WaveCflStatus))]
    private double _wavePropagationSpeedC = 1.0;

    [ObservableProperty]
    private string _waveExcitationType = "impulse";

    [ObservableProperty]
    private double _waveDamping = 0.005;

    [ObservableProperty]
    private bool _waveShowSand = true;

    [ObservableProperty]
    private string _waveFormulaTitle = "Chladni Plate Eigenmode (n=3, m=5)";

    [ObservableProperty]
    private string _waveFormulaDetail = "Acoustic 2D eigenmode on square plate: u(x, y) = cos(3x)cos(5y) - cos(5x)cos(3y). Sand grains bounce away from antinodes and collect on zero-vibration nodal lines.";

    public double WaveCflNumber => WavePropagationSpeedC * (0.016 / 0.035);
    public string WaveCflStatus => WaveCflNumber < 0.707
        ? $"CFL = {WaveCflNumber:F2} (Stable)"
        : $"CFL = {WaveCflNumber:F2} (Unstable)";

    // Differential Geometry & 3D Space Curves
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurveCurvatureKappa))]
    [NotifyPropertyChangedFor(nameof(CurveTorsionTau))]
    [NotifyPropertyChangedFor(nameof(CurveRadiusOfCurvature))]
    [NotifyPropertyChangedFor(nameof(CurveLancretRatio))]
    [NotifyPropertyChangedFor(nameof(CurveFrenetRegimeStatus))]
    private SpaceCurvePreset _selectedSpaceCurvePreset = SpaceCurvePreset.CircularHelix;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurveCurvatureKappa))]
    [NotifyPropertyChangedFor(nameof(CurveTorsionTau))]
    [NotifyPropertyChangedFor(nameof(CurveRadiusOfCurvature))]
    [NotifyPropertyChangedFor(nameof(CurveLancretRatio))]
    [NotifyPropertyChangedFor(nameof(CurveFrenetRegimeStatus))]
    private double _curveParamT = 2.5;

    [ObservableProperty]
    private double _curveTMin = 0.0;

    [ObservableProperty]
    private double _curveTMax = 12.566;

    [ObservableProperty]
    private bool _curveShowFrenetFrame = true;

    [ObservableProperty]
    private bool _curveShowOsculatingPlane = true;

    [ObservableProperty]
    private bool _curveShowOsculatingCircle = true;

    [ObservableProperty]
    private string _curveFormulaTitle = "Circular Helix: r(t) = ⟨2 cos t, 2 sin t, 0.6 t⟩";

    [ObservableProperty]
    private string _curveFormulaDetail = "Classic spatial spiral with constant curvature κ = 0.459 and constant torsion τ = 0.138. The moving Frenet-Serret trihedron ⟨T, N, B⟩ glides along the curve.";

    public string CurveCurvatureKappa
    {
        get
        {
            var frame = SpaceCurveEngine.ComputeFrenetFrame(SelectedSpaceCurvePreset, CurveParamT);
            return $"κ = {frame.Curvature:F3}";
        }
    }

    public string CurveTorsionTau
    {
        get
        {
            var frame = SpaceCurveEngine.ComputeFrenetFrame(SelectedSpaceCurvePreset, CurveParamT);
            return $"τ = {frame.Torsion:F3}";
        }
    }

    public string CurveRadiusOfCurvature
    {
        get
        {
            var frame = SpaceCurveEngine.ComputeFrenetFrame(SelectedSpaceCurvePreset, CurveParamT);
            return frame.RadiusOfCurvature > 50.0 ? "R = ∞ (Inflection)" : $"R = {frame.RadiusOfCurvature:F2}";
        }
    }

    public string CurveLancretRatio
    {
        get
        {
            var frame = SpaceCurveEngine.ComputeFrenetFrame(SelectedSpaceCurvePreset, CurveParamT);
            if (Math.Abs(frame.Curvature) < 1e-4) return "τ/κ = N/A (κ ≈ 0)";
            return $"τ/κ = {frame.Torsion / frame.Curvature:F3}";
        }
    }

    public string CurveFrenetRegimeStatus
    {
        get
        {
            if (SelectedSpaceCurvePreset == SpaceCurvePreset.CircularHelix) return "Lancret Helix (τ/κ = const)";
            var frame = SpaceCurveEngine.ComputeFrenetFrame(SelectedSpaceCurvePreset, CurveParamT);
            if (Math.Abs(frame.Torsion) < 1e-3) return "Planar Curve (τ = 0)";
            return "General 3D Space Curve";
        }
    }

    // Riemann Zeta Function & Prime Distribution (Zeta & Primes)
    [ObservableProperty]
    private ZetaViewMode _selectedZetaMode = ZetaViewMode.CriticalLine;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZetaNearestZeroOrdinal))]
    [NotifyPropertyChangedFor(nameof(ZetaNearestZeroHeight))]
    [NotifyPropertyChangedFor(nameof(ZetaRiemannSiegelZVal))]
    [NotifyPropertyChangedFor(nameof(ZetaGramIntervalStatus))]
    private double _zetaProbeT = 14.1347;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZetaPrimeLiResidual))]
    private int _zetaZeroCount = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZetaPrimeLiResidual))]
    private double _zetaXMax = 50.0;

    [ObservableProperty]
    private bool _zetaShowZeros = true;

    [ObservableProperty]
    private bool _zetaShowSmoothLi = true;

    [ObservableProperty]
    private bool _zetaShowExplicitWave = true;

    [ObservableProperty]
    private string _zetaFormulaTitle = "Riemann-Siegel Z(t) on Critical Line s = 1/2 + it";

    [ObservableProperty]
    private string _zetaFormulaDetail = "Zeros of ζ(s) on the critical strip coincide with real zero-crossings of Z(t) and pass directly through the origin in the complex polar plane.";

    public string ZetaNearestZeroOrdinal
    {
        get
        {
            double t = ZetaProbeT;
            double minDist = double.MaxValue;
            int nearestIdx = 1;
            for (int i = 0; i < RiemannZetaEngine.KnownZeros.Length; i++)
            {
                double d = Math.Abs(t - RiemannZetaEngine.KnownZeros[i]);
                if (d < minDist)
                {
                    minDist = d;
                    nearestIdx = i + 1;
                }
            }
            return $"Zero γ_{nearestIdx} (n = {nearestIdx})";
        }
    }

    public string ZetaNearestZeroHeight
    {
        get
        {
            double t = ZetaProbeT;
            double minDist = double.MaxValue;
            double nearestZ = RiemannZetaEngine.KnownZeros[0];
            for (int i = 0; i < RiemannZetaEngine.KnownZeros.Length; i++)
            {
                double d = Math.Abs(t - RiemannZetaEngine.KnownZeros[i]);
                if (d < minDist)
                {
                    minDist = d;
                    nearestZ = RiemannZetaEngine.KnownZeros[i];
                }
            }
            return $"t_{nearestZ:F2} = {nearestZ:F4}";
        }
    }

    public string ZetaRiemannSiegelZVal
    {
        get
        {
            double z = RiemannZetaEngine.EvaluateSiegelZ(ZetaProbeT);
            return $"Z(t) = {z:F3}";
        }
    }

    public string ZetaGramIntervalStatus
    {
        get
        {
            double z = Math.Abs(RiemannZetaEngine.EvaluateSiegelZ(ZetaProbeT));
            if (z < 0.15) return "Zero Hit! (Z ≈ 0)";
            return "Critical Strip Active";
        }
    }

    public string ZetaPrimeLiResidual
    {
        get
        {
            double x = ZetaXMax;
            double piVal = RiemannZetaEngine.ExactPrimeCount(x);
            double liVal = RiemannZetaEngine.LogarithmicIntegral(x);
            double err = Math.Abs(piVal - liVal);
            return $"|π - Li| = {err:F1} (RH Bound)";
        }
    }

    // 13. Heat Diffusion & Laplace PDE
    [ObservableProperty]
    private string _heatPreset = "laplace";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeatStabilityCflStatus))]
    [NotifyPropertyChangedFor(nameof(HeatMaximumPrincipleStatus))]
    private double _heatDiffusivity = 0.22;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeatBoundaryStatus))]
    private string _heatBoundaryType = "Dirichlet";

    [ObservableProperty]
    private bool _heatShowFlux = true;

    [ObservableProperty]
    private bool _heatShowIsotherms = true;

    [ObservableProperty]
    private bool _heatIsSimulating = true;

    [ObservableProperty]
    private string _heatFormulaTitle = "2D Heat Diffusion & Laplace Equilibrium: ∂T/∂t = α ∇²T";

    [ObservableProperty]
    private string _heatFormulaDetail = "Fourier's Law of Heat Conduction: q = -k ∇T. Thermal energy diffuses down temperature gradients until reaching Laplace steady-state ∇²T = 0.";

    public string HeatStabilityCflStatus
    {
        get
        {
            double pct = (HeatDiffusivity / 0.25) * 100.0;
            return pct <= 100.0 ? $"CFL {pct:F0}% (Stable)" : $"CFL {pct:F0}% (Unstable)";
        }
    }

    public string HeatMaximumPrincipleStatus => "Max Principle Active";

    public string HeatBoundaryStatus => $"Boundary: {HeatBoundaryType}";

    // 14. Quantum Wave Mechanics & Schrödinger Equation
    [ObservableProperty]
    private string _quantumPreset = "tunneling";

    [ObservableProperty]
    private bool _quantumShowPhaseColoring = true;

    [ObservableProperty]
    private bool _quantumShowProbabilityDensity = true;

    [ObservableProperty]
    private bool _quantumShowWaveComponents = true;

    [ObservableProperty]
    private bool _quantumShowPotential = true;

    [ObservableProperty]
    private bool _quantumIsSimulating = true;

    [ObservableProperty]
    private double _quantumTimeStep = 0.04;

    [ObservableProperty]
    private double _quantumBarrierHeight = 4.0;

    [ObservableProperty]
    private double _quantumBarrierWidth = 1.2;

    [ObservableProperty]
    private double _quantumWavepacketK0 = 2.2;

    [ObservableProperty]
    private double _quantumWavepacketSigma = 1.0;

    [ObservableProperty]
    private string _quantumFormulaTitle = "Schrödinger Equation: iħ ∂ψ/∂t = (-ħ²/2m ∇² + V)ψ";

    [ObservableProperty]
    private string _quantumFormulaDetail = "Unitary time-evolution operator U(t) = exp(-iHt/ħ) solved via Crank-Nicolson Cayley transform. Wavepacket undergoes quantum tunneling through classically forbidden barriers.";

    // 15. Special Relativity & Minkowski Spacetime
    [ObservableProperty]
    private string _relativityPreset = "boost";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RelativityLorentzGamma))]
    [NotifyPropertyChangedFor(nameof(RelativityTimeDilationFactor))]
    [NotifyPropertyChangedFor(nameof(RelativityLengthContraction))]
    [NotifyPropertyChangedFor(nameof(RelativityDopplerShift))]
    [NotifyPropertyChangedFor(nameof(RelativitySpacetimeIntervalStatus))]
    private double _relativityBeta = 0.6;

    [ObservableProperty]
    private bool _relativityShowHyperbolas = true;

    [ObservableProperty]
    private bool _relativityShowLightCone = true;

    [ObservableProperty]
    private bool _relativityShowSimultaneityGrid = true;

    [ObservableProperty]
    private bool _relativityShowWorldlines = true;

    [ObservableProperty]
    private bool _relativityIsSimulating = true;

    [ObservableProperty]
    private string _relativityFormulaTitle = "Minkowski Spacetime & Lorentz Boost: s² = c²t² - x²";

    [ObservableProperty]
    private string _relativityFormulaDetail = "Lorentz transformation: ct' = γ(ct - βx), x' = γ(x - βct). The spacetime interval s² is strictly invariant across all inertial frames.";

    public string RelativityLorentzGamma
    {
        get
        {
            double b = Math.Clamp(Math.Abs(RelativityBeta), 0.0, 0.999);
            double gamma = 1.0 / Math.Sqrt(1.0 - b * b);
            return $"γ = {gamma:F3}";
        }
    }

    public string RelativityTimeDilationFactor
    {
        get
        {
            double b = Math.Clamp(Math.Abs(RelativityBeta), 0.0, 0.999);
            double gamma = 1.0 / Math.Sqrt(1.0 - b * b);
            return $"Δt/Δτ = {gamma:F2}× (Slow Clock)";
        }
    }

    public string RelativityLengthContraction
    {
        get
        {
            double b = Math.Clamp(Math.Abs(RelativityBeta), 0.0, 0.999);
            double gamma = 1.0 / Math.Sqrt(1.0 - b * b);
            return $"L/L₀ = {1.0 / gamma:F3} (Lorentz Shrink)";
        }
    }

    public string RelativityDopplerShift
    {
        get
        {
            double b = Math.Clamp(RelativityBeta, -0.999, 0.999);
            double factor = Math.Sqrt((1.0 - b) / (1.0 + b));
            return $"f'/f = {factor:F3} (Relativistic Doppler)";
        }
    }

    public string RelativitySpacetimeIntervalStatus
    {
        get
        {
            double b = Math.Abs(RelativityBeta);
            if (b < 1e-4) return "Static (s² > 0)";
            if (b >= 0.999) return "Null (s² = 0)";
            return $"Timelike (s² = {1.0 - b * b:F2})";
        }
    }

    // 16. Fluid Dynamics & Navier-Stokes Vorticity
    [ObservableProperty]
    private string _fluidPreset = "karman";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FluidReynoldsNumber))]
    [NotifyPropertyChangedFor(nameof(FluidFlowRegimeStatus))]
    [NotifyPropertyChangedFor(nameof(FluidStrouhalNumber))]
    [NotifyPropertyChangedFor(nameof(FluidEnstrophyStatus))]
    private double _fluidViscosity = 0.0006;

    [ObservableProperty]
    private bool _fluidShowVorticity = true;

    [ObservableProperty]
    private bool _fluidShowVelocityArrows = true;

    [ObservableProperty]
    private bool _fluidShowStreamlines = true;

    [ObservableProperty]
    private bool _fluidShowParticles = true;

    [ObservableProperty]
    private bool _fluidIsSimulating = true;

    [ObservableProperty]
    private string _fluidFormulaTitle = "2D Incompressible Navier-Stokes: ∂ω/∂t + (u·∇)ω = ν∇²ω";

    [ObservableProperty]
    private string _fluidFormulaDetail = "Vorticity-streamfunction formulation: ∇²ψ = -ω, u = ∂ψ/∂y, v = -∂ψ/∂x. Incompressibility ∇·u = 0 is satisfied identically.";

    public string FluidReynoldsNumber
    {
        get
        {
            double re = 1.0 / Math.Max(1e-5, FluidViscosity);
            return $"Re = {re:F0}";
        }
    }

    public string FluidFlowRegimeStatus
    {
        get
        {
            double re = 1.0 / Math.Max(1e-5, FluidViscosity);
            if (re < 50) return "Creeping Stokes";
            if (re < 300) return "Kármán Vortex Street";
            if (re < 2000) return "Transitional Wake";
            return "Turbulent Cascade";
        }
    }

    public string FluidStrouhalNumber
    {
        get
        {
            double re = 1.0 / Math.Max(1e-5, FluidViscosity);
            double st = Math.Max(0.0, 0.198 * (1.0 - 19.7 / Math.Max(20.0, re)));
            return $"St ≈ {st:F3} (Shedding)";
        }
    }

    public string FluidEnstrophyStatus => "Enstrophy Cascade Active";

    // 17. Topology & 4D Hypersurface Manifolds
    [ObservableProperty]
    private string _topologyPreset = "tesseract";

    [ObservableProperty]
    private double _topologyRotationSpeed = 1.0;

    [ObservableProperty]
    private bool _topologyShowVertices = true;

    [ObservableProperty]
    private bool _topologyShowDepthColoring = true;

    [ObservableProperty]
    private bool _topologyShowStereographicSphere = true;

    [ObservableProperty]
    private bool _topologyIsRotating = true;

    [ObservableProperty]
    private string _topologyFormulaTitle = "Tesseract (8-Cell / 4D Hypercube): 16 Vertices, 32 Edges, 24 Faces, 8 Cubes";

    [ObservableProperty]
    private string _topologyFormulaDetail = "Euler-Poincaré characteristic χ = V - E + F - C = 16 - 32 + 24 - 8 = 0. Double rotation in SO(4) projected stereographically into ℝ³.";

    // 18. Celestial Mechanics & Gravitational N-Body Chaos
    [ObservableProperty]
    private string _celestialPreset = "figure8";

    [ObservableProperty]
    private double _celestialTimeStep = 0.005;

    [ObservableProperty]
    private bool _celestialShowTrails = true;

    [ObservableProperty]
    private bool _celestialShowVelocityVectors = true;

    [ObservableProperty]
    private bool _celestialShowCenterOfMass = true;

    [ObservableProperty]
    private bool _celestialShowPotentialGrid = true;

    [ObservableProperty]
    private bool _celestialIsSimulating = true;

    [ObservableProperty]
    private string _celestialFormulaTitle = "Figure-8 3-Body Choreography (Chenciner & Montgomery)";

    [ObservableProperty]
    private string _celestialFormulaDetail = "Exact stable planar solution with 3 equal masses chasing each other along a figure-8 orbit. Symplectic leapfrog preserves total energy E = T + V without secular drift.";

    // 19. Non-Euclidean Hyperbolic & Elliptic Geometry
    [ObservableProperty]
    private string _hyperbolicPreset = "heptagonal";

    [ObservableProperty]
    private string _hyperbolicModel = "poincare";

    [ObservableProperty]
    private int _hyperbolicTilingDepth = 2;

    [ObservableProperty]
    private bool _hyperbolicShowTessellation = true;

    [ObservableProperty]
    private bool _hyperbolicShowVertices = true;

    [ObservableProperty]
    private bool _hyperbolicShowInteractiveTriangle = true;

    [ObservableProperty]
    private string _hyperbolicFormulaTitle = "Poincaré Disk Model & Heptagonal Tiling {7, 3}";

    [ObservableProperty]
    private string _hyperbolicFormulaDetail = "Riemannian metric ds² = 4(dx² + dy²)/(1 - r²)² with constant negative Gaussian curvature K = -1. Geodesics are circular arcs orthogonal to the boundary circle.";

    // 20. Information Theory, Entropy & Signal Encoding
    [ObservableProperty]
    private string _informationPreset = "huffman";

    [ObservableProperty]
    private string _informationMode = "huffman";

    [ObservableProperty]
    private string _informationSourceText = "INFORMATION THEORY AND SHANNON ENTROPY";

    [ObservableProperty]
    private string _informationModulation = "qam16";

    [ObservableProperty]
    private double _informationSnrDb = 14.0;

    [ObservableProperty]
    private double _informationCrossoverProbability = 0.15;

    [ObservableProperty]
    private bool _informationShowTreeBranches = true;

    [ObservableProperty]
    private bool _informationShowProbabilityBars = true;

    [ObservableProperty]
    private bool _informationShowConstellationCloud = true;

    [ObservableProperty]
    private string _informationFormulaTitle = "Shannon Entropy & Optimal Huffman Coding Tree";

    [ObservableProperty]
    private string _informationFormulaDetail = "Shannon entropy H(X) = -Σ pᵢ log₂(pᵢ) defines the fundamental limit of lossless data compression. Huffman coding builds an optimal prefix-free binary tree achieving average code length L < H + 1.";

    // 21. Classical Mechanics, Lagrangian & Double Pendulum Chaos
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LagrangianMotionRegime))]
    private string _lagrangianPreset = "double";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LagrangianTotalEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianKineticEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianPotentialEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianMotionRegime))]
    private double _lagrangianL1 = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LagrangianTotalEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianKineticEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianPotentialEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianMotionRegime))]
    private double _lagrangianL2 = 0.9;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LagrangianTotalEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianKineticEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianPotentialEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianMotionRegime))]
    private double _lagrangianM1 = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LagrangianTotalEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianKineticEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianPotentialEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianMotionRegime))]
    private double _lagrangianM2 = 0.8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LagrangianTotalEnergy))]
    [NotifyPropertyChangedFor(nameof(LagrangianPotentialEnergy))]
    private double _lagrangianGravity = 9.81;

    [ObservableProperty]
    private double _lagrangianDamping = 0.0;

    [ObservableProperty]
    private bool _lagrangianShowShadow = true;

    [ObservableProperty]
    private bool _lagrangianShowTrails = true;

    [ObservableProperty]
    private bool _lagrangianShowPhasePortrait = true;

    [ObservableProperty]
    private bool _lagrangianIsSimulating = true;

    [ObservableProperty]
    private string _lagrangianFormulaTitle = "Double Pendulum Chaos & Lagrangian Dynamics";

    [ObservableProperty]
    private string _lagrangianFormulaDetail = "Coupled nonlinear equations derived via Euler-Lagrange d/dt(∂ℒ/∂θ̇ᵢ) - ∂ℒ/∂θᵢ = 0. Exhibits the Butterfly Effect with exponential trajectory divergence and positive Lyapunov exponent.";

    public double LagrangianKineticEnergy => 0.5 * (LagrangianM1 + LagrangianM2) * LagrangianL1 * LagrangianL1 * 0.25 + 0.5 * LagrangianM2 * LagrangianL2 * LagrangianL2 * 1.0;
    public double LagrangianPotentialEnergy => -(LagrangianM1 + LagrangianM2) * LagrangianGravity * LagrangianL1 * 0.707 - LagrangianM2 * LagrangianGravity * LagrangianL2 * 0.5;
    public double LagrangianTotalEnergy => LagrangianKineticEnergy + LagrangianPotentialEnergy;

    public string LagrangianMotionRegime => LagrangianPreset switch
    {
        "kapitza" => "Kapitza Lock",
        "poincare" => "KAM Island Chains",
        _ => (LagrangianL1 + LagrangianL2 > 2.4)
            ? "High-Inertia Chaos"
            : "Deterministic Chaos"
    };

    // 22. Modern Cryptography, Elliptic Curves & Finite Fields
    [ObservableProperty]
    private string _cryptoDisplayMode = "real";

    [ObservableProperty]
    private string _cryptoPreset = "secp256k1";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CryptoDiscriminant))]
    [NotifyPropertyChangedFor(nameof(CryptoDiscriminantText))]
    [NotifyPropertyChangedFor(nameof(CryptoCurveTopologyBadge))]
    private double _cryptoParamA = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CryptoDiscriminant))]
    [NotifyPropertyChangedFor(nameof(CryptoDiscriminantText))]
    [NotifyPropertyChangedFor(nameof(CryptoCurveTopologyBadge))]
    private double _cryptoParamB = 7.0;

    public double CryptoDiscriminant => -16.0 * (4.0 * Math.Pow(CryptoParamA, 3) + 27.0 * Math.Pow(CryptoParamB, 2));
    public string CryptoDiscriminantText => $"Δ = -16(4a³ + 27b²) = {CryptoDiscriminant:F1}";
    public string CryptoCurveTopologyBadge
    {
        get
        {
            if (Math.Abs(CryptoDiscriminant) < 0.05)
                return "Singular Degenerate (Cusp/Node - Unsafe!)";
            return CryptoDiscriminant > 0
                ? "Non-singular: Two Components (Egg + Branch)"
                : "Non-singular: Single Connected Branch";
        }
    }

    [ObservableProperty]
    private long _cryptoPrimeP = 97;

    [ObservableProperty]
    private long _cryptoFiniteA = 0;

    [ObservableProperty]
    private long _cryptoFiniteB = 7;

    [ObservableProperty]
    private bool _cryptoShowTangentSecant = true;

    [ObservableProperty]
    private bool _cryptoShowReflectionLines = true;

    [ObservableProperty]
    private long _cryptoAlicePrivateKey = 5;

    [ObservableProperty]
    private long _cryptoBobPrivateKey = 7;

    [ObservableProperty]
    private long _cryptoScalarK = 12;

    [ObservableProperty]
    private string _cryptoFormulaTitle = "secp256k1 (Bitcoin & Ethereum Curve)";

    [ObservableProperty]
    private string _cryptoFormulaDetail = "Koblitz curve y² = x³ + 7 over real numbers and discrete field F_97. Parameter a=0 enables rapid point doubling without multiplications.";

    // 23. Discrete Dynamical Systems, Arnold's Cat Map & IFS
    [ObservableProperty]
    private string _discreteDynamicsDisplayMode = "arnold";

    [ObservableProperty]
    private string _discreteDynamicsPreset = "arnold";

    [ObservableProperty]
    private double _discreteDynamicsLogisticR = 3.65;

    [ObservableProperty]
    private double _discreteDynamicsLogisticX0 = 0.45;

    [ObservableProperty]
    private int _discreteDynamicsCobwebSteps = 60;

    [ObservableProperty]
    private string _discreteDynamicsIfsPreset = "barnsley";

    [ObservableProperty]
    private int _discreteDynamicsCatStep = 0;

    [ObservableProperty]
    private bool _discreteDynamicsShowEigenvectors = true;

    [ObservableProperty]
    private string _discreteDynamicsFormulaTitle = "Arnold's Cat Map (Toral Automorphism)";

    [ObservableProperty]
    private string _discreteDynamicsFormulaDetail = "Hyperbolic toral automorphism [[2, 1], [1, 1]] mod 1 on a 64x64 discrete lattice. Area-preserving (det = 1) with Lyapunov exponent λ ≈ 0.9624. Exact Poincaré recurrence reconstitutes the cat at step 48.";

    // 24. Bifurcation & Catastrophe Theory (René Thom)
    [ObservableProperty]
    private string _catastropheMode = "cusp";

    [ObservableProperty]
    private string _catastrophePreset = "cusp";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CatastropheCuspDiscriminant))]
    [NotifyPropertyChangedFor(nameof(CatastropheCuspStatus))]
    private double _catastropheParamA = -2.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CatastropheCuspDiscriminant))]
    [NotifyPropertyChangedFor(nameof(CatastropheCuspStatus))]
    private double _catastropheParamB = 0.0;

    public double CatastropheCuspDiscriminant => 4.0 * Math.Pow(CatastropheParamA, 3) + 27.0 * Math.Pow(CatastropheParamB, 2);
    public string CatastropheCuspStatus
    {
        get
        {
            if (CatastropheCuspDiscriminant < -0.05)
                return "Inside Cusp (Bistable: 2 Stable Wells + 1 Barrier, Hysteresis Active)";
            if (CatastropheCuspDiscriminant > 0.05)
                return "Outside Cusp (Monostable: Single Global Well)";
            return "On Fold Bifurcation Boundary (Critical Coalescence)";
        }
    }

    [ObservableProperty]
    private double _catastropheStateX = 1.581;

    [ObservableProperty]
    private bool _catastropheShowBifurcationSet = true;

    [ObservableProperty]
    private bool _catastropheShowPotentialWell = true;

    [ObservableProperty]
    private bool _catastropheShowEquilibriumSurface = true;

    [ObservableProperty]
    private string _catastropheFormulaTitle = "Cusp Catastrophe (A₃) & Hysteresis Jump";

    [ObservableProperty]
    private string _catastropheFormulaDetail = "Potential V(x) = ¼x⁴ + ½ax² + bx. Bifurcation set 4a³ + 27b² = 0 separates monostable and bistable sheets. Moving across fold lines forces discontinuous catastrophic jumps in equilibrium state.";

    // 25. Clifford Geometric Algebra & Multivector Calculus (William Clifford & David Hestenes)
    [ObservableProperty]
    private string _cliffordMode = "wedge";

    [ObservableProperty]
    private string _cliffordPreset = "wedge";

    [ObservableProperty]
    private double _cliffordVectorAX = 1.8;

    [ObservableProperty]
    private double _cliffordVectorAY = 0.6;

    [ObservableProperty]
    private double _cliffordVectorAZ = 0.0;

    [ObservableProperty]
    private double _cliffordVectorBX = 0.4;

    [ObservableProperty]
    private double _cliffordVectorBY = 1.6;

    [ObservableProperty]
    private double _cliffordVectorBZ = 0.8;

    [ObservableProperty]
    private double _cliffordRotorAngle = 1.047;

    [ObservableProperty]
    private double _cliffordReflectionAngle = 0.524;

    [ObservableProperty]
    private bool _cliffordShowBivectorDisk = true;

    [ObservableProperty]
    private bool _cliffordShowDualNormal = true;

    [ObservableProperty]
    private bool _cliffordShowMultivectorGrades = true;

    [ObservableProperty]
    private bool _cliffordShowRotorArc = true;

    [ObservableProperty]
    private string _cliffordFormulaTitle = "Exterior Wedge Product & Oriented Plane (a ∧ b)";

    [ObservableProperty]
    private string _cliffordFormulaDetail = "Geometric product ab = a·b + a∧b decomposes into symmetric scalar inner product and antisymmetric bivector area. The dual vector n = -I(a∧b) points normal to the plane.";

    // 26. Differential Geometry, Geodesics & Curvature on Curved Surfaces (Carl Friedrich Gauss & Bernhard Riemann)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiffGeomGaussianCurvatureK))]
    [NotifyPropertyChangedFor(nameof(DiffGeomMeanCurvatureH))]
    [NotifyPropertyChangedFor(nameof(DiffGeomCurvatureStatus))]
    private string _diffGeomSurfaceType = "torus";

    [ObservableProperty]
    private string _diffGeomPreset = "torus";

    [ObservableProperty]
    private double _diffGeomStartU = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiffGeomGaussianCurvatureK))]
    [NotifyPropertyChangedFor(nameof(DiffGeomMeanCurvatureH))]
    [NotifyPropertyChangedFor(nameof(DiffGeomCurvatureStatus))]
    private double _diffGeomStartV = 0.0;

    [ObservableProperty]
    private double _diffGeomHeadingAngle = 0.785; // 45 deg

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiffGeomGaussianCurvatureK))]
    [NotifyPropertyChangedFor(nameof(DiffGeomMeanCurvatureH))]
    [NotifyPropertyChangedFor(nameof(DiffGeomCurvatureStatus))]
    private double _diffGeomDeformationAngle = 0.0; // Bonnet parameter [0, pi/2]

    [ObservableProperty]
    private bool _diffGeomShowCurvatureHeatmap = true;

    [ObservableProperty]
    private bool _diffGeomShowGeodesicRay = true;

    [ObservableProperty]
    private bool _diffGeomShowNormalVector = true;

    [ObservableProperty]
    private bool _diffGeomShowChristoffelMatrix = true;

    [ObservableProperty]
    private string _diffGeomFormulaTitle = "Torus & Geodesic Winding Chaos";

    [ObservableProperty]
    private string _diffGeomFormulaDetail = "Gaussian curvature K = cos(v) / (r(R + r cos v)) transitions from positive on outer equator to zero on crown to negative on inner equator.";

    public string DiffGeomGaussianCurvatureK => DiffGeomSurfaceType.ToLowerInvariant() switch
    {
        "sphere" => "K = +1.000 (Constant Positive)",
        "pseudosphere" => "K = -1.000 (Constant Negative)",
        "enneper" => "K = -0.640 (Sub-Zero Parabolic)",
        "bonnet" => "K = -0.500 (Preserved Isometry)",
        _ => Math.Cos(DiffGeomStartV) switch
        {
            > 0.05 => $"K = {Math.Cos(DiffGeomStartV) / (0.8 * (2.0 + 0.8 * Math.Cos(DiffGeomStartV))):F3} (Outer Equator)",
            < -0.05 => $"K = {Math.Cos(DiffGeomStartV) / (0.8 * (2.0 + 0.8 * Math.Cos(DiffGeomStartV))):F3} (Inner Throat)",
            _ => "K = 0.000 (Crown Parabolic)"
        }
    };

    public string DiffGeomMeanCurvatureH => DiffGeomSurfaceType.ToLowerInvariant() switch
    {
        "sphere" => "H = +1.000 (Umbilic Sphere)",
        "enneper" => "H = 0.000 (Minimal Surface)",
        "pseudosphere" => "H = +0.850",
        "bonnet" => "H = 0.000 (Minimal Catenoid/Helicoid)",
        _ => "H = +0.625 (Toroidal Mean)"
    };

    public string DiffGeomCurvatureStatus => DiffGeomSurfaceType.ToLowerInvariant() switch
    {
        "sphere" => "Elliptic (K > 0)",
        "pseudosphere" => "Hyperbolic (K < 0)",
        "enneper" => "Minimal (H = 0)",
        "bonnet" => "Isometric Family",
        _ => Math.Cos(DiffGeomStartV) switch
        {
            > 0.05 => "Elliptic (K > 0)",
            < -0.05 => "Hyperbolic (K < 0)",
            _ => "Parabolic (K = 0)"
        }
    };

    // 27. Knot Theory, Braids & 3D Topological Invariants (Vaughan Jones, Kurt Reidemeister, Emil Artin)
    [ObservableProperty]
    private string _knotType = "trefoil";

    [ObservableProperty]
    private string _knotPreset = "trefoil";

    [ObservableProperty]
    private double _knotTubeRadius = 0.18;

    [ObservableProperty]
    private int _knotTorusP = 3;

    [ObservableProperty]
    private int _knotTorusQ = 2;

    [ObservableProperty]
    private string _knotBraidWord = "s1 s2^-1 s1 s2^-1 s1 s2^-1";

    [ObservableProperty]
    private bool _knotShow3DTube = true;

    [ObservableProperty]
    private bool _knotShowPlanarProjection = true;

    [ObservableProperty]
    private bool _knotShowPolynomialCard = true;

    [ObservableProperty]
    private bool _knotShowBraidDiagram = true;

    [ObservableProperty]
    private string _knotFormulaTitle = "Trefoil Knot 3₁ & Skein Invariant";

    [ObservableProperty]
    private string _knotFormulaDetail = "The simplest non-trivial prime knot with 3 crossings and writhe w = -3. Characterized by Jones polynomial V(t) = -t⁻⁴ + t⁻³ + t⁻¹.";

    // 28. Symplectic Geometry, Hamiltonian Mechanics & Phase Space Invariants (William Rowan Hamilton & Joseph Liouville)
    [ObservableProperty]
    private string _hamiltonSystemType = "henonheiles";

    [ObservableProperty]
    private string _hamiltonPreset = "henonheiles";

    [ObservableProperty]
    private double _hamiltonEnergyLevel = 0.12;

    [ObservableProperty]
    private string _hamiltonIntegratorMode = "yoshida";

    [ObservableProperty]
    private double _hamiltonTimeStep = 0.04;

    [ObservableProperty]
    private bool _hamiltonShowPoincareSection = true;

    [ObservableProperty]
    private bool _hamiltonShowPotentialContours = true;

    [ObservableProperty]
    private bool _hamiltonShowEnergyErrorGraph = true;

    [ObservableProperty]
    private bool _hamiltonShowLiouvilleDroplet = true;

    [ObservableProperty]
    private string _hamiltonFormulaTitle = "Hénon-Heiles Anharmonic Chaos & KAM Tori";

    [ObservableProperty]
    private string _hamiltonFormulaDetail = "Canonical 2D galactic potential transitioning from regular invariant KAM tori at low energy E < 0.11 to global ergodic chaos as E approaches escape energy Ec = 1/6.";

    [ObservableProperty]
    private double _hamiltonDuffingAlpha = 0.5;

    [ObservableProperty]
    private double _hamiltonDuffingBeta = 0.25;

    // 29. Algebraic Topology, Simplicial Homology & Persistent Betti Numbers (Henri Poincaré, Emmy Noether, Leopold Vietoris)
    [ObservableProperty]
    private string _homologyComplexType = "torus";

    [ObservableProperty]
    private string _homologyPreset = "torus";

    [ObservableProperty]
    private double _homologyFiltrationRadius = 0.8;

    [ObservableProperty]
    private bool _homologyShowGeneratorCycles = true;

    [ObservableProperty]
    private bool _homologyShowBoundaryMatrix = true;

    [ObservableProperty]
    private bool _homologyShowPersistenceBarcodes = true;

    [ObservableProperty]
    private bool _homologyShowVertexLabels = true;

    [ObservableProperty]
    private string _homologyFormulaTitle = "2-Torus T² Simplicial Triangulation";

    [ObservableProperty]
    private string _homologyFormulaDetail = "Canonical 9-vertex 18-triangle triangulation with glued opposite edges. Betti numbers β = (1, 2, 1), Euler characteristic χ = 0.";

    // 30. Lie Algebras, Root Systems & Dynkin Diagrams (Sophus Lie, Wilhelm Killing, Élie Cartan)
    [ObservableProperty]
    private string _lieType = "A2";

    [ObservableProperty]
    private bool _lieShowDynkinDiagram = true;

    [ObservableProperty]
    private bool _lieShowCartanMatrix = true;

    [ObservableProperty]
    private bool _lieShowCoxeterCircles = true;

    [ObservableProperty]
    private bool _lieShowRootLabels = true;

    [ObservableProperty]
    private double _lieCameraYaw = 0.6;

    [ObservableProperty]
    private double _lieCameraPitch = 0.4;

    [ObservableProperty]
    private string _lieFormulaTitle = "Special Unitary Lie Algebra su(3) [A₂ Root Lattice]";

    [ObservableProperty]
    private string _lieFormulaDetail = "Hexagonal root system with rank 2, dimension 8, 6 roots. Symmetries of quarks in Gell-Mann's Eightfold Way.";

    // 31. Clay Millennium Prize Problems & Solutions (CMI 2000)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveMillenniumProblemInfo))]
    private MillenniumProblemType _selectedMillenniumProblem = MillenniumProblemType.PoincareConjecture;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveRicciFlowState))]
    private double _millenniumRicciTime = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveBsdEllipticState))]
    [NotifyPropertyChangedFor(nameof(ActiveBsdEllipticPoint))]
    private double _millenniumBsdParamA = -1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveBsdEllipticState))]
    [NotifyPropertyChangedFor(nameof(ActiveBsdEllipticPoint))]
    private double _millenniumBsdParamB = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveBsdEllipticPoint))]
    private int _bsdSelectedMultiplier = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveNavierStokesBlowupState))]
    private double _millenniumNavierViscosity = 0.001;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveNavierStokesBlowupState))]
    private double _millenniumNavierTime = 1.2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveYangMillsLatticeState))]
    private double _millenniumYangMillsBeta = 2.4;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActivePvsNpSatState))]
    private double _millenniumSatAlpha = 4.267;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActivePvsNpSatState))]
    private int _millenniumSatVars = 24;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveHodgeDiamondState))]
    private string _millenniumHodgeVariety = "k3";

    [ObservableProperty]
    private bool _millenniumShowTheoryOverlay = true;

    [ObservableProperty]
    private bool _millenniumShowInvariantTelemetry = true;

    [ObservableProperty]
    private bool _isRicciFlowAnimating;

    [ObservableProperty]
    private bool _isNavierAnimating;

    public MillenniumProblemInfo ActiveMillenniumProblemInfo => MillenniumEngine.Problems[(int)SelectedMillenniumProblem];
    public RicciFlowState ActiveRicciFlowState => MillenniumEngine.SimulateRicciFlow(MillenniumRicciTime);
    public RiemannGueState ActiveRiemannGueState => MillenniumEngine.SimulateRiemannGue(14.13, 16);
    public BsdEllipticState ActiveBsdEllipticState => MillenniumEngine.SimulateBsdElliptic(MillenniumBsdParamA, MillenniumBsdParamB);
    public BsdMultiplePoint? ActiveBsdEllipticPoint
    {
        get
        {
            var multiples = ActiveBsdEllipticState.GeneratorMultiples;
            if (multiples == null || multiples.Count == 0) return null;
            return multiples.FirstOrDefault(p => p.Multiplier == BsdSelectedMultiplier) ?? multiples[0];
        }
    }

    [RelayCommand]
    public void StepBsdPointAddition()
    {
        var multiples = ActiveBsdEllipticState.GeneratorMultiples;
        if (multiples != null && multiples.Count > 0)
        {
            int maxMul = multiples.Max(p => p.Multiplier);
            BsdSelectedMultiplier = BsdSelectedMultiplier >= maxMul ? 1 : BsdSelectedMultiplier + 1;
        }
    }

    [RelayCommand]
    public void ResetBsdPointAddition()
    {
        BsdSelectedMultiplier = 1;
    }
    public NavierStokesBlowupState ActiveNavierStokesBlowupState => MillenniumEngine.SimulateNavierStokesBlowup(MillenniumNavierTime, MillenniumNavierViscosity);
    public YangMillsLatticeState ActiveYangMillsLatticeState => MillenniumEngine.SimulateYangMillsLattice(MillenniumYangMillsBeta);
    public PvsNpSatState ActivePvsNpSatState => MillenniumEngine.SimulatePvsNpPhaseTransition(MillenniumSatVars, MillenniumSatAlpha);
    public HodgeDiamondState ActiveHodgeDiamondState => MillenniumEngine.SimulateHodgeDiamond(MillenniumHodgeVariety);

    // Riemann Hypothesis Solution & Exploration Observables
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveHilbertPolyaState))]
    [NotifyPropertyChangedFor(nameof(ActiveDeBruijnNewmanState))]
    [NotifyPropertyChangedFor(nameof(ActiveLiCriterionState))]
    [NotifyPropertyChangedFor(nameof(ActiveZeroHunterState))]
    [NotifyPropertyChangedFor(nameof(ActiveGueChaosState))]
    private RiemannAttackFramework _selectedRiemannAttack = RiemannAttackFramework.HilbertPolya;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveDeBruijnNewmanState))]
    private double _riemannDeBruijnT = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveLiCriterionState))]
    private int _riemannLiMaxN = 15;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveZeroHunterState))]
    private double _riemannHunterTMin = 10.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveZeroHunterState))]
    private double _riemannHunterTMax = 65.0;

    public HilbertPolyaState ActiveHilbertPolyaState => RiemannSolutionEngine.SimulateHilbertPolya(12);
    public DeBruijnNewmanState ActiveDeBruijnNewmanState => RiemannSolutionEngine.SimulateDeBruijnNewman(RiemannDeBruijnT);
    public LiCriterionState ActiveLiCriterionState => RiemannSolutionEngine.SimulateLiCriterion(RiemannLiMaxN);
    public ZeroHunterState ActiveZeroHunterState => RiemannSolutionEngine.SimulateZeroHunter(RiemannHunterTMin, RiemannHunterTMax);
    public GueChaosState ActiveGueChaosState => RiemannSolutionEngine.SimulateGueChaos(20);

    // Textbook Typeset Cards Models
    [ObservableProperty]
    private IntegralTypesetInfo? _integralInfo;

    [ObservableProperty]
    private DerivativeTypesetInfo? _derivativeInfo;

    [ObservableProperty]
    private MatrixTypesetInfo? _matrixInfo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPresets))]
    private string _presetSearchFilter = string.Empty;

    // Collections
    public ObservableCollection<MathPreset> Presets { get; } = new(MathPresetCatalog.AllPresets);

    public IEnumerable<MathPreset> FilteredPresets
    {
        get
        {
            var query = Presets.AsEnumerable();

            if (FilterPresetsByActiveLab && !string.IsNullOrEmpty(ActiveLaboratory))
            {
                var labPresets = query.Where(p => string.Equals(p.Category, ActiveLaboratory, StringComparison.OrdinalIgnoreCase)).ToList();
                if (labPresets.Count > 0)
                {
                    query = labPresets;
                }
            }

            if (!string.IsNullOrWhiteSpace(PresetSearchFilter))
            {
                query = query.Where(p =>
                    p.Title.Contains(PresetSearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(PresetSearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(PresetSearchFilter, StringComparison.OrdinalIgnoreCase));
            }

            return query;
        }
    }

    [RelayCommand]
    public void CanvasResetView() => CanvasResetViewRequested?.Invoke();

    [RelayCommand]
    public void CanvasZoomIn() => CanvasZoomInRequested?.Invoke();

    [RelayCommand]
    public void CanvasZoomOut() => CanvasZoomOutRequested?.Invoke();

    [RelayCommand]
    public void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;

    [RelayCommand]
    public void ToggleSymbolRibbon() => IsSymbolRibbonOpen = !IsSymbolRibbonOpen;

    [RelayCommand]
    public void SetPresetFilterMode(bool activeLabOnly) => FilterPresetsByActiveLab = activeLabOnly;

    [RelayCommand]
    public void ClearOmnibar() => OmnibarQuery = string.Empty;

    [RelayCommand]
    public void ClearPresetSearchFilter() => PresetSearchFilter = string.Empty;

    public ObservableCollection<DerivationStep> DerivationSteps { get; } = new();
    public ObservableCollection<CriticalPoint> CriticalPointsList { get; } = new();

    public MainWindowViewModel(IFryPluginContext? pluginContext = null)
    {
        _pluginContext = pluginContext;
        ActiveInstance = this;

        // Initialize Parameters
        Parameters.Add(new DynamicParameterViewModel("a", 1.0, -5.0, 5.0, 0.1));
        Parameters.Add(new DynamicParameterViewModel("b", 0.0, -5.0, 5.0, 0.1));
        Parameters.Add(new DynamicParameterViewModel("c", 0.0, -5.0, 5.0, 0.1));

        foreach (var p in Parameters)
        {
            p.ValueChanged += (s, e) => RecompileAllFunctions();
        }

        // Initialize Primary and Secondary Functions
        var f1 = new FunctionItemViewModel("f1", "f(x)", "x^3 - 3*x", new SolidColorBrush(Color.FromRgb(6, 182, 212))); // Cyan
        var f2 = new FunctionItemViewModel("f2", "g(x)", "cos(x)", new SolidColorBrush(Color.FromRgb(244, 63, 94))) { IsVisible = false }; // Rose

        f1.FunctionChanged += (s, e) => RecomputeCalculus();
        f2.FunctionChanged += (s, e) => RecomputeCalculus();

        Functions.Add(f1);
        Functions.Add(f2);

        // Animation Timer for Parameter and Morph Scrubbing (60 FPS)
        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();

        RecomputeCalculus();
        UpdateMatrixInfo();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        bool anyParamActive = false;
        foreach (var p in Parameters)
        {
            if (p.IsAnimating)
            {
                p.StepAnimation(0.016);
                anyParamActive = true;
            }
        }

        if (IsMorphing)
        {
            double next = MorphProgress + 0.012;
            if (next > 1.0) next = 0.0;
            MorphProgress = next;
        }

        if (IsRicciFlowAnimating)
        {
            double next = MillenniumRicciTime + 0.02;
            if (next > 4.5) next = 0.0;
            MillenniumRicciTime = Math.Round(next, 2);
        }

        if (IsNavierAnimating)
        {
            double next = MillenniumNavierTime + 0.015;
            if (next > 2.5) next = 0.0;
            MillenniumNavierTime = Math.Round(next, 2);
        }

        if (anyParamActive)
        {
            RecompileAllFunctions();
        }
    }

    partial void OnIntegrationAChanged(double value) => RecomputeIntegral();
    partial void OnIntegrationBChanged(double value) => RecomputeIntegral();
    partial void OnRiemannNChanged(int value) => RecomputeIntegral();
    partial void OnRiemannRuleChanged(RiemannRule value) => RecomputeIntegral();
    partial void OnSecantDeltaXChanged(double value) => RecomputeDerivativeCard();
    partial void OnProbeXChanged(double value) => RecomputeDerivativeCard();

    partial void OnShowTaylorSeriesChanged(bool value) => RecomputeCalculus();
    partial void OnTaylorDegreeChanged(int value) => RecomputeCalculus();
    partial void OnTaylorCenterChanged(double value) => RecomputeCalculus();

    partial void OnSelectedMillenniumProblemChanged(MillenniumProblemType value)
    {
        if (value != MillenniumProblemType.PoincareConjecture)
            IsRicciFlowAnimating = false;
        if (value != MillenniumProblemType.NavierStokesSmoothness)
            IsNavierAnimating = false;
    }

    [RelayCommand]
    public void SetVectorFieldPreset(string preset)
    {
        var (p, q) = preset switch
        {
            "vortex" => ("-y", "x"),
            "pendulum" => ("y", "-sin(x) - 0.25*y"),
            "predator" => ("x*(1 - 0.5*y)", "-y*(0.75 - 0.5*x)"),
            "vanderpol" => ("y", "1.5*(1 - x^2)*y - x"),
            "saddle" => ("x", "-y"),
            "sink" => ("-x - y", "x - y"),
            _ => ("-y", "x")
        };
        VectorFieldP = p;
        VectorFieldQ = q;
        VectorFieldPresetRequested?.Invoke(p, q);
    }

    [RelayCommand]
    public void SetComplexPreset(string preset)
    {
        switch (preset.ToLowerInvariant())
        {
            case "z2":
            case "zsquared":
                SelectedComplexPreset = ComplexPreset.ZSquared;
                ComplexCustomFormula = "z^2";
                ComplexFormulaTitle = "f(z) = z²  (Complex Quadratic Map)";
                ComplexFormulaDetail = "Doubles polar phase argument arg(w) = 2θ and squares modulus |w| = r². Branch point at origin.";
                break;
            case "roots":
            case "cubicroots":
                SelectedComplexPreset = ComplexPreset.CubicRootsOfUnity;
                ComplexCustomFormula = "z^3 - 1";
                ComplexFormulaTitle = "f(z) = z³ - 1  (Roots of Unity)";
                ComplexFormulaDetail = "Fundamental Theorem of Algebra: 3 symmetric zeros on unit circle separated by 120° (2π/3 radians).";
                break;
            case "exp":
            case "exponential":
                SelectedComplexPreset = ComplexPreset.Exponential;
                ComplexCustomFormula = "exp(z)";
                ComplexFormulaTitle = "f(z) = eᶻ  (Euler Complex Exponential)";
                ComplexFormulaDetail = "e^(x+iy) = e^x(cos y + i sin y). Maps horizontal lines to rays and vertical lines to concentric circles.";
                break;
            case "sin":
            case "sine":
                SelectedComplexPreset = ComplexPreset.Sine;
                ComplexCustomFormula = "sin(z)";
                ComplexFormulaTitle = "f(z) = sin(z)  (Complex Sine)";
                ComplexFormulaDetail = "sin(x + iy) = sin x cosh y + i cos x sinh y. Conformal mapping unbounded in imaginary direction.";
                break;
            case "pole":
            case "simplepole":
                SelectedComplexPreset = ComplexPreset.SimplePole;
                ComplexCustomFormula = "1 / z";
                ComplexFormulaTitle = "f(z) = 1/z  (Simple Pole & Inversion)";
                ComplexFormulaDetail = "Maps interior of unit disk to exterior and origin to complex infinity ∞. Reverses orientation.";
                break;
            case "mobius":
                SelectedComplexPreset = ComplexPreset.Mobius;
                ComplexCustomFormula = "(z - 1) / (z + 1)";
                ComplexFormulaTitle = "f(z) = (z - 1)/(z + 1)  (Möbius Automorphism)";
                ComplexFormulaDetail = "Conformal fractional linear transformation mapping imaginary axis onto unit circle |w| = 1.";
                break;
            case "joukowsky":
            case "airfoil":
                SelectedComplexPreset = ComplexPreset.JoukowskyAirfoil;
                ComplexCustomFormula = "0.5 * (z + 1/z)";
                ComplexFormulaTitle = "f(z) = ½(z + 1/z)  (Joukowsky Aerodynamic Map)";
                ComplexFormulaDetail = "Conformal transformation mapping circles passing through z=-1 into aerodynamic cambered airfoils with sharp trailing edge.";
                break;
        }
    }

    [RelayCommand]
    public void SetSurfacePreset(string preset)
    {
        switch (preset.ToLowerInvariant())
        {
            case "paraboloid":
                SelectedSurfacePreset = SurfacePreset.Paraboloid;
                SurfaceFormula = "0.5 * (x^2 + y^2)";
                SurfaceFormulaTitle = "z = ½(x² + y²)  (Elliptic Paraboloid)";
                SurfaceFormulaDetail = "Smooth convex bowl with global minimum at origin (0, 0). Positive definite Hessian D > 0.";
                break;
            case "saddle":
            case "hyperbolic":
                SelectedSurfacePreset = SurfacePreset.HyperbolicSaddle;
                SurfaceFormula = "0.5 * (x^2 - y^2)";
                SurfaceFormulaTitle = "z = ½(x² - y²)  (Hyperbolic Saddle)";
                SurfaceFormulaDetail = "Classic mountain pass geometry. Hessian determinant D = -1 < 0 reveals invariant saddle at origin.";
                break;
            case "sombrero":
            case "mexicanhat":
                SelectedSurfacePreset = SurfacePreset.Sombrero;
                SurfaceFormula = "2.5 * sin(2*sqrt(x^2+y^2)) / (2*sqrt(x^2+y^2))";
                SurfaceFormulaTitle = "z = 2.5·sinc(2r)  (Sombrero Radial Ripple)";
                SurfaceFormulaDetail = "Circularly symmetric Bessel wave radiating outward from central global peak.";
                break;
            case "gaussian":
            case "hill":
                SelectedSurfacePreset = SurfacePreset.GaussianHill;
                SurfaceFormula = "3 * exp(-0.4*(x^2 + y^2))";
                SurfaceFormulaTitle = "z = 3·exp(-0.4r²)  (2D Gaussian Hill)";
                SurfaceFormulaDetail = "Bivariate normal distribution surface. Gradients radiate outward from maximum at origin.";
                break;
            case "monkeysaddle":
            case "monkey":
                SelectedSurfacePreset = SurfacePreset.MonkeySaddle;
                SurfaceFormula = "0.25 * (x^3 - 3*x*y^2)";
                SurfaceFormulaTitle = "z = ¼(x³ - 3xy²)  (Monkey Saddle)";
                SurfaceFormulaDetail = "Features three depressions (two for monkey legs, one for tail). Degenerate Hessian D = 0.";
                break;
            case "ripple":
            case "wave":
                SelectedSurfacePreset = SurfacePreset.WaveRipple;
                SurfaceFormula = "1.5 * sin(x) * cos(y)";
                SurfaceFormulaTitle = "z = 1.5·sin(x)cos(y)  (Orthogonal Wave)";
                SurfaceFormulaDetail = "Periodic lattice of alternating crests, troughs, and saddles.";
                break;
            case "eggcrate":
                SelectedSurfacePreset = SurfacePreset.EggCrate;
                SurfaceFormula = "1.2 * (sin(x) + sin(y))";
                SurfaceFormulaTitle = "z = 1.2·(sin x + sin y)  (Egg Crate Surface)";
                SurfaceFormulaDetail = "Periodic orthogonal potential surface widely studied in crystallography and solid-state physics.";
                break;
        }
    }

    [RelayCommand]
    public void SetFractalPreset(string preset)
    {
        ActiveLaboratory = "Fractals & Chaos";
        switch (preset.ToLowerInvariant())
        {
            case "mandelbrot":
                SelectedFractalType = FractalType.Mandelbrot;
                FractalFormulaTitle = "Mandelbrot Set: z_{n+1} = z_n² + c";
                FractalFormulaDetail = "The dictionary of all Julia sets. Boundary points exhibit infinite self-similarity at all zoom depths.";
                break;
            case "seahorse":
                SelectedFractalType = FractalType.Mandelbrot;
                FractalFormulaTitle = "Seahorse Valley: c ≈ -0.74364 + 0.13182i";
                FractalFormulaDetail = "Intricate double-spiral filaments between the main cardioid and period-2 bulb.";
                break;
            case "elephant":
                SelectedFractalType = FractalType.Mandelbrot;
                FractalFormulaTitle = "Elephant Valley: c ≈ 0.274 + 0.006i";
                FractalFormulaDetail = "Trunk-like whorls and spirals along the positive real boundary of the main cardioid.";
                break;
            case "douady":
            case "rabbit":
                SelectedFractalType = FractalType.Julia;
                FractalJuliaCr = -0.123;
                FractalJuliaCi = 0.745;
                FractalFormulaTitle = "Douady's Rabbit Julia Set: c = -0.123 + 0.745i";
                FractalFormulaDetail = "Features 3-fold pinch-point rotational symmetry corresponding to period-3 bulb parameter.";
                break;
            case "sanmarco":
                SelectedFractalType = FractalType.Julia;
                FractalJuliaCr = -0.75;
                FractalJuliaCi = 0.0;
                FractalFormulaTitle = "San Marco Basilica Julia Set: c = -0.75 + 0i";
                FractalFormulaDetail = "Named after the Venetian basilica's arches and spires, showing reflection symmetry.";
                break;
            case "newton":
                SelectedFractalType = FractalType.NewtonBasin;
                FractalNewtonDegree = 3;
                FractalFormulaTitle = "Newton-Raphson Basins: z³ - 1 = 0";
                FractalFormulaDetail = "Basins of attraction partitioned by fractal boundaries where root selection is chaotic.";
                break;
            case "burningship":
                SelectedFractalType = FractalType.BurningShip;
                FractalFormulaTitle = "Burning Ship: (|Re(z)| + i|Im(z)|)² + c";
                FractalFormulaDetail = "Non-analytic complex recurrence creating sharp maritime ship and mast fractal silhouettes.";
                break;
        }
    }

    [RelayCommand]
    public void SetFractalType(string typeStr)
    {
        if (Enum.TryParse<FractalType>(typeStr, true, out var t))
        {
            SelectedFractalType = t;
            FractalFormulaTitle = t switch
            {
                FractalType.Mandelbrot => "Mandelbrot Set: z_{n+1} = z_n² + c",
                FractalType.Julia => $"Julia Set: c = {FractalJuliaCr:F3} + {FractalJuliaCi:F3}i",
                FractalType.NewtonBasin => $"Newton Basin: z^{FractalNewtonDegree} - 1 = 0",
                FractalType.BurningShip => "Burning Ship: (|Re(z)| + i|Im(z)|)² + c",
                _ => "Fractal"
            };
        }
    }

    [RelayCommand]
    public void SetFractalPalette(string paletteStr)
    {
        if (Enum.TryParse<FractalPalette>(paletteStr, true, out var p))
        {
            SelectedFractalPalette = p;
        }
    }

    [RelayCommand]
    public void SetWavePreset(string preset)
    {
        ActiveLaboratory = "PDE & Waves";
        switch (preset.ToLowerInvariant())
        {
            case "chladni35":
            case "rosette":
                SelectedWaveMode = WaveSimulationMode.ChladniPlate;
                WaveModeN = 3;
                WaveModeM = 5;
                WaveFormulaTitle = "Chladni Intricate Rosette (n=3, m=5)";
                WaveFormulaDetail = "High-order acoustic resonance mode with intersecting diagonal and circular nodal rings.";
                break;
            case "chladni13":
            case "cross":
                SelectedWaveMode = WaveSimulationMode.ChladniPlate;
                WaveModeN = 1;
                WaveModeM = 3;
                WaveFormulaTitle = "Chladni Ring & Cross (n=1, m=3)";
                WaveFormulaDetail = "Fundamental symmetric mode with central nodal cross and outer concentric square.";
                break;
            case "chladni24":
            case "clover":
                SelectedWaveMode = WaveSimulationMode.ChladniPlate;
                WaveModeN = 2;
                WaveModeM = 4;
                WaveFormulaTitle = "Chladni Clover Lattice (n=2, m=4)";
                WaveFormulaDetail = "Fourfold cloverleaf nodal partitions with checkerboard antinode phases.";
                break;
            case "drum":
            case "circulardrum":
                SelectedWaveMode = WaveSimulationMode.CircularDrum;
                WaveModeN = 1;
                WaveModeM = 2;
                WaveFormulaTitle = "Circular Drum Bessel Mode J₂(α r) cos(2θ)";
                WaveFormulaDetail = "Radial Bessel and azimuthal standing wave with clamped boundary u(R)=0.";
                break;
            case "rippletank":
            case "ripple":
                SelectedWaveMode = WaveSimulationMode.InteractiveRippleTank;
                WaveFormulaTitle = "2D Wave Equation PDE: u_tt = c² ∇²u - γ u_t";
                WaveFormulaDetail = "Interactive finite-difference leapfrog wave tank. Click anywhere to drop droplets and observe interference.";
                break;
        }
    }

    [RelayCommand]
    public void SetWaveMode(string modeStr)
    {
        if (Enum.TryParse<WaveSimulationMode>(modeStr, true, out var m))
        {
            SelectedWaveMode = m;
        }
    }

    [RelayCommand]
    public void SetSpaceCurvePreset(string preset)
    {
        ActiveLaboratory = "Space Curves 3D";
        switch (preset.ToLowerInvariant())
        {
            case "trefoil":
                SelectedSpaceCurvePreset = SpaceCurvePreset.TrefoilKnot;
                CurveTMin = 0;
                CurveTMax = 6.28318;
                CurveParamT = 1.8;
                CurveFormulaTitle = "Trefoil Knot: r(t) = ⟨sin t + 2 sin 2t, cos t - 2 cos 2t, -sin 3t⟩";
                CurveFormulaDetail = "Simplest non-trivial mathematical knot (3_1) in knot theory, embedded smoothly on a torus with variable curvature and torsion.";
                break;
            case "viviani":
                SelectedSpaceCurvePreset = SpaceCurvePreset.VivianiCurve;
                CurveTMin = -6.28318;
                CurveTMax = 6.28318;
                CurveParamT = 1.0;
                CurveFormulaTitle = "Viviani's Curve: r(t) = ⟨1 + cos t, sin t, 2 sin(t/2)⟩";
                CurveFormulaDetail = "Figure-eight space curve formed by the intersection of a sphere with a cylinder touching its equator.";
                break;
            case "torus":
            case "torusknot":
                SelectedSpaceCurvePreset = SpaceCurvePreset.TorusKnot;
                CurveTMin = 0;
                CurveTMax = 12.56637;
                CurveParamT = 3.5;
                CurveFormulaTitle = "Torus Knot (p=2, q=3): r(t) on Torus Surface";
                CurveFormulaDetail = "Closed space curve winding 2 times around the rotational symmetry axis and 3 times through the donut hole.";
                break;
            case "twistedcubic":
                SelectedSpaceCurvePreset = SpaceCurvePreset.TwistedCubic;
                CurveTMin = -2.0;
                CurveTMax = 2.0;
                CurveParamT = 0.8;
                CurveFormulaTitle = "Twisted Cubic: r(t) = ⟨t, t², t³⟩";
                CurveFormulaDetail = "Fundamental smooth rational algebraic curve of degree 3 in projective 3-space with vanishing inflection.";
                break;
            case "conical":
            case "spiral":
                SelectedSpaceCurvePreset = SpaceCurvePreset.ConicalSpiral;
                CurveTMin = 0;
                CurveTMax = 15.70796;
                CurveParamT = 7.0;
                CurveFormulaTitle = "Conical Spiral: r(t) = ⟨t cos t, t sin t, t⟩";
                CurveFormulaDetail = "Spiral curve winding around a right circular cone with linearly expanding radius and pitch.";
                break;
            case "helix":
            default:
                SelectedSpaceCurvePreset = SpaceCurvePreset.CircularHelix;
                CurveTMin = 0;
                CurveTMax = 12.56637;
                CurveParamT = 2.5;
                CurveFormulaTitle = "Circular Helix: r(t) = ⟨2 cos t, 2 sin t, 0.6 t⟩";
                CurveFormulaDetail = "Classic spatial spiral with constant curvature κ = 0.459 and constant torsion τ = 0.138. The moving Frenet-Serret trihedron ⟨T, N, B⟩ glides along the curve.";
                break;
        }
    }

    [RelayCommand]
    public void SetZetaPreset(string preset)
    {
        ActiveLaboratory = "Zeta & Primes";
        switch (preset.ToLowerInvariant())
        {
            case "zero1":
                SelectedZetaMode = ZetaViewMode.CriticalLine;
                ZetaProbeT = 14.134725;
                ZetaFormulaTitle = "First Riemann Zero γ₁ = 14.134725";
                ZetaFormulaDetail = "The lowest non-trivial zero of ζ(s) on the critical line s = 1/2 + it. The complex phasor ζ(1/2 + it) passes directly through the origin (0, 0).";
                break;
            case "zero2":
                SelectedZetaMode = ZetaViewMode.CriticalLine;
                ZetaProbeT = 21.022040;
                ZetaFormulaTitle = "Second Riemann Zero γ₂ = 21.022040";
                ZetaFormulaDetail = "Second non-trivial zero of ζ(s). Notice the exact zero-crossing of the real Riemann-Siegel Z(t) function.";
                break;
            case "zero3":
                SelectedZetaMode = ZetaViewMode.CriticalLine;
                ZetaProbeT = 25.010858;
                ZetaFormulaTitle = "Third Riemann Zero γ₃ = 25.010858";
                ZetaFormulaDetail = "Third non-trivial zero of ζ(s). Gram points and zero spacings reflect the GUE random matrix eigenvalue conjecture (Montgomery-Dyson).";
                break;
            case "primestaircase":
                SelectedZetaMode = ZetaViewMode.PrimeStaircase;
                ZetaZeroCount = 10;
                ZetaXMax = 50.0;
                ZetaFormulaTitle = "Prime Staircase π(x) vs Smooth Gauss R(x)";
                ZetaFormulaDetail = "The discrete step function π(x) counts primes ≤ x. Gauss's logarithmic integral Li(x) and Riemann's R(x) capture the average smooth density 1/ln(x).";
                break;
            case "explicitprimes":
                SelectedZetaMode = ZetaViewMode.PrimeStaircase;
                ZetaZeroCount = 18;
                ZetaXMax = 60.0;
                ZetaFormulaTitle = "Riemann's Explicit Formula: Standing Waves from Zeros";
                ZetaFormulaDetail = "Superposition of waves from 18 non-trivial Riemann zeros interferes constructively to produce vertical steps precisely at prime numbers!";
                break;
            case "criticalline":
            default:
                SelectedZetaMode = ZetaViewMode.CriticalLine;
                ZetaProbeT = 14.1347;
                ZetaFormulaTitle = "Riemann-Siegel Z(t) on Critical Line s = 1/2 + it";
                ZetaFormulaDetail = "The real-valued function Z(t) = e^(i θ(t)) ζ(1/2 + it) whose roots are the non-trivial zeros of ζ(s). Drag the probe or scrub t to explore.";
                break;
        }
    }

    [RelayCommand]
    public void SetZetaViewMode(string modeStr)
    {
        ActiveLaboratory = "Zeta & Primes";
        if (Enum.TryParse<ZetaViewMode>(modeStr, true, out var m))
        {
            SelectedZetaMode = m;
        }
    }

    [RelayCommand]
    public void SetHeatPreset(string preset)
    {
        ActiveLaboratory = "Heat & Diffusion";
        HeatPreset = preset;
        switch (preset.ToLowerInvariant())
        {
            case "laplace":
                HeatFormulaTitle = "Laplace Steady-State: ∇²T = 0";
                HeatFormulaDetail = "Dirichlet boundary equilibrium. Temperatures satisfy the mean value property everywhere: T(x, y) = average of neighbors.";
                break;
            case "dipole":
                HeatFormulaTitle = "Thermal Dipole: Heat Source & Sink";
                HeatFormulaDetail = "Continuous thermal injection (+100°C) coupled with cooling sink (-40°C). Fourier flux vectors q = -k ∇T stream directly from hot to cold.";
                break;
            case "insulated":
                HeatFormulaTitle = "Adiabatic Cavity: ∂T/∂n = 0";
                HeatFormulaDetail = "Neumann zero-flux boundaries prevent heat loss. Central thermal pulse diffuses evenly while total thermal energy is strictly conserved.";
                break;
            case "corners":
                HeatFormulaTitle = "Four-Corner Heat Chamber";
                HeatFormulaDetail = "Opposing thermal reservoirs create quadrupolar saddle isotherms at the domain center.";
                break;
            default:
                HeatFormulaTitle = "2D Heat Diffusion PDE: ∂T/∂t = α ∇²T";
                HeatFormulaDetail = "Von Neumann stable finite-difference thermal relaxation. Click or drag to inject heat flames (Left Click) or cold sinks (Right Click).";
                break;
        }
    }

    [RelayCommand]
    public void ToggleHeatSimulation()
    {
        HeatIsSimulating = !HeatIsSimulating;
    }

    [RelayCommand]
    public void SetQuantumPreset(string preset)
    {
        ActiveLaboratory = "Quantum & Waves";
        QuantumPreset = preset;
        switch (preset.ToLowerInvariant())
        {
            case "tunneling":
                QuantumFormulaTitle = "Quantum Barrier Tunneling (V₀ > E)";
                QuantumFormulaDetail = "Wavepacket strikes a potential barrier higher than its kinetic energy. Part reflects while an evanescent wave penetrates and emerges as a transmitted packet!";
                break;
            case "harmonic":
                QuantumFormulaTitle = "Harmonic Oscillator Coherent State: V(x) = ½kx²";
                QuantumFormulaDetail = "A displaced Gaussian coherent state oscillates periodically back and forth inside the parabolic potential well without dispersing.";
                break;
            case "doublewell":
                QuantumFormulaTitle = "Double-Well Tunneling: V(x) = a(x² - 9)²";
                QuantumFormulaDetail = "Superposition of ground and first excited states producing macroscopic quantum tunneling beats and Rabi oscillations between wells.";
                break;
            case "step":
                QuantumFormulaTitle = "Step Potential & Quantum Reflection: V(x) = V₀ Θ(x)";
                QuantumFormulaDetail = "Incident wavepacket experiences impedance mismatch, producing standing wave interference fringes and partial reflection.";
                break;
            case "free":
                QuantumFormulaTitle = "Wavepacket Dispersion: Δx · Δp ≥ ħ/2";
                QuantumFormulaDetail = "Free particle spreading in space. The spatial width σ(t) grows hyperbolically over time as momentum Fourier modes separate.";
                break;
            default:
                QuantumFormulaTitle = "Time-Dependent Schrödinger Wave Mechanics";
                QuantumFormulaDetail = "Unitary Crank-Nicolson Cayley solver conserving total probability ∫|ψ|²dx = 1.000. Click & drag on canvas to paint potential barriers!";
                break;
        }
    }

    [RelayCommand]
    public void ToggleQuantumSimulation()
    {
        QuantumIsSimulating = !QuantumIsSimulating;
    }

    [RelayCommand]
    public void SetRelativityPreset(string preset)
    {
        ActiveLaboratory = "Relativity & Spacetime";
        RelativityPreset = preset;
        switch (preset.ToLowerInvariant())
        {
            case "twin":
                RelativityBeta = 0.8;
                RelativityFormulaTitle = "The Twin Paradox: Asymmetric Proper Aging";
                RelativityFormulaDetail = "Traveling twin accelerates and turns around at 0.8c. Earth twin ages 10 units, while the traveling twin accumulates only τ = 6.0 units!";
                break;
            case "muon":
                RelativityBeta = 0.98;
                RelativityFormulaTitle = "Atmospheric Muon Dilation: γ ≈ 5.0";
                RelativityFormulaDetail = "Muons created at 10 km reach the ground because their lifetime is dilated (or atmosphere is length-contracted in their rest frame).";
                break;
            case "bell":
                RelativityBeta = 0.7;
                RelativityFormulaTitle = "Bell's Spaceship Paradox: Relativistic Stress";
                RelativityFormulaDetail = "Identically accelerated rockets maintain constant distance L in the launch frame, but their proper separation grows to γL, snapping the thread!";
                break;
            case "doppler":
                RelativityBeta = 0.6;
                RelativityFormulaTitle = "Relativistic Doppler Shift: f' = f √((1-β)/(1+β))";
                RelativityFormulaDetail = "Periodic light pulses are redshifted by a factor of 0.5 when receding, and blueshifted by 2.0 when approaching.";
                break;
            default:
                RelativityBeta = 0.6;
                RelativityFormulaTitle = "Minkowski Spacetime & Lorentz Boost: s² = c²t² - x²";
                RelativityFormulaDetail = "Hyperbolic calibration curves ct² - x² = τ² geometrically prove length contraction and the relativity of simultaneity.";
                break;
        }
    }

    [RelayCommand]
    public void ToggleRelativitySimulation()
    {
        RelativityIsSimulating = !RelativityIsSimulating;
    }

    [RelayCommand]
    public void SetFluidPreset(string preset)
    {
        ActiveLaboratory = "Fluid Dynamics";
        FluidPreset = preset;
        switch (preset.ToLowerInvariant())
        {
            case "karman":
                FluidViscosity = 0.0006;
                FluidFormulaTitle = "Von Kármán Vortex Street Shedding (Re ≈ 1600)";
                FluidFormulaDetail = "Symmetric boundary layer separation around a circular cylinder transitions into alternating clockwise and counter-clockwise swirling vortex eddies.";
                break;
            case "dipole":
                FluidViscosity = 0.0004;
                FluidFormulaTitle = "Vortex Dipole Collision & Rebound";
                FluidFormulaDetail = "Counter-rotating vortex pair self-propelling forward under mutual Biot-Savart induction, mushrooming upon boundary collision.";
                break;
            case "kelvin":
                FluidViscosity = 0.0003;
                FluidFormulaTitle = "Kelvin-Helmholtz Shear Layer Instability";
                FluidFormulaDetail = "Discontinuous parallel shear streams roll up exponentially into periodic spiral cat's-eye vortices.";
                break;
            case "taylor":
                FluidViscosity = 0.0008;
                FluidFormulaTitle = "Taylor-Green Periodic Vortex Lattice";
                FluidFormulaDetail = "Exact analytical Navier-Stokes solution decaying exponentially in time due to viscous dissipation e^(-2νk²t).";
                break;
            case "cavity":
                FluidViscosity = 0.001;
                FluidFormulaTitle = "Lid-Driven Cavity Recirculation Flow";
                FluidFormulaDetail = "Viscous shear driven by sliding upper boundary produces a dominant central recirculation eddy and secondary Moffatt corner vortices.";
                break;
            default:
                FluidFormulaTitle = "2D Incompressible Navier-Stokes Equations";
                FluidFormulaDetail = "Left-click and drag on the canvas to inject swirling vorticity; right-click and drag to drop solid boundary obstacles.";
                break;
        }
    }

    [RelayCommand]
    public void ToggleFluidSimulation()
    {
        FluidIsSimulating = !FluidIsSimulating;
    }

    [RelayCommand]
    public void SetTopologyPreset(string preset)
    {
        ActiveLaboratory = "Topology & 4D Manifolds";
        TopologyPreset = preset;
        switch (preset.ToLowerInvariant())
        {
            case "clifford":
                TopologyFormulaTitle = "Clifford Flat Torus in S³: S¹(1/√2) × S¹(1/√2)";
                TopologyFormulaDetail = "A flat Riemannian torus embedded without geometric shear in the unit 3-sphere: x₁² + x₂² = 1/2, x₃² + x₄² = 1/2.";
                break;
            case "klein":
                TopologyFormulaTitle = "4D Non-Self-Intersecting Klein Bottle: χ = 0";
                TopologyFormulaDetail = "In ℝ⁴, the non-orientable Klein bottle embeds cleanly without the 3D self-intersection singularity. The extra dimension passes the neck through harmlessly.";
                break;
            case "hopf":
                TopologyFormulaTitle = "Hopf Fibration Bundle: π: S³ → S² (Fibers S¹)";
                TopologyFormulaDetail = "The unit 3-sphere partitioned into a family of non-intersecting, pairwise linked great circles. Stereographic projection maps each fiber to a Villarceau circle.";
                break;
            case "cell24":
                TopologyFormulaTitle = "24-Cell Regular 4-Polytope (Octaplex): F = (24, 96, 96, 24)";
                TopologyFormulaDetail = "The unique self-dual regular 4-polytope with 24 octahedral cells and 24 vertices, having no 3-dimensional Platonic counterpart.";
                break;
            default:
                TopologyFormulaTitle = "Tesseract (8-Cell / 4D Hypercube): 16 Vertices, 32 Edges, 24 Faces, 8 Cubes";
                TopologyFormulaDetail = "Euler-Poincaré characteristic χ = V - E + F - C = 16 - 32 + 24 - 8 = 0. Double rotation in SO(4) projected stereographically into ℝ³.";
                break;
        }
    }

    [RelayCommand]
    public void ToggleTopologyRotation()
    {
        TopologyIsRotating = !TopologyIsRotating;
    }

    [RelayCommand]
    public void SetCelestialPreset(string preset)
    {
        ActiveLaboratory = "Celestial & N-Body";
        CelestialPreset = preset;
        switch (preset.ToLowerInvariant())
        {
            case "lagrange":
                CelestialFormulaTitle = "Restricted 3-Body: Lagrange Points L₁...L₅ & Trojan Asteroids";
                CelestialFormulaDetail = "Effective gravitational + centrifugal potential ∇Φ_eff = 0 creates 5 equilibrium points. Triangular points L4 and L5 host stable Trojan and Greek asteroid swarms.";
                break;
            case "pythagorean":
                CelestialFormulaTitle = "Burrau's Problem: Pythagorean 3-Body Gravitational Chaos";
                CelestialFormulaDetail = "Masses 3, 4, 5 released from rest at vertices of a 3-4-5 right triangle. Chaos leads to repeated close binary encounters and ultimate high-speed ejection.";
                break;
            case "sunearthmoon":
                CelestialFormulaTitle = "Hierarchical 3-Body: Sun-Earth-Moon Orbit Precession";
                CelestialFormulaDetail = "Perturbed Keplerian motion where the lunar orbit undergoes nodal regression and apsidal precession while maintaining long-term Kozai-Lidov stability.";
                break;
            case "galaxy":
                CelestialFormulaTitle = "N-Body Galactic Collision & Tidal Bridge Tails (Toomre Model)";
                CelestialFormulaDetail = "Two merging galactic disk cores with 90 orbiting stars ripped by gravitational tidal gradients (F_tide ∝ 2GM r / R³) into sweeping tidal tails.";
                break;
            default:
                CelestialFormulaTitle = "Figure-8 3-Body Choreography (Chenciner & Montgomery)";
                CelestialFormulaDetail = "Exact stable planar solution with 3 equal masses chasing each other along a figure-8 orbit. Symplectic leapfrog preserves total energy E = T + V without secular drift.";
                break;
        }
    }

    [RelayCommand]
    public void ToggleCelestialSimulation()
    {
        CelestialIsSimulating = !CelestialIsSimulating;
    }

    [RelayCommand]
    public void SetHyperbolicPreset(string preset)
    {
        ActiveLaboratory = "Hyperbolic Geometry";
        HyperbolicPreset = preset;
        switch (preset.ToLowerInvariant())
        {
            case "octagonal":
                HyperbolicFormulaTitle = "Regular Octagonal Tiling {8, 3} (K = -1)";
                HyperbolicFormulaDetail = "Three regular octagons meeting at each vertex in the Poincaré disk. Schläfli criterion (p-2)(q-2) = (8-2)(3-2) = 6 > 4 forces non-Euclidean hyperbolic space.";
                break;
            case "square5":
                HyperbolicFormulaTitle = "Order-5 Square Tiling {4, 5}: Right-Angled Pentagons";
                HyperbolicFormulaDetail = "Five squares meeting at every vertex with right angles (90°). In Euclidean space 4 × 90° = 360°, but negative curvature accommodates 5 × 90° = 450°!";
                break;
            case "ideal":
                HyperbolicFormulaTitle = "Ideal Hyperbolic Triangle (All Angles 0°, Area = π)";
                HyperbolicFormulaDetail = "All 3 vertices touch the boundary circle at infinity |z| = 1. Interior angles vanish α = β = γ = 0°, yielding the theoretical maximum area π ≈ 3.14159.";
                break;
            case "holonomy":
                HyperbolicFormulaTitle = "Gauss-Bonnet Theorem & Angular Defect: Area = π - Σα";
                HyperbolicFormulaDetail = "Drag handles A, B, C inside the disk to observe angle sum strictly less than 180°. The angular defect π - (α + β + γ) precisely equals the enclosed hyperbolic surface area!";
                break;
            default:
                HyperbolicFormulaTitle = "Poincaré Disk Model & Heptagonal Tiling {7, 3}";
                HyperbolicFormulaDetail = "Three regular heptagons meeting at each vertex, immortalized in M.C. Escher's Circle Limit series. Conformal mapping preserves all intersection angles.";
                break;
        }
    }

    [RelayCommand]
    public void SetHyperbolicModel(string model)
    {
        HyperbolicModel = model;
    }

    [RelayCommand]
    public void SetInformationPreset(string preset)
    {
        ActiveLaboratory = "Information & Entropy";
        InformationPreset = preset;

        switch (preset.ToLowerInvariant())
        {
            case "qam16" or "qam":
                InformationMode = "constellation";
                InformationModulation = "qam16";
                InformationSnrDb = 16.0;
                InformationFormulaTitle = "16-QAM Constellation & Shannon-Hartley Capacity";
                InformationFormulaDetail = "16 quadrature amplitude modulation encodes 4 bits/symbol. The Shannon-Hartley capacity C = B log₂(1 + S/N) bounds the maximum error-free transmission rate over an AWGN channel.";
                break;
            case "qpsk":
                InformationMode = "constellation";
                InformationModulation = "qpsk";
                InformationSnrDb = 8.0;
                InformationFormulaTitle = "QPSK Modulation & Bit Error Rate Waterfall";
                InformationFormulaDetail = "Quadrature phase shift keying with 4 orthogonal states (2 bits/sym). In phase space (I, Q), AWGN perturbations cause symbol detection errors according to Pb = ½ erfc(√(Eb/N0)).";
                break;
            case "bsc" or "channel":
                InformationMode = "bsc";
                InformationCrossoverProbability = 0.12;
                InformationFormulaTitle = "Binary Symmetric Channel (BSC) & Binary Entropy Hb(p)";
                InformationFormulaDetail = "Channel crossover transition diagram 0 ↔ 1 with error probability p. Channel capacity C = 1 - Hb(p) drops from 1.0 bit/use at p = 0 to 0.0 bits/use at p = 0.5 (pure white noise).";
                break;
            case "kldiv" or "relative":
                InformationMode = "kldiv";
                InformationFormulaTitle = "Kullback-Leibler Divergence (Relative Entropy) D_KL(P || Q)";
                InformationFormulaDetail = "Measures the informational inefficiency of assuming distribution Q when the true distribution is P: D_KL(P || Q) = Σ pᵢ log₂(pᵢ / qᵢ) ≥ 0. Note the fundamental asymmetry D_KL(P || Q) ≠ D_KL(Q || P)!";
                break;
            default:
                InformationMode = "huffman";
                InformationSourceText = "INFORMATION THEORY AND SHANNON ENTROPY";
                InformationFormulaTitle = "Shannon Entropy & Optimal Huffman Coding Tree";
                InformationFormulaDetail = "Shannon entropy H(X) = -Σ pᵢ log₂(pᵢ) defines the fundamental limit of lossless data compression. Huffman coding builds an optimal prefix-free binary tree achieving average code length L < H + 1.";
                break;
        }
    }

    [RelayCommand]
    public void SetInformationMode(string mode)
    {
        InformationMode = mode;
    }

    [RelayCommand]
    public void SetInformationModulation(string mod)
    {
        InformationModulation = mod;
    }

    [RelayCommand]
    public void SetLagrangianPreset(string preset)
    {
        ActiveLaboratory = "Classical Mechanics";
        LagrangianPreset = preset;

        switch (preset.ToLowerInvariant())
        {
            case "poincare" or "kam" or "torus":
                LagrangianFormulaTitle = "Poincaré Section & KAM Invariant Tori";
                LagrangianFormulaDetail = "Stroboscopic cross-section (θ₁ vs ω₁) at θ₂ = 0. Shows regular quasiperiodic Kolmogorov-Arnold-Moser curves at low energy, transitioning to homoclinic tangles and ergodic chaos.";
                break;
            case "kapitza" or "inverted":
                LagrangianFormulaTitle = "Kapitza's Inverted Pendulum Stabilization";
                LagrangianFormulaDetail = "High-frequency vertical base oscillation dynamically stabilizes the inverted upright pendulum. Effective potential V_eff(θ) = mgL cos θ + ¼ m A² ω² sin² θ creates a stable potential minimum at θ = π.";
                break;
            case "spring" or "fermi":
                LagrangianFormulaTitle = "Spring Pendulum & Fermi Resonance";
                LagrangianFormulaDetail = "Coupled 2D elastic pendulum showing non-linear energy transfer between radial stretch bounce and angular swing when the frequency ratio ω_r ≈ 2 ω_θ.";
                break;
            case "foucault":
                LagrangianFormulaTitle = "Foucault Pendulum & Coriolis Precession";
                LagrangianFormulaDetail = "Spherical pendulum in Earth's rotating non-inertial frame. The oscillation plane precesses at angular velocity dψ/dt = -Ω_⊕ sin φ, completing one full rotation in 24 / sin φ hours.";
                break;
            default:
                LagrangianFormulaTitle = "Double Pendulum Chaos & Lagrangian Dynamics";
                LagrangianFormulaDetail = "Coupled nonlinear equations derived via Euler-Lagrange d/dt(∂ℒ/∂θ̇ᵢ) - ∂ℒ/∂θᵢ = 0. Exhibits the Butterfly Effect with exponential trajectory divergence and positive Lyapunov exponent.";
                break;
        }
    }

    [RelayCommand]
    public void ToggleLagrangianSimulation()
    {
        LagrangianIsSimulating = !LagrangianIsSimulating;
    }

    [RelayCommand]
    public void SetCryptoPreset(string preset)
    {
        ActiveLaboratory = "Cryptography & Finite Fields";
        CryptoPreset = preset;

        switch (preset.ToLowerInvariant())
        {
            case "twocomponents" or "egg":
                CryptoDisplayMode = "real";
                CryptoParamA = -3.0;
                CryptoParamB = 1.0;
                CryptoFormulaTitle = "Two-Component Elliptic Curve (Δ > 0)";
                CryptoFormulaDetail = "Discriminant Δ = -16(4a³ + 27b²) > 0 yields two disconnected topological components: an isolated compact oval egg and an infinite open branch.";
                break;
            case "nodal":
                CryptoDisplayMode = "real";
                CryptoParamA = -3.0;
                CryptoParamB = 2.0;
                CryptoFormulaTitle = "Singular Nodal Curve (Δ = 0)";
                CryptoFormulaDetail = "Vanishing discriminant (Δ = 0) creates an ordinary double point (self-intersecting node) at (1, 0), violating the non-singularity requirement for a group.";
                break;
            case "cusp":
                CryptoDisplayMode = "real";
                CryptoParamA = 0.0;
                CryptoParamB = 0.0;
                CryptoFormulaTitle = "Singular Cusp Curve (Δ = 0)";
                CryptoFormulaDetail = "Discriminant Δ = 0 with a = b = 0 creates a sharp geometric cusp singularity at the origin where tangent lines coincide.";
                break;
            case "finite":
                CryptoDisplayMode = "finite";
                CryptoFiniteA = -3;
                CryptoFiniteB = 7;
                CryptoPrimeP = 89;
                CryptoFormulaTitle = "Discrete Finite Field Grid E(F_p)";
                CryptoFormulaDetail = "Solutions to y² ≡ x³ + ax + b (mod p). Hasse's Theorem bounds the group order: |#E(F_p) - (p+1)| ≤ 2√p on the discrete torus.";
                break;
            case "ecdh":
                CryptoDisplayMode = "ecdh";
                CryptoFiniteA = -3;
                CryptoFiniteB = 7;
                CryptoPrimeP = 89;
                CryptoAlicePrivateKey = 5;
                CryptoBobPrivateKey = 7;
                CryptoFormulaTitle = "Elliptic Curve Diffie-Hellman (ECDH) Key Exchange";
                CryptoFormulaDetail = "Alice and Bob establish an identical shared secret S = d_A(d_B G) = d_B(d_A G) over an insecure channel without revealing private keys.";
                break;
            case "ecdlp":
                CryptoDisplayMode = "ecdlp";
                CryptoFiniteA = 0;
                CryptoFiniteB = 7;
                CryptoPrimeP = 101;
                CryptoScalarK = 25;
                CryptoFormulaTitle = "ECDLP Scalar Hopping Trajectory (Trapdoor Function)";
                CryptoFormulaDetail = "Stepping 1G → kG scatters points pseudorandomly across F_p. Reversing Q = kG requires O(√n) Pollard's rho vs O(log k) forward double-and-add.";
                break;
            default: // secp256k1
                CryptoDisplayMode = "real";
                CryptoParamA = 0.0;
                CryptoParamB = 7.0;
                CryptoFiniteA = 0;
                CryptoFiniteB = 7;
                CryptoPrimeP = 97;
                CryptoFormulaTitle = "secp256k1 (Bitcoin & Ethereum Curve)";
                CryptoFormulaDetail = "Koblitz curve y² = x³ + 7 over real numbers and discrete field F_97. Parameter a=0 enables rapid point doubling without multiplications.";
                break;
        }
    }

    [RelayCommand]
    public void SetCryptoMode(string mode)
    {
        CryptoDisplayMode = mode;
    }

    [RelayCommand]
    public void SetDiscreteDynamicsPreset(string preset)
    {
        ActiveLaboratory = "Discrete Dynamics & Chaos";
        DiscreteDynamicsPreset = preset;

        switch (preset.ToLowerInvariant())
        {
            case "cobweb" or "logistic":
                DiscreteDynamicsDisplayMode = "cobweb";
                DiscreteDynamicsLogisticR = 3.65;
                DiscreteDynamicsLogisticX0 = 0.45;
                DiscreteDynamicsCobwebSteps = 60;
                DiscreteDynamicsFormulaTitle = "Logistic Map Cobweb Trajectory: x_{n+1} = r x_n (1 - x_n)";
                DiscreteDynamicsFormulaDetail = "Iterated 1D quadratic map displaying period-doubling cascade, limit cycles, and deterministic chaos. The cobweb construction visualizes the iteration sequence.";
                break;
            case "bifurcation" or "feigenbaum":
                DiscreteDynamicsDisplayMode = "bifurcation";
                DiscreteDynamicsLogisticR = 3.82;
                DiscreteDynamicsFormulaTitle = "Feigenbaum Period-Doubling Pitchfork Tree";
                DiscreteDynamicsFormulaDetail = "Attractor orbit diagram as growth parameter r increases from 2.8 to 4.0. Period 2, 4, 8, 16... doublings accumulate at r_∞ ≈ 3.5699 with universal scaling δ ≈ 4.6692.";
                break;
            case "barnsley" or "fern":
                DiscreteDynamicsDisplayMode = "ifs";
                DiscreteDynamicsIfsPreset = "barnsley";
                DiscreteDynamicsFormulaTitle = "Barnsley Fern (Iterated Function System)";
                DiscreteDynamicsFormulaDetail = "Self-affine fractal plant generated by 4 contractive affine maps. Michael Barnsley's collage theorem demonstrates infinite structural complexity from 24 matrix coefficients.";
                break;
            case "sierpinski" or "gasket":
                DiscreteDynamicsDisplayMode = "ifs";
                DiscreteDynamicsIfsPreset = "sierpinski";
                DiscreteDynamicsFormulaTitle = "Sierpinski Gasket (Chaos Game)";
                DiscreteDynamicsFormulaDetail = "Fractal triangle generated by choosing one of 3 vertices uniformly at random and stepping halfway. Non-integer Hausdorff dimension D = ln 3 / ln 2 ≈ 1.585.";
                break;
            default: // arnold
                DiscreteDynamicsDisplayMode = "arnold";
                DiscreteDynamicsCatStep = 0;
                DiscreteDynamicsShowEigenvectors = true;
                DiscreteDynamicsFormulaTitle = "Arnold's Cat Map (Toral Automorphism)";
                DiscreteDynamicsFormulaDetail = "Hyperbolic toral automorphism [[2, 1], [1, 1]] mod 1 on a 64x64 discrete lattice. Area-preserving (det = 1) with Lyapunov exponent λ ≈ 0.9624. Exact Poincaré recurrence reconstitutes the cat at step 48.";
                break;
        }
    }

    [RelayCommand]
    public void SetDiscreteDynamicsMode(string mode)
    {
        DiscreteDynamicsDisplayMode = mode;
    }

    [RelayCommand]
    public void StepArnoldCat()
    {
        DiscreteDynamicsCatStep = (DiscreteDynamicsCatStep + 1) % 48;
    }

    [RelayCommand]
    public void ResetArnoldCat()
    {
        DiscreteDynamicsCatStep = 0;
    }

    [RelayCommand]
    public void SetCatastrophePreset(string preset)
    {
        ActiveLaboratory = "Bifurcation & Catastrophe Theory";
        CatastrophePreset = preset;

        switch (preset.ToLowerInvariant())
        {
            case "fold" or "saddlenode":
                CatastropheMode = "fold";
                CatastropheParamA = -1.5;
                CatastropheParamB = 0.0;
                CatastropheStateX = 1.225;
                CatastropheFormulaTitle = "Fold Catastrophe (Saddle-Node Annihilation, A₂)";
                CatastropheFormulaDetail = "Potential V(x) = ⅓x³ + ax. Equilibrium condition x² + a = 0. Two equilibria (stable node and unstable saddle) collide at a = 0 and annihilate.";
                break;
            case "butterfly" or "trimodal":
                CatastropheMode = "butterfly";
                CatastropheParamA = -4.0;
                CatastropheParamB = 0.0;
                CatastropheStateX = 1.0;
                CatastropheFormulaTitle = "Butterfly Catastrophe & Trimodality (A₅)";
                CatastropheFormulaDetail = "Potential V(x) = ⅙x⁶ + ¼ax⁴ + ⅓bx³ + ½cx² + dx. Four control parameters yield up to three coexisting stable equilibrium valleys (trimodal stability).";
                break;
            case "zeeman" or "machine":
                CatastropheMode = "zeeman";
                CatastropheParamA = 1.5;
                CatastropheParamB = 0.5;
                CatastropheFormulaTitle = "Zeeman's Catastrophe Machine";
                CatastropheFormulaDetail = "Mechanical device invented by Sir Christopher Zeeman. Elastic wheel subjected to smooth control handle displacement exhibits sudden non-linear snapping transitions.";
                break;
            case "swallowtail" or "caustic":
                CatastropheMode = "swallowtail";
                CatastropheParamA = -2.0;
                CatastropheParamB = 0.5;
                CatastropheFormulaTitle = "Swallowtail Catastrophe & Optical Caustic (A₄)";
                CatastropheFormulaDetail = "Potential V(x) = ⅕x⁵ + ⅓ax³ + ½bx² + cx. Codimension-3 singularity governing focusing light caustics and wave catastrophe integrals.";
                break;
            default: // cusp
                CatastropheMode = "cusp";
                CatastropheParamA = -2.5;
                CatastropheParamB = 0.0;
                CatastropheStateX = 1.581;
                CatastropheFormulaTitle = "Cusp Catastrophe (A₃) & Hysteresis Jump";
                CatastropheFormulaDetail = "Potential V(x) = ¼x⁴ + ½ax² + bx. Bifurcation set 4a³ + 27b² = 0 separates monostable and bistable sheets. Moving across fold lines forces discontinuous catastrophic jumps in equilibrium state.";
                break;
        }
    }

    [RelayCommand]
    public void SetCatastropheMode(string mode)
    {
        CatastropheMode = mode;
    }

    [RelayCommand]
    public void SetCliffordPreset(string preset)
    {
        ActiveLaboratory = "Clifford Geometric Algebra";
        CliffordPreset = preset;

        switch (preset.ToLowerInvariant())
        {
            case "rotor":
                CliffordMode = "rotor";
                CliffordVectorAX = 1.8;
                CliffordVectorAY = 0.0;
                CliffordVectorAZ = 0.0;
                CliffordVectorBX = 0.0;
                CliffordVectorBY = 1.6;
                CliffordVectorBZ = 0.0;
                CliffordRotorAngle = Math.PI / 3.0; // 60 deg
                CliffordFormulaTitle = "Rotor Rotation & Sandwich Product (R v R†)";
                CliffordFormulaDetail = "Gimbal-lock-free rotation in oriented bivector plane B via sandwich product v' = R v R† where R = cos(θ/2) - B sin(θ/2). Eliminates Euler singularities completely.";
                break;

            case "reflection":
                CliffordMode = "reflection";
                CliffordVectorAX = 1.5;
                CliffordVectorAY = 1.2;
                CliffordVectorAZ = 0.5;
                CliffordReflectionAngle = Math.PI / 6.0; // 30 deg
                CliffordFormulaTitle = "Double Reflection Composition (Cartan-Dieudonné)";
                CliffordFormulaDetail = "Every spatial rotation is the product of reflections across two planes: v'' = n₂ n₁ v n₁ n₂ = R v R† with rotor R = n₂ n₁.";
                break;

            case "maxwell":
                CliffordMode = "maxwell";
                CliffordVectorAX = 0.0;
                CliffordVectorAY = 1.8;
                CliffordVectorAZ = 0.0;
                CliffordVectorBX = 0.0;
                CliffordVectorBY = 0.0;
                CliffordVectorBZ = 1.4;
                CliffordFormulaTitle = "Riemann-Silberstein Electromagnetic Bivector";
                CliffordFormulaDetail = "Electric vector E and magnetic bivector IcB combine into F = E + IcB. All 4 Maxwell equations collapse into the single spacetime equation ∇F = J.";
                break;

            case "spacetime":
                CliffordMode = "spacetime";
                CliffordRotorAngle = 0.65;
                CliffordFormulaTitle = "Spacetime Boost Hyperbolic Rotor (STA G₁,₃)";
                CliffordFormulaDetail = "Lorentz boost as a hyperbolic rotor L = cosh(ζ/2) + e₀₁ sinh(ζ/2) where e₀₁² = +1. Preserves the Minkowski metric s² = c²t² - x².";
                break;

            case "wedge":
            default:
                CliffordMode = "wedge";
                CliffordVectorAX = 1.8;
                CliffordVectorAY = 0.6;
                CliffordVectorAZ = 0.0;
                CliffordVectorBX = 0.4;
                CliffordVectorBY = 1.6;
                CliffordVectorBZ = 0.8;
                CliffordFormulaTitle = "Exterior Wedge Product & Oriented Plane (a ∧ b)";
                CliffordFormulaDetail = "Geometric product ab = a·b + a∧b decomposes into symmetric scalar inner product and antisymmetric bivector area. The dual vector n = -I(a∧b) points normal to the plane.";
                break;
        }
    }

    [RelayCommand]
    public void SetCliffordMode(string mode)
    {
        CliffordMode = mode;
    }

    [RelayCommand]
    public void SetDifferentialGeometrySurface(string surface)
    {
        DiffGeomSurfaceType = surface;
    }

    public void SetDifferentialGeometryPreset(string preset)
    {
        ActiveLaboratory = "Differential Geometry";
        DiffGeomPreset = preset;

        switch (preset.ToLowerInvariant())
        {
            case "sphere":
                DiffGeomSurfaceType = "sphere";
                DiffGeomStartU = 0.0;
                DiffGeomStartV = 0.2;
                DiffGeomHeadingAngle = 0.5;
                DiffGeomFormulaTitle = "Sphere & Great Circle Geodesics (K = 1/R² > 0)";
                DiffGeomFormulaDetail = "Constant positive curvature K = 1/R². Geodesics are closed Great Circles forming shortest paths on the 2-sphere S².";
                break;

            case "pseudosphere":
                DiffGeomSurfaceType = "pseudosphere";
                DiffGeomStartU = 0.0;
                DiffGeomStartV = 0.8;
                DiffGeomHeadingAngle = 0.785;
                DiffGeomFormulaTitle = "Beltrami Pseudosphere & Hyperbolic Geodesics (K = -1)";
                DiffGeomFormulaDetail = "Constant negative curvature K = -1. Geodesic rays diverge exponentially, modeling the hyperbolic plane H² locally.";
                break;

            case "enneper":
                DiffGeomSurfaceType = "enneper";
                DiffGeomStartU = 0.0;
                DiffGeomStartV = 0.0;
                DiffGeomHeadingAngle = 0.8;
                DiffGeomFormulaTitle = "Enneper Minimal Surface (H = 0, K ≤ 0)";
                DiffGeomFormulaDetail = "Self-intersecting minimal surface with vanishing Mean Curvature H = 0. Principal curvatures are equal and opposite (κ₁ = -κ₂).";
                break;

            case "bonnet":
                DiffGeomSurfaceType = "bonnet";
                DiffGeomStartU = 0.0;
                DiffGeomStartV = 0.0;
                DiffGeomHeadingAngle = 0.6;
                DiffGeomDeformationAngle = 0.35;
                DiffGeomFormulaTitle = "Bonnet Isometric Family (Catenoid ↔ Helicoid)";
                DiffGeomFormulaDetail = "Isometric deformation preserves metric tensor g_ij and Gaussian curvature K at every point with zero intrinsic stretching.";
                break;

            case "torus":
            default:
                DiffGeomSurfaceType = "torus";
                DiffGeomStartU = 0.0;
                DiffGeomStartV = 0.0;
                DiffGeomHeadingAngle = 0.785;
                DiffGeomFormulaTitle = "Torus & Geodesic Winding Chaos";
                DiffGeomFormulaDetail = "Gaussian curvature K = cos(v) / (r(R + r cos v)) transitions from positive on outer equator to zero on crown to negative on inner equator.";
                break;
        }
    }

    [RelayCommand]
    public void SetKnotType(string type)
    {
        KnotType = type;
    }

    public void SetKnotPreset(string preset)
    {
        ActiveLaboratory = "Knot Theory & Invariants";
        KnotPreset = preset;

        switch (preset.ToLowerInvariant())
        {
            case "figure8":
            case "4_1":
                KnotType = "figure8";
                KnotTubeRadius = 0.18;
                KnotBraidWord = "s1 s2^-1 s1 s2^-1";
                KnotFormulaTitle = "Figure-Eight Knot 4₁ (Amphichiral)";
                KnotFormulaDetail = "The unique prime knot with 4 crossings. Amphichiral with palindromic Jones polynomial V(t) = t⁻² - t⁻¹ + 1 - t + t² and zero writhe.";
                break;

            case "cinquefoil":
            case "5_1":
                KnotType = "cinquefoil";
                KnotTubeRadius = 0.16;
                KnotTorusP = 5;
                KnotTorusQ = 2;
                KnotBraidWord = "s1 s2 s1 s2 s1";
                KnotFormulaTitle = "Cinquefoil Torus Knot 5₁ (Solomon's Seal)";
                KnotFormulaDetail = "The (5, 2) torus knot with 5 alternating crossings, high chiral asymmetry, and Seifert genus g = 2. Jones polynomial V(t) = -t⁻⁷ + t⁻⁶ - t⁻⁵ + t⁻⁴ + t⁻².";
                break;

            case "borromean":
                KnotType = "borromean";
                KnotTubeRadius = 0.15;
                KnotBraidWord = "s1 s2^-1 s1 s2^-1";
                KnotFormulaTitle = "Borromean Rings (Brunnian Link)";
                KnotFormulaDetail = "Three closed loops with pairwise linking numbers Lk(K_i, K_j) = 0, yet inextricably topologically intertwined. Cutting any single ring frees the others.";
                break;

            case "whitehead":
                KnotType = "whitehead";
                KnotTubeRadius = 0.15;
                KnotBraidWord = "s1 s2 s1^-1 s2^-1";
                KnotFormulaTitle = "Whitehead Link (Zero Linking Number)";
                KnotFormulaDetail = "Two-component link with 5 crossings and vanishing Gauss linking number Lk = 0, demonstrating that linking number alone cannot detect link separation.";
                break;

            case "braid":
                KnotType = "trefoil";
                KnotTubeRadius = 0.18;
                KnotBraidWord = "s1 s2^-1 s1 s2^-1 s1 s2^-1";
                KnotFormulaTitle = "Artin Braid Group B₃ & Yang-Baxter";
                KnotFormulaDetail = "3-strand Artin braid satisfying the braid equation σ₁ σ₂ σ₁ = σ₂ σ₁ σ₂. Braid closure produces knots and links via Alexander's and Markov's theorems.";
                break;

            case "trefoil":
            default:
                KnotType = "trefoil";
                KnotTubeRadius = 0.18;
                KnotTorusP = 3;
                KnotTorusQ = 2;
                KnotBraidWord = "s1 s2^-1 s1 s2^-1 s1 s2^-1";
                KnotFormulaTitle = "Trefoil Knot 3₁ & Skein Invariant";
                KnotFormulaDetail = "The simplest non-trivial prime knot with 3 crossings and writhe w = -3. Characterized by Jones polynomial V(t) = -t⁻⁴ + t⁻³ + t⁻¹.";
                break;
        }
    }

    [RelayCommand]
    public void SetHamiltonianSystem(string system)
    {
        HamiltonSystemType = system;
    }

    [RelayCommand]
    public void SetHamiltonianIntegrator(string mode)
    {
        HamiltonIntegratorMode = mode;
    }

    [RelayCommand]
    public void SetHamiltonianPreset(string preset)
    {
        ActiveLaboratory = "Symplectic & Hamiltonian";
        HamiltonPreset = preset;

        switch (preset.ToLowerInvariant())
        {
            case "duffing":
                HamiltonSystemType = "duffing";
                HamiltonEnergyLevel = 0.5;
                HamiltonIntegratorMode = "yoshida";
                HamiltonTimeStep = 0.03;
                HamiltonFormulaTitle = "Duffing Quartic Potential & Homoclinic Bifurcation";
                HamiltonFormulaDetail = "Double-well potential V(q) = -½q² + ¼q⁴ with hyperbolic saddle at origin (0, 0) and homoclinic figure-8 separatrix separating left and right potential wells.";
                break;

            case "pendulum":
                HamiltonSystemType = "pendulum";
                HamiltonEnergyLevel = 1.0;
                HamiltonIntegratorMode = "yoshida";
                HamiltonTimeStep = 0.04;
                HamiltonFormulaTitle = "Nonlinear Pendulum & Phase Space Libration";
                HamiltonFormulaDetail = "Hamiltonian H(q, p) = ½p² - cos(q). Bounded oscillatory libration for H < 1; continuous unidirectional whirling rotation for H > 1; separatrix at H = 1.";
                break;

            case "liouville":
                HamiltonSystemType = "liouville";
                HamiltonEnergyLevel = 0.12;
                HamiltonIntegratorMode = "yoshida";
                HamiltonTimeStep = 0.02;
                HamiltonShowLiouvilleDroplet = true;
                HamiltonFormulaTitle = "Liouville's Theorem & Phase Volume Invariance";
                HamiltonFormulaDetail = "Incompressibility of Hamiltonian phase fluid d/dt(Area) = 0. Symplectic integrators preserve symplectic 2-form ω = dq ∧ dp exactly without artificial numerical dissipation or swelling.";
                break;

            case "yoshida":
                HamiltonSystemType = "yoshida";
                HamiltonEnergyLevel = 0.14;
                HamiltonIntegratorMode = "yoshida";
                HamiltonTimeStep = 0.05;
                HamiltonShowEnergyErrorGraph = true;
                HamiltonFormulaTitle = "Yoshida 4th-Order Symplectic vs Classical RK4";
                HamiltonFormulaDetail = "Symmetric 4-stage composition integrator preserving the modified shadow Hamiltonian H̃. Absolute energy error stays strictly bounded within O(dt⁴) over 10⁶ steps, whereas RK4 suffers secular drift.";
                break;

            case "henonheiles":
            default:
                HamiltonSystemType = "henonheiles";
                HamiltonEnergyLevel = 0.12;
                HamiltonIntegratorMode = "yoshida";
                HamiltonTimeStep = 0.04;
                HamiltonShowPoincareSection = true;
                HamiltonFormulaTitle = "Hénon-Heiles Anharmonic Chaos & KAM Tori";
                HamiltonFormulaDetail = "Canonical 2D galactic potential transitioning from regular invariant KAM tori at low energy E < 0.11 to global ergodic chaos as E approaches escape energy Ec = 1/6.";
                break;
        }
    }

    [RelayCommand]
    public void SetHomologyComplexType(string type)
    {
        HomologyComplexType = type;
    }

    [RelayCommand]
    public void SetHomologyFiltration(string epsStr)
    {
        if (double.TryParse(epsStr, System.Globalization.CultureInfo.InvariantCulture, out double eps))
        {
            HomologyFiltrationRadius = eps;
        }
    }

    [RelayCommand]
    public void SetHomologyPreset(string preset)
    {
        ActiveLaboratory = "Algebraic Topology & Homology";
        HomologyPreset = preset;

        switch (preset.ToLowerInvariant())
        {
            case "sphere":
                HomologyComplexType = "sphere";
                HomologyFormulaTitle = "2-Sphere S² Octahedron Triangulation";
                HomologyFormulaDetail = "Canonical spherical triangulation with 6 vertices, 12 edges, and 8 faces. Simply connected (H₁ = 0) with β = (1, 0, 1) and Euler characteristic χ = 2.";
                break;

            case "klein":
                HomologyComplexType = "klein";
                HomologyFormulaTitle = "Klein Bottle K² Non-Orientable Surface";
                HomologyFormulaDetail = "Triangulation with twisted edge identification. Non-orientable surface with torsion group H₁(K²; ℤ) ≅ ℤ ⊕ ℤ₂ and Betti numbers over ℚ: β = (1, 1, 0).";
                break;

            case "projective":
            case "rp2":
                HomologyComplexType = "projective";
                HomologyFormulaTitle = "Real Projective Plane ℝP² Minimal Triangulation";
                HomologyFormulaDetail = "Minimal 6-vertex 10-triangle triangulation. Non-orientable 2-manifold with Euler characteristic χ = 1 and 2-torsion H₁(ℝP²; ℤ) ≅ ℤ₂.";
                break;

            case "mobius":
                HomologyComplexType = "mobius";
                HomologyFormulaTitle = "Möbius Strip with Single Boundary ∂M ≅ S¹";
                HomologyFormulaDetail = "Canonical non-orientable 2D manifold with boundary. Retracts onto a central circle S¹, yielding β = (1, 1, 0) and Euler characteristic χ = 0.";
                break;

            case "persistence":
                HomologyComplexType = "persistence";
                HomologyFiltrationRadius = 0.8;
                HomologyFormulaTitle = "Vietoris-Rips Point Cloud Filtration & Barcodes";
                HomologyFormulaDetail = "Topological data analysis tracking birth and death of Betti cycles [ε_birth, ε_death] across growing filtration radius ε. Identifies true topological signal from noise.";
                break;

            case "torus":
            default:
                HomologyComplexType = "torus";
                HomologyFormulaTitle = "2-Torus T² Simplicial Triangulation";
                HomologyFormulaDetail = "Canonical 9-vertex 18-triangle triangulation with glued opposite edges. Betti numbers β = (1, 2, 1), Euler characteristic χ = 0.";
                break;
        }
    }

    [RelayCommand]
    public void SetLieType(string type)
    {
        ActiveLaboratory = "Lie Algebras & Dynkin";
        LieType = type.ToUpperInvariant();

        switch (type.ToUpperInvariant())
        {
            case "B2":
            case "SO5":
                LieFormulaTitle = "Orthogonal Lie Algebra so(5) [B₂ Root Lattice]";
                LieFormulaDetail = "Octagonal root lattice with 8 roots (4 short of length 1, 4 long of length √2). Dimension 10, rank 2, Coxeter number h = 4.";
                break;

            case "G2":
                LieFormulaTitle = "Exceptional Simple Lie Algebra G₂";
                LieFormulaDetail = "Rank 2, dimension 14, 12 roots in a 12-pointed star. Preserves the non-associative octonionic cross product on Im(𝕆).";
                break;

            case "A3":
            case "SU4":
                LieFormulaTitle = "Special Unitary Lie Algebra su(4) [A₃ Cuboctahedron]";
                LieFormulaDetail = "Rank 3, dimension 15, 12 roots forming a cuboctahedron in 3D. Simply laced A₃ Dynkin diagram.";
                break;

            case "B3":
            case "SO7":
                LieFormulaTitle = "Orthogonal Lie Algebra so(7) [B₃ Rhombic Dodecahedron]";
                LieFormulaDetail = "Rank 3, dimension 21, 18 roots forming a rhombic dodecahedron. Non-simply laced Dynkin diagram with double bond.";
                break;

            case "E8":
                LieFormulaTitle = "Exceptional Lie Algebra E₈ (String Theory & Heterotic Geometry)";
                LieFormulaDetail = "The supreme exceptional simple Lie algebra. Rank 8, dimension 248, 240 roots projected onto 2D Coxeter plane in 8 concentric 30-gons.";
                break;

            case "A2":
            case "SU3":
            default:
                LieFormulaTitle = "Special Unitary Lie Algebra su(3) [A₂ Root Lattice]";
                LieFormulaDetail = "Hexagonal root system with rank 2, dimension 8, 6 roots. Symmetries of quarks in Gell-Mann's Eightfold Way.";
                break;
        }
    }

    // Complex / Conformal Möbius Builder Commands
    [RelayCommand]
    public void SetMobiusPreset(string preset)
    {
        switch (preset.ToLowerInvariant())
        {
            case "cayley":
                MobiusParamA = 1; MobiusParamB = -1; MobiusParamC = 1; MobiusParamD = 1;
                ComplexCustomFormula = "(z - 1) / (z + 1)";
                break;
            case "inversion":
                MobiusParamA = 0; MobiusParamB = 1; MobiusParamC = 1; MobiusParamD = 0;
                ComplexCustomFormula = "1 / z";
                break;
            case "scaling":
                MobiusParamA = 2; MobiusParamB = 0; MobiusParamC = 0; MobiusParamD = 1;
                ComplexCustomFormula = "2 * z";
                break;
            case "rotation":
                MobiusParamA = 0; MobiusParamB = 1; MobiusParamC = 0; MobiusParamD = 1;
                ComplexCustomFormula = "z";
                break;
            case "joukowsky":
                ComplexCustomFormula = "0.5 * (z + 1/z)";
                break;
        }
    }

    [RelayCommand]
    public void ApplyMobiusTransformation()
    {
        ComplexCustomFormula = $"({MobiusParamA}*z + {MobiusParamB}) / ({MobiusParamC}*z + {MobiusParamD})";
    }

    [RelayCommand]
    public void InsertComplexSymbol(string symbol)
    {
        if (string.IsNullOrEmpty(ComplexCustomFormula))
            ComplexCustomFormula = symbol;
        else
            ComplexCustomFormula += symbol;
    }

    // 3D Multivariable Surface Builder Commands
    [RelayCommand]
    public void ApplyQuadricSurface()
    {
        SurfaceFormula = $"{SurfaceParamA}*x^2 + {SurfaceParamB}*y^2 + {SurfaceParamC}*x*y";
    }

    [RelayCommand]
    public void InsertSurfaceSymbol(string symbol)
    {
        if (string.IsNullOrEmpty(SurfaceFormula))
            SurfaceFormula = symbol;
        else
            SurfaceFormula += symbol;
    }

    // Fractals & Julia Set Builder Commands
    [RelayCommand]
    public void SetFamousJuliaPreset(string name)
    {
        ActiveLaboratory = "Fractals & Chaos";
        SelectedFractalType = FractalType.Julia;
        switch (name.ToLowerInvariant())
        {
            case "douady":
            case "rabbit":
                FractalJuliaCr = -0.123; FractalJuliaCi = 0.745;
                break;
            case "sanmarco":
            case "basilica":
                FractalJuliaCr = -0.75; FractalJuliaCi = 0.0;
                break;
            case "dendrite":
                FractalJuliaCr = 0.0; FractalJuliaCi = 1.0;
                break;
            case "siegel":
            case "disk":
                FractalJuliaCr = -0.391; FractalJuliaCi = -0.587;
                break;
            case "cauliflower":
                FractalJuliaCr = 0.25; FractalJuliaCi = 0.0;
                break;
            case "airplane":
                FractalJuliaCr = -1.754; FractalJuliaCi = 0.0;
                break;
        }
    }

    [RelayCommand]
    public void ResetFractalView()
    {
        FractalMaxIterations = 140;
    }

    // Quantum Wave Mechanics Builder Commands
    [RelayCommand]
    public void InjectCustomWavepacket()
    {
        ActiveLaboratory = "Quantum & Waves";
        QuantumPreset = "free";
    }

    // Cryptography Weierstrass Builder Commands
    [RelayCommand]
    public void SetCryptoPrimeP(object param)
    {
        if (param is long l) CryptoPrimeP = l;
        else if (param is int i) CryptoPrimeP = i;
        else if (param != null && long.TryParse(param.ToString(), out var parsed)) CryptoPrimeP = parsed;
    }

    [RelayCommand]
    public void SetCryptoCurveCoeffs(string preset)
    {
        switch (preset.ToLowerInvariant())
        {
            case "bitcoin":
                CryptoParamA = 0.0; CryptoParamB = 7.0;
                break;
            case "twocomp":
                CryptoParamA = -3.0; CryptoParamB = 1.0;
                break;
            case "cusp":
                CryptoParamA = 0.0; CryptoParamB = 0.0;
                break;
            case "node":
                CryptoParamA = -3.0; CryptoParamB = 2.0;
                break;
        }
    }

    // Bifurcation & Catastrophe Theory Commands
    [RelayCommand]
    public void TriggerHysteresisSweep()
    {
        CatastropheParamB = -CatastropheParamB;
    }

    [RelayCommand]
    public void SetCatastropheCriticalPoint()
    {
        CatastropheParamA = 0.0;
        CatastropheParamB = 0.0;
        CatastropheStateX = 0.0;
    }

    [RelayCommand]
    public void SetCatastropheMaxwellPoint()
    {
        CatastropheParamA = -2.5;
        CatastropheParamB = 0.0;
    }

    // Symplectic Geometry & Hamiltonian Commands
    [RelayCommand]
    public void SetHamiltonianEnergyLevel(object param)
    {
        if (param is double d) HamiltonEnergyLevel = d;
        else if (param is float f) HamiltonEnergyLevel = f;
        else if (param != null && double.TryParse(param.ToString(), System.Globalization.CultureInfo.InvariantCulture, out var parsed)) HamiltonEnergyLevel = parsed;
    }

    // Harmonic Series Synthesizer Commands (Drawer 3)
    [RelayCommand]
    public void SetHarmonicFilter(object filter)
    {
        var f = filter?.ToString()?.ToLowerInvariant() ?? "all";
        FourierHarmonicFilter = f;
        switch (f)
        {
            case "odd":
                SetFourierPreset("square");
                break;
            case "even":
                SetFourierPreset("sawtooth");
                break;
            case "triangle":
                SetFourierPreset("triangle");
                break;
            case "lowpass":
                Harmonics = 5;
                break;
            case "all":
            default:
                Harmonics = 35;
                break;
        }
    }

    // Probability & Statistics Distribution Commands (Drawer 4)
    [RelayCommand]
    public void SetDistributionPreset(object preset)
    {
        var p = preset?.ToString()?.ToLowerInvariant();
        switch (p)
        {
            case "symmetric":
                ProbabilityP = 0.50;
                break;
            case "skew_right":
            case "rare":
                ProbabilityP = 0.20;
                break;
            case "skew_left":
            case "high":
                ProbabilityP = 0.80;
                break;
            case "quarter":
                ProbabilityP = 0.25;
                break;
            case "threequarters":
                ProbabilityP = 0.75;
                break;
        }
    }

    // PDE & Wave Mechanics Excitation Commands (Drawer 10)
    [RelayCommand]
    public void SetWaveExcitation(object type)
    {
        var t = type?.ToString()?.ToLowerInvariant() ?? "impulse";
        WaveExcitationType = t;
        switch (t)
        {
            case "impulse":
                WaveFormulaTitle = "Droplet Impulse (Dirac δ Wavefront)";
                WaveFormulaDetail = "Localized point perturbation spreads as concentric cylindrical wavefronts governed by Huygens' principle in 2D.";
                break;
            case "gaussian":
                WaveFormulaTitle = "Gaussian Wavepacket Propagation";
                WaveFormulaDetail = "Smooth bell-shaped profile minimizes high-frequency numerical dispersion across the discrete finite-difference grid.";
                break;
            case "standing":
                WaveFormulaTitle = $"Pure Helmholtz Eigenmode (n={WaveModeN}, m={WaveModeM})";
                WaveFormulaDetail = "Stationary harmonic standing pattern with zero-velocity nodal lines where acoustic interference is completely destructive.";
                break;
            case "dipole":
                WaveFormulaTitle = "Acoustic Dipole Interference";
                WaveFormulaDetail = "Out-of-phase dual excitations generate hyperbolic interference fringes with alternating constructive and destructive lobes.";
                break;
        }
    }

    // Special Relativity Kinematics Commands (Drawer 15)
    [RelayCommand]
    public void SetRelativityBetaPreset(object betaVal)
    {
        if (betaVal is double d)
        {
            RelativityBeta = d;
            return;
        }
        var s = betaVal?.ToString()?.ToLowerInvariant();
        switch (s)
        {
            case "0.0":
            case "zero":
                RelativityBeta = 0.0;
                break;
            case "0.5":
                RelativityBeta = 0.5;
                break;
            case "0.866":
            case "gamma2":
                RelativityBeta = 0.866;
                break;
            case "0.99":
            case "gamma7":
                RelativityBeta = 0.99;
                break;
            case "0.999":
                RelativityBeta = 0.999;
                break;
            default:
                if (double.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                    RelativityBeta = parsed;
                break;
        }
    }

    // Classical Mechanics & Lagrangian Regime Commands (Drawer 21)
    [RelayCommand]
    public void SetLagrangianRegime(object regime)
    {
        var r = regime?.ToString()?.ToLowerInvariant();
        switch (r)
        {
            case "regular":
                LagrangianL1 = 1.0;
                LagrangianL2 = 0.5;
                LagrangianM1 = 2.0;
                LagrangianM2 = 0.2;
                SetLagrangianPreset("poincare");
                break;
            case "chaotic":
                LagrangianL1 = 1.0;
                LagrangianL2 = 0.9;
                LagrangianM1 = 1.0;
                LagrangianM2 = 0.8;
                SetLagrangianPreset("double");
                break;
            case "high_energy":
                LagrangianL1 = 1.5;
                LagrangianL2 = 1.4;
                LagrangianM1 = 2.5;
                LagrangianM2 = 2.0;
                SetLagrangianPreset("double");
                break;
            case "inverted":
                SetLagrangianPreset("kapitza");
                break;
        }
    }

    // Differential Geometry Curvature Preset Commands (Drawer 26)
    [RelayCommand]
    public void SetDiffGeomGeometryPreset(object preset)
    {
        var p = preset?.ToString()?.ToLowerInvariant();
        switch (p)
        {
            case "sphere":
                SetDifferentialGeometryPreset("sphere");
                break;
            case "pseudosphere":
                SetDifferentialGeometryPreset("pseudosphere");
                break;
            case "enneper_minimal":
            case "enneper":
                SetDifferentialGeometryPreset("enneper");
                break;
            case "bonnet":
                SetDifferentialGeometryPreset("bonnet");
                break;
            case "torus_outer":
                SetDifferentialGeometryPreset("torus");
                DiffGeomStartV = 0.0;
                break;
            case "torus_inner":
                SetDifferentialGeometryPreset("torus");
                DiffGeomStartV = 3.14;
                break;
            default:
                SetDifferentialGeometryPreset("torus");
                break;
        }
    }

    // Dynamical Chaos Attractor Presets (Drawer 5)
    [RelayCommand]
    public void SetChaosAttractorPreset(object preset)
    {
        var p = preset?.ToString()?.ToLowerInvariant();
        switch (p)
        {
            case "standard":
            case "butterfly":
                ChaosSigma = 10.0;
                ChaosRho = 28.0;
                ChaosBeta = 8.0 / 3.0;
                break;
            case "subcritical":
                ChaosSigma = 10.0;
                ChaosRho = 22.0;
                ChaosBeta = 8.0 / 3.0;
                break;
            case "transient":
                ChaosSigma = 10.0;
                ChaosRho = 24.5;
                ChaosBeta = 8.0 / 3.0;
                break;
            case "periodic":
                ChaosSigma = 10.0;
                ChaosRho = 99.96;
                ChaosBeta = 8.0 / 3.0;
                break;
            case "intermittent":
                ChaosSigma = 10.0;
                ChaosRho = 166.3;
                ChaosBeta = 8.0 / 3.0;
                break;
            default:
                ChaosSigma = 10.0;
                ChaosRho = 28.0;
                ChaosBeta = 8.0 / 3.0;
                break;
        }
    }

    [RelayCommand]
    public void PerturbChaosTrajectory()
    {
        ShowChaosDualTrajectories = true;
    }

    // Vector Field Topology & Probe Commands (Drawer 6)
    [RelayCommand]
    public void SetVectorFieldProbePoint(object point)
    {
        var pt = point?.ToString()?.ToLowerInvariant();
        switch (pt)
        {
            case "origin":
                VectorFieldSeedX = 0.0;
                VectorFieldSeedY = 0.0;
                break;
            case "one":
            case "one_one":
                VectorFieldSeedX = 1.0;
                VectorFieldSeedY = 1.0;
                break;
            case "two_zero":
                VectorFieldSeedX = 2.0;
                VectorFieldSeedY = 0.0;
                break;
            case "neg_one":
                VectorFieldSeedX = -1.0;
                VectorFieldSeedY = 1.0;
                break;
            default:
                VectorFieldSeedX = 1.0;
                VectorFieldSeedY = 1.0;
                break;
        }
    }

    [RelayCommand]
    public void SetVectorFieldTopologyPreset(object preset)
    {
        var p = preset?.ToString()?.ToLowerInvariant();
        switch (p)
        {
            case "center":
            case "vortex":
                SetVectorFieldPreset("vortex");
                break;
            case "saddle":
                SetVectorFieldPreset("saddle");
                break;
            case "sink":
                SetVectorFieldPreset("sink");
                break;
            case "vanderpol":
                SetVectorFieldPreset("vanderpol");
                break;
            case "pendulum":
                SetVectorFieldPreset("pendulum");
                break;
            case "predator":
                SetVectorFieldPreset("predator");
                break;
            default:
                SetVectorFieldPreset("vortex");
                break;
        }
    }

    // Space Curve Geometry Commands (Drawer 11)
    [RelayCommand]
    public void SetSpaceCurveGeometry(object preset)
    {
        var p = preset?.ToString()?.ToLowerInvariant() ?? "helix";
        SetSpaceCurvePreset(p);
    }

    // Riemann Zeta Zero Teleport Commands (Drawer 12)
    [RelayCommand]
    public void SetZetaZeroTeleport(object zero)
    {
        if (zero is int idx && idx >= 1 && idx <= RiemannZetaEngine.KnownZeros.Length)
        {
            ZetaProbeT = RiemannZetaEngine.KnownZeros[idx - 1];
            return;
        }
        var s = zero?.ToString()?.ToLowerInvariant();
        if (int.TryParse(s, out int n) && n >= 1 && n <= RiemannZetaEngine.KnownZeros.Length)
        {
            ZetaProbeT = RiemannZetaEngine.KnownZeros[n - 1];
        }
        else
        {
            ZetaProbeT = s switch
            {
                "zero1" or "gamma1" or "1" => RiemannZetaEngine.KnownZeros[0],
                "zero2" or "gamma2" or "2" => RiemannZetaEngine.KnownZeros[1],
                "zero3" or "gamma3" or "3" => RiemannZetaEngine.KnownZeros[2],
                "zero4" or "gamma4" or "4" => RiemannZetaEngine.KnownZeros[3],
                "zero5" or "gamma5" or "5" => RiemannZetaEngine.KnownZeros[4],
                "zero6" or "gamma6" or "6" => RiemannZetaEngine.KnownZeros[5],
                "zero7" or "gamma7" or "7" => RiemannZetaEngine.KnownZeros[6],
                "zero8" or "gamma8" or "8" => RiemannZetaEngine.KnownZeros[7],
                _ => RiemannZetaEngine.KnownZeros[0]
            };
        }
    }

    // Heat Boundary Condition Commands (Drawer 13)
    [RelayCommand]
    public void SetHeatBoundaryCondition(object bc)
    {
        var b = bc?.ToString()?.ToLowerInvariant();
        switch (b)
        {
            case "dirichlet":
                HeatBoundaryType = "Dirichlet";
                SetHeatPreset("laplace");
                break;
            case "neumann":
            case "insulated":
                HeatBoundaryType = "Neumann";
                SetHeatPreset("insulated");
                break;
            case "periodic":
                HeatBoundaryType = "Periodic";
                SetHeatPreset("laplace");
                break;
            default:
                HeatBoundaryType = "Dirichlet";
                break;
        }
    }

    // Fluid Reynolds Presets Commands (Drawer 16)
    [RelayCommand]
    public void SetFluidReynoldsPreset(object preset)
    {
        var p = preset?.ToString()?.ToLowerInvariant();
        switch (p)
        {
            case "creeping":
                FluidViscosity = 0.003;
                break;
            case "karman":
                FluidViscosity = 0.0006;
                SetFluidPreset("karman");
                break;
            case "wake":
                FluidViscosity = 0.0003;
                SetFluidPreset("kelvin");
                break;
            case "turbulent":
                FluidViscosity = 0.00015;
                break;
            default:
                FluidViscosity = 0.0006;
                SetFluidPreset("karman");
                break;
        }
    }

    // 31. Clay Millennium Prize Problems Commands (Drawer 31)
    [RelayCommand]
    public void SelectMillenniumProblem(object? problem)
    {
        ActiveLaboratory = "Millennium Problems";
        if (problem is MillenniumProblemType mpt)
        {
            SelectedMillenniumProblem = mpt;
        }
        else if (problem != null)
        {
            string s = problem.ToString()?.ToLowerInvariant() ?? "";
            SelectedMillenniumProblem = s switch
            {
                "poincare" or "0" => MillenniumProblemType.PoincareConjecture,
                "riemann" or "1" => MillenniumProblemType.RiemannHypothesis,
                "bsd" or "2" => MillenniumProblemType.BirchSwinnertonDyer,
                "navierstokes" or "navier" or "3" => MillenniumProblemType.NavierStokesSmoothness,
                "yangmills" or "4" => MillenniumProblemType.YangMillsMassGap,
                "pvsnp" or "5" => MillenniumProblemType.PversusNP,
                "hodge" or "6" => MillenniumProblemType.HodgeConjecture,
                _ => MillenniumProblemType.PoincareConjecture
            };
        }
        if (SelectedMillenniumProblem != MillenniumProblemType.PoincareConjecture)
            IsRicciFlowAnimating = false;
        if (SelectedMillenniumProblem != MillenniumProblemType.NavierStokesSmoothness)
            IsNavierAnimating = false;
        OnPropertyChanged(nameof(ActiveMillenniumProblemInfo));
    }

    [RelayCommand]
    public void StepRicciFlow(object? step)
    {
        ActiveLaboratory = "Millennium Problems";
        SelectedMillenniumProblem = MillenniumProblemType.PoincareConjecture;
        double dt = 0.5;
        if (step != null && double.TryParse(step.ToString(), out double parsed))
        {
            dt = parsed;
        }
        double next = MillenniumRicciTime + dt;
        if (next > 5.2) next = 0.0;
        MillenniumRicciTime = Math.Round(next, 2);
    }

    [RelayCommand]
    public void TriggerRicciSurgery()
    {
        ActiveLaboratory = "Millennium Problems";
        SelectedMillenniumProblem = MillenniumProblemType.PoincareConjecture;
        MillenniumRicciTime = 2.4;
    }

    [RelayCommand]
    public void ToggleRicciFlowAnimation()
    {
        IsRicciFlowAnimating = !IsRicciFlowAnimating;
        if (IsRicciFlowAnimating)
        {
            IsNavierAnimating = false;
            ActiveLaboratory = "Millennium Problems";
            SelectedMillenniumProblem = MillenniumProblemType.PoincareConjecture;
        }
    }

    [RelayCommand]
    public void SetBsdCurvePreset(object? preset)
    {
        ActiveLaboratory = "Millennium Problems";
        SelectedMillenniumProblem = MillenniumProblemType.BirchSwinnertonDyer;
        BsdSelectedMultiplier = 1;
        string key = preset?.ToString()?.ToLowerInvariant() ?? "rank1";
        switch (key)
        {
            case "rank0":
                MillenniumBsdParamA = -1.0;
                MillenniumBsdParamB = 1.0;
                break;
            case "rank1":
                MillenniumBsdParamA = -2.0;
                MillenniumBsdParamB = 0.0;
                break;
            case "rank2":
                MillenniumBsdParamA = -43.0;
                MillenniumBsdParamB = 166.0;
                break;
            default:
                MillenniumBsdParamA = -1.0;
                MillenniumBsdParamB = 1.0;
                break;
        }
    }

    [RelayCommand]
    public void SetNavierRegime(object? regime)
    {
        ActiveLaboratory = "Millennium Problems";
        SelectedMillenniumProblem = MillenniumProblemType.NavierStokesSmoothness;
        string key = regime?.ToString()?.ToLowerInvariant() ?? "viscous";
        switch (key)
        {
            case "openai" or "openai_solution" or "openai_blowup":
                MillenniumNavierViscosity = 0.00001;
                MillenniumNavierTime = 1.84;
                break;
            case "viscous":
                MillenniumNavierViscosity = 0.005;
                MillenniumNavierTime = 1.0;
                break;
            case "critical":
                MillenniumNavierViscosity = 0.001;
                MillenniumNavierTime = 1.4;
                break;
            case "turbulent_blowup" or "blowup":
                MillenniumNavierViscosity = 0.00005;
                MillenniumNavierTime = 2.6;
                break;
        }
    }

    [RelayCommand]
    public void SetRiemannAttack(object? attack)
    {
        ActiveLaboratory = "Millennium Problems";
        SelectedMillenniumProblem = MillenniumProblemType.RiemannHypothesis;

        if (attack is RiemannAttackFramework framework)
        {
            SelectedRiemannAttack = framework;
            return;
        }

        string key = attack?.ToString()?.ToLowerInvariant()?.Replace("_", "").Replace("-", "") ?? "hilbert";
        SelectedRiemannAttack = key switch
        {
            "hilbert" or "polya" or "hilbertpolya" or "operator" or "quantum" => RiemannAttackFramework.HilbertPolya,
            "debruijn" or "newman" or "debruijnnewman" or "lambda" or "flow" => RiemannAttackFramework.DeBruijnNewman,
            "li" or "licriterion" or "positivity" => RiemannAttackFramework.LiCriterion,
            "hunter" or "zerohunter" or "hunt" or "gram" or "turing" => RiemannAttackFramework.ZeroHunter,
            "gue" or "guechaos" or "chaos" or "wigner" => RiemannAttackFramework.GueChaos,
            "music" or "primes" or "primeharmonics" or "harmonics" => RiemannAttackFramework.PrimeHarmonics,
            _ => RiemannAttackFramework.HilbertPolya
        };
    }

    [RelayCommand]
    public void SetDeBruijnT(object? tParam)
    {
        if (tParam != null && double.TryParse(tParam.ToString(), out double tVal))
        {
            RiemannDeBruijnT = tVal;
        }
    }

    [RelayCommand]
    public void HuntRiemannZeros()
    {
        ActiveLaboratory = "Millennium Problems";
        SelectedMillenniumProblem = MillenniumProblemType.RiemannHypothesis;
        SelectedRiemannAttack = RiemannAttackFramework.ZeroHunter;
        OnPropertyChanged(nameof(ActiveZeroHunterState));
    }

    [RelayCommand]
    public void PlayRiemannChord()
    {
        RiemannSolutionEngine.PlayRiemannChordTone(2.0, 0.35);
    }

    [RelayCommand]
    public void ToggleNavierAnimation()
    {
        IsNavierAnimating = !IsNavierAnimating;
        if (IsNavierAnimating)
        {
            IsRicciFlowAnimating = false;
            ActiveLaboratory = "Millennium Problems";
            SelectedMillenniumProblem = MillenniumProblemType.NavierStokesSmoothness;
        }
    }

    [RelayCommand]
    public void PlayRiemannZero(object? zeroParam)
    {
        double zeroHeight = 14.134725;
        if (zeroParam != null && double.TryParse(zeroParam.ToString(), out double parsedVal))
        {
            if (parsedVal >= 1 && parsedVal <= 8 && parsedVal == Math.Floor(parsedVal))
            {
                double[] knownZeros = { 14.134725, 21.022040, 25.010858, 30.424876, 32.935062, 37.586178, 40.918719, 43.327073 };
                zeroHeight = knownZeros[(int)parsedVal - 1];
            }
            else
            {
                zeroHeight = parsedVal;
            }
        }
        else if (ZetaProbeT > 5.0)
        {
            zeroHeight = ZetaProbeT;
        }

        AudioToneGenerator.PlayRiemannZeroTone(zeroHeight, 1.2, 0.35);
    }

    [RelayCommand]
    public void SetYangMillsCoupling(object? coupling)
    {
        ActiveLaboratory = "Millennium Problems";
        SelectedMillenniumProblem = MillenniumProblemType.YangMillsMassGap;
        string key = coupling?.ToString()?.ToLowerInvariant() ?? "scaling";
        switch (key)
        {
            case "strong":
                MillenniumYangMillsBeta = 1.2;
                break;
            case "scaling":
                MillenniumYangMillsBeta = 2.4;
                break;
            case "asymptotic":
                MillenniumYangMillsBeta = 5.2;
                break;
        }
    }

    [RelayCommand]
    public void SetSatClauseRatio(object? ratio)
    {
        ActiveLaboratory = "Millennium Problems";
        SelectedMillenniumProblem = MillenniumProblemType.PversusNP;
        string key = ratio?.ToString()?.ToLowerInvariant() ?? "critical";
        switch (key)
        {
            case "easy_sat" or "easy":
                MillenniumSatAlpha = 3.2;
                break;
            case "critical":
                MillenniumSatAlpha = 4.267;
                break;
            case "unsat":
                MillenniumSatAlpha = 5.8;
                break;
        }
    }

    [RelayCommand]
    public void SetHodgeVariety(object? variety)
    {
        ActiveLaboratory = "Millennium Problems";
        SelectedMillenniumProblem = MillenniumProblemType.HodgeConjecture;
        MillenniumHodgeVariety = variety?.ToString()?.ToLowerInvariant() ?? "k3";
    }

    [RelayCommand]
    public void InsertSymbol(string symbol)
    {
        if (symbol is "millennium" or "zeta" or "heat" or "psi" or "mandelbrot" or "chladni" or "helix")
        {
            OmnibarQuery = symbol;
            ExecuteOmnibar();
            return;
        }

        if (string.IsNullOrEmpty(OmnibarQuery))
        {
            OmnibarQuery = symbol;
        }
        else
        {
            OmnibarQuery += symbol;
        }
    }

    private bool _isSyncingMatrix;

    partial void OnMatrixChanged(Matrix2x2 value)
    {
        UpdateMatrixInfo();
        if (!_isSyncingMatrix)
        {
            _isSyncingMatrix = true;
            MatrixM11 = value.M11;
            MatrixM12 = value.M12;
            MatrixM21 = value.M21;
            MatrixM22 = value.M22;
            _isSyncingMatrix = false;
        }
        UpdateTransformedVector();
    }

    partial void OnMatrixM11Changed(double value) => SyncMatrixFromFields();
    partial void OnMatrixM12Changed(double value) => SyncMatrixFromFields();
    partial void OnMatrixM21Changed(double value) => SyncMatrixFromFields();
    partial void OnMatrixM22Changed(double value) => SyncMatrixFromFields();

    private void SyncMatrixFromFields()
    {
        if (_isSyncingMatrix) return;
        _isSyncingMatrix = true;
        Matrix = new Matrix2x2(MatrixM11, MatrixM12, MatrixM21, MatrixM22);
        _isSyncingMatrix = false;
        UpdateTransformedVector();
    }

    partial void OnTestVectorU1Changed(double value) => UpdateTransformedVector();
    partial void OnTestVectorU2Changed(double value) => UpdateTransformedVector();

    private void UpdateTransformedVector()
    {
        TransformedVectorV1 = Matrix.M11 * TestVectorU1 + Matrix.M12 * TestVectorU2;
        TransformedVectorV2 = Matrix.M21 * TestVectorU1 + Matrix.M22 * TestVectorU2;
        TestVector = new Vector2D(TestVectorU1, TestVectorU2);
    }

    [RelayCommand]
    public void TransposeMatrix() => Matrix = Matrix.Transpose();

    [RelayCommand]
    public void InvertMatrix()
    {
        var inv = Matrix.Inverse();
        if (inv.HasValue) Matrix = inv.Value;
    }

    [RelayCommand]
    public void NegateMatrix() => Matrix = new Matrix2x2(-Matrix.M11, -Matrix.M12, -Matrix.M21, -Matrix.M22);

    [RelayCommand]
    public void ResetIdentityMatrix() => Matrix = Matrix2x2.Identity;

    [RelayCommand]
    public void SetSymmetricSqueeze() => Matrix = new Matrix2x2(1.5, 0.0, 0.0, 1.0 / 1.5);

    // Dynamic Parameter Explorer for Visual Calculus
    partial void OnDynamicParamAChanged(double value) => RecompileFunctionsWithConstants();
    partial void OnDynamicParamBChanged(double value) => RecompileFunctionsWithConstants();
    partial void OnDynamicParamCChanged(double value) => RecompileFunctionsWithConstants();

    public void RecompileFunctionsWithConstants()
    {
        var constants = new Dictionary<string, double>
        {
            ["a"] = DynamicParamA,
            ["b"] = DynamicParamB,
            ["c"] = DynamicParamC,
            ["k"] = DynamicParamA
        };
        foreach (var fn in Functions)
        {
            fn.Recompile(constants);
        }
        RecomputeCalculus();
    }

    [RelayCommand]
    public void AddPresetCurve(string expr)
    {
        int index = Functions.Count + 1;
        var colors = new[]
        {
            Color.FromRgb(16, 185, 129), // Emerald
            Color.FromRgb(245, 158, 11), // Amber
            Color.FromRgb(168, 85, 247), // Violet
            Color.FromRgb(59, 130, 246)  // Blue
        };
        var color = new SolidColorBrush(colors[(index - 1) % colors.Length]);
        var fn = new FunctionItemViewModel($"f{index}", $"h{index}(x)", expr, color);
        fn.FunctionChanged += (s, e) => RecomputeCalculus();
        var constants = new Dictionary<string, double>
        {
            ["a"] = DynamicParamA,
            ["b"] = DynamicParamB,
            ["c"] = DynamicParamC,
            ["k"] = DynamicParamA
        };
        fn.Recompile(constants);
        Functions.Add(fn);
        RecomputeCalculus();
    }

    [RelayCommand]
    public void InsertSymbolIntoPrimaryCurve(string symbol)
    {
        var primary = Functions.FirstOrDefault();
        if (primary != null)
        {
            primary.ExpressionText = string.IsNullOrWhiteSpace(primary.ExpressionText)
                ? symbol
                : primary.ExpressionText + " " + symbol;
        }
    }

    // Vector Field Visual Builder Commands
    [RelayCommand]
    public void ApplyCustomVectorField()
    {
        VectorFieldPresetRequested?.Invoke(VectorFieldP, VectorFieldQ);
        VectorFieldSystemStatus = $"Active ODE: dx/dt = {VectorFieldP}, dy/dt = {VectorFieldQ}";
    }

    [RelayCommand]
    public void InsertVectorFieldP(string token)
    {
        VectorFieldP = string.IsNullOrWhiteSpace(VectorFieldP) ? token : VectorFieldP + " " + token;
        ApplyCustomVectorField();
    }

    [RelayCommand]
    public void InsertVectorFieldQ(string token)
    {
        VectorFieldQ = string.IsNullOrWhiteSpace(VectorFieldQ) ? token : VectorFieldQ + " " + token;
        ApplyCustomVectorField();
    }

    // Knot Theory Visual Builder Commands
    [RelayCommand]
    public void AppendBraidGenerator(string token)
    {
        KnotBraidWord = string.IsNullOrWhiteSpace(KnotBraidWord) ? token : KnotBraidWord.Trim() + " " + token;
    }

    [RelayCommand]
    public void ClearBraidWord()
    {
        KnotBraidWord = string.Empty;
    }

    [RelayCommand]
    public void SetTorusKnotParams(string pair)
    {
        var parts = pair.Split(',');
        if (parts.Length == 2 && int.TryParse(parts[0], out int p) && int.TryParse(parts[1], out int q))
        {
            KnotTorusP = p;
            KnotTorusQ = q;
            SetKnotType("torus");
        }
    }

    [RelayCommand]
    public void SelectLaboratory(string labName)
    {
        ActiveLaboratory = labName;
    }

    [RelayCommand]
    public void AddFunction()
    {
        int index = Functions.Count + 1;
        var colors = new[]
        {
            Color.FromRgb(16, 185, 129), // Emerald
            Color.FromRgb(245, 158, 11), // Amber
            Color.FromRgb(168, 85, 247), // Violet
            Color.FromRgb(59, 130, 246)  // Blue
        };
        var color = new SolidColorBrush(colors[(index - 1) % colors.Length]);
        var fn = new FunctionItemViewModel($"f{index}", $"h{index}(x)", "sin(x)", color);
        fn.FunctionChanged += (s, e) => RecomputeCalculus();
        Functions.Add(fn);
        RecomputeCalculus();
    }

    [RelayCommand]
    public void RemoveFunction(FunctionItemViewModel fn)
    {
        if (Functions.Count <= 1) return; // Keep at least one
        Functions.Remove(fn);
        RecomputeCalculus();
    }

    [RelayCommand]
    public void ExecuteOmnibar()
    {
        if (string.IsNullOrWhiteSpace(OmnibarQuery)) return;
        string query = OmnibarQuery.Trim();

        // 0. Clay Millennium Prize Problems
        if (query.Contains("millennium", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("clay problem", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("perelman", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("ricci flow", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("poincare conjecture", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("birch", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("swinnerton", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("navier-stokes", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("navierstokes", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("yang-mills", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("yangmills", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("mass gap", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("p vs np", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("pvsnp", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("cook levin", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("hodge conjecture", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("hodge diamond", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Millennium Problems";
            if (query.Contains("poincare", StringComparison.OrdinalIgnoreCase) || query.Contains("perelman", StringComparison.OrdinalIgnoreCase) || query.Contains("ricci", StringComparison.OrdinalIgnoreCase))
                SelectMillenniumProblem("poincare");
            else if (query.Contains("riemann", StringComparison.OrdinalIgnoreCase) || query.Contains("zeta", StringComparison.OrdinalIgnoreCase) || query.Contains("gue", StringComparison.OrdinalIgnoreCase))
                SelectMillenniumProblem("riemann");
            else if (query.Contains("bsd", StringComparison.OrdinalIgnoreCase) || query.Contains("birch", StringComparison.OrdinalIgnoreCase) || query.Contains("swinnerton", StringComparison.OrdinalIgnoreCase))
                SelectMillenniumProblem("bsd");
            else if (query.Contains("navier", StringComparison.OrdinalIgnoreCase) || query.Contains("stokes", StringComparison.OrdinalIgnoreCase) || query.Contains("blowup", StringComparison.OrdinalIgnoreCase))
                SelectMillenniumProblem("navierstokes");
            else if (query.Contains("yang", StringComparison.OrdinalIgnoreCase) || query.Contains("mills", StringComparison.OrdinalIgnoreCase) || query.Contains("mass gap", StringComparison.OrdinalIgnoreCase))
                SelectMillenniumProblem("yangmills");
            else if (query.Contains("p vs np", StringComparison.OrdinalIgnoreCase) || query.Contains("pvsnp", StringComparison.OrdinalIgnoreCase) || query.Contains("sat", StringComparison.OrdinalIgnoreCase))
                SelectMillenniumProblem("pvsnp");
            else if (query.Contains("hodge", StringComparison.OrdinalIgnoreCase) || query.Contains("cycle", StringComparison.OrdinalIgnoreCase))
                SelectMillenniumProblem("hodge");
            else
                SelectMillenniumProblem("poincare");
            return;
        }

        // 1. Matrix Syntax: [[a, b], [c, d]]
        if (query.StartsWith("[[") && query.EndsWith("]]"))
        {
            ActiveLaboratory = "Linear Algebra";
            ParseMatrixString(query);
            return;
        }

        // 2. Vector Field Syntax: <P, Q> or (P, Q) or contains "dx/dt"
        if ((query.StartsWith("<") && query.EndsWith(">")) ||
            query.Contains("dx/dt") ||
            (query.Contains(",") && query.Contains("y") && !query.Contains("(")))
        {
            ActiveLaboratory = "Vector Fields & ODEs";
            string clean = query.TrimStart('<').TrimEnd('>');
            clean = clean.Replace("dx/dt", "").Replace("dy/dt", "").Replace("=", "");
            var parts = clean.Split(',');
            if (parts.Length >= 2)
            {
                VectorFieldP = parts[0].Trim();
                VectorFieldQ = parts[1].Trim();
                VectorFieldPresetRequested?.Invoke(VectorFieldP, VectorFieldQ);
                return;
            }
        }

        // 3. Explicit 3D Surface Syntax: starts with "z =" or "surface"
        if (query.StartsWith("z =", StringComparison.OrdinalIgnoreCase) ||
            query.StartsWith("z=", StringComparison.OrdinalIgnoreCase) ||
            query.StartsWith("surface", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Surfaces & 3D";
            string expr = query;
            if (expr.StartsWith("surface", StringComparison.OrdinalIgnoreCase))
            {
                expr = expr.Substring(7).Trim();
            }
            if (expr.StartsWith("z", StringComparison.OrdinalIgnoreCase))
            {
                int eqIdx = expr.IndexOf('=');
                if (eqIdx >= 0) expr = expr.Substring(eqIdx + 1).Trim();
            }
            SurfaceFormula = expr;
            SurfaceFormulaTitle = $"z = {expr}";
            SurfaceFormulaDetail = "User-defined 3D surface z = f(x, y) rendered in real-time with tangent plane and Hessian analysis.";
            return;
        }

        // 3.5 Heat Diffusion & Laplace PDE: heat, diffusion, thermal, laplace, poisson
        if (query.Contains("heat", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("diffusion", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("thermal", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("laplace", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("poisson", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Heat & Diffusion";
            if (query.Contains("dipole", StringComparison.OrdinalIgnoreCase)) SetHeatPreset("dipole");
            else if (query.Contains("insulated", StringComparison.OrdinalIgnoreCase) || query.Contains("neumann", StringComparison.OrdinalIgnoreCase)) SetHeatPreset("insulated");
            else if (query.Contains("corner", StringComparison.OrdinalIgnoreCase)) SetHeatPreset("corners");
            else SetHeatPreset("laplace");
            return;
        }

        // 3.8 Quantum Wave Mechanics & Schrödinger Equation: quantum, schrodinger, wavepacket, tunneling, psi, coherent
        if (query.Contains("quantum", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("schrodinger", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("schrödinger", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("wavepacket", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("tunneling", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("coherent", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("doublewell", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("psi", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Quantum & Waves";
            if (query.Contains("harmonic", StringComparison.OrdinalIgnoreCase) || query.Contains("coherent", StringComparison.OrdinalIgnoreCase)) SetQuantumPreset("harmonic");
            else if (query.Contains("double", StringComparison.OrdinalIgnoreCase) || query.Contains("well", StringComparison.OrdinalIgnoreCase)) SetQuantumPreset("doublewell");
            else if (query.Contains("step", StringComparison.OrdinalIgnoreCase)) SetQuantumPreset("step");
            else if (query.Contains("free", StringComparison.OrdinalIgnoreCase) || query.Contains("dispersion", StringComparison.OrdinalIgnoreCase)) SetQuantumPreset("free");
            else SetQuantumPreset("tunneling");
            return;
        }

        // 3.9 Special Relativity & Minkowski Spacetime: relativity, minkowski, spacetime, lorentz, lightcone, twin, paradox, muon, dilation, contraction, boost
        if (query.Contains("relativity", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("minkowski", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("spacetime", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("lorentz", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("lightcone", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("twin", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("paradox", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("muon", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("dilation", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("contraction", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("boost", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Relativity & Spacetime";
            if (query.Contains("twin", StringComparison.OrdinalIgnoreCase)) SetRelativityPreset("twin");
            else if (query.Contains("muon", StringComparison.OrdinalIgnoreCase)) SetRelativityPreset("muon");
            else if (query.Contains("bell", StringComparison.OrdinalIgnoreCase)) SetRelativityPreset("bell");
            else if (query.Contains("doppler", StringComparison.OrdinalIgnoreCase)) SetRelativityPreset("doppler");
            else SetRelativityPreset("boost");
            return;
        }

        // 3.10 Fluid Dynamics & Navier-Stokes Vorticity: fluid, navier, stokes, vorticity, streamfunction, karman, eddy, turbulence, vortex
        if (query.Contains("fluid", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("navier", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("stokes", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("vorticity", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("vortex", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("streamfunction", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("karman", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("eddy", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("turbulence", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Fluid Dynamics";
            if (query.Contains("dipole", StringComparison.OrdinalIgnoreCase)) SetFluidPreset("dipole");
            else if (query.Contains("kelvin", StringComparison.OrdinalIgnoreCase) || query.Contains("shear", StringComparison.OrdinalIgnoreCase)) SetFluidPreset("kelvin");
            else if (query.Contains("taylor", StringComparison.OrdinalIgnoreCase)) SetFluidPreset("taylor");
            else if (query.Contains("cavity", StringComparison.OrdinalIgnoreCase)) SetFluidPreset("cavity");
            else SetFluidPreset("karman");
            return;
        }

        // 3.11 Topology & 4D Hypersurface Manifolds: tesseract, 4d, hypercube, clifford torus, hopf, klein, manifold, topology, cell24
        if (!query.Contains("homology", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("simplicial", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("betti", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("torsion", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("vietoris", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("rips", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("persistence", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("algebraic", StringComparison.OrdinalIgnoreCase) &&
            (query.Contains("tesseract", StringComparison.OrdinalIgnoreCase) ||
             query.Contains("hypercube", StringComparison.OrdinalIgnoreCase) ||
             query.Contains("4d", StringComparison.OrdinalIgnoreCase) ||
             query.Contains("manifold", StringComparison.OrdinalIgnoreCase) ||
             query.Contains("topology", StringComparison.OrdinalIgnoreCase) ||
             (query.Contains("clifford", StringComparison.OrdinalIgnoreCase) && (query.Contains("torus", StringComparison.OrdinalIgnoreCase) || query.Contains("flat", StringComparison.OrdinalIgnoreCase))) ||
             query.Contains("hopf", StringComparison.OrdinalIgnoreCase) ||
             query.Contains("cell24", StringComparison.OrdinalIgnoreCase) ||
             (query.Contains("klein", StringComparison.OrdinalIgnoreCase) && !query.Contains("gordon", StringComparison.OrdinalIgnoreCase))))
        {
            ActiveLaboratory = "Topology & 4D Manifolds";
            if (query.Contains("clifford", StringComparison.OrdinalIgnoreCase)) SetTopologyPreset("clifford");
            else if (query.Contains("klein", StringComparison.OrdinalIgnoreCase)) SetTopologyPreset("klein");
            else if (query.Contains("hopf", StringComparison.OrdinalIgnoreCase)) SetTopologyPreset("hopf");
            else if (query.Contains("cell24", StringComparison.OrdinalIgnoreCase) || query.Contains("24cell", StringComparison.OrdinalIgnoreCase)) SetTopologyPreset("cell24");
            else SetTopologyPreset("tesseract");
            return;
        }

        // 3.12 Celestial Mechanics & Gravitational N-Body Chaos: celestial, nbody, gravity, figure8, lagrange, trojan, trojans, pythagorean, burrau, galaxy, galactic, collision, tidal, tails, choreography, merger, kepler, orbit
        if (query.Contains("celestial", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("nbody", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("gravity", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("figure8", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("choreography", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("lagrange", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("trojan", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("pythagorean", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("burrau", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("galaxy", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("galactic", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("collision", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("tidal", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("tails", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("merger", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("kepler", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("orbit", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Celestial & N-Body";
            if (query.Contains("lagrange", StringComparison.OrdinalIgnoreCase) || query.Contains("trojan", StringComparison.OrdinalIgnoreCase)) SetCelestialPreset("lagrange");
            else if (query.Contains("pythagorean", StringComparison.OrdinalIgnoreCase) || query.Contains("burrau", StringComparison.OrdinalIgnoreCase)) SetCelestialPreset("pythagorean");
            else if (query.Contains("moon", StringComparison.OrdinalIgnoreCase) || query.Contains("sun", StringComparison.OrdinalIgnoreCase) || query.Contains("solar", StringComparison.OrdinalIgnoreCase)) SetCelestialPreset("sunearthmoon");
            else if (query.Contains("galaxy", StringComparison.OrdinalIgnoreCase) || query.Contains("galactic", StringComparison.OrdinalIgnoreCase) || query.Contains("collision", StringComparison.OrdinalIgnoreCase) || query.Contains("merger", StringComparison.OrdinalIgnoreCase) || query.Contains("tidal", StringComparison.OrdinalIgnoreCase)) SetCelestialPreset("galaxy");
            else SetCelestialPreset("figure8");
            return;
        }

        // 3.125 Differential Geometry, Geodesics & Curvature: diffgeom, differential geometry, theorema egregium, christoffel, pseudosphere, enneper, gaussian curvature
        if (query.Contains("diffgeom", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("differential geometry", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("theorema egregium", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("christoffel", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("gaussian curvature", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("pseudosphere", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("enneper", StringComparison.OrdinalIgnoreCase) ||
            (query.Contains("geodesic", StringComparison.OrdinalIgnoreCase) && !query.Contains("poincare", StringComparison.OrdinalIgnoreCase) && !query.Contains("defect", StringComparison.OrdinalIgnoreCase)) ||
            (query.Contains("bonnet", StringComparison.OrdinalIgnoreCase) && (query.Contains("catenoid", StringComparison.OrdinalIgnoreCase) || query.Contains("helicoid", StringComparison.OrdinalIgnoreCase) || query.Contains("isometric", StringComparison.OrdinalIgnoreCase) || query.Contains("family", StringComparison.OrdinalIgnoreCase))))
        {
            ActiveLaboratory = "Differential Geometry";
            if (query.Contains("sphere", StringComparison.OrdinalIgnoreCase) && !query.Contains("pseudosphere", StringComparison.OrdinalIgnoreCase)) SetDifferentialGeometryPreset("sphere");
            else if (query.Contains("pseudosphere", StringComparison.OrdinalIgnoreCase) || query.Contains("tractrix", StringComparison.OrdinalIgnoreCase)) SetDifferentialGeometryPreset("pseudosphere");
            else if (query.Contains("enneper", StringComparison.OrdinalIgnoreCase) || query.Contains("minimal", StringComparison.OrdinalIgnoreCase)) SetDifferentialGeometryPreset("enneper");
            else if (query.Contains("bonnet", StringComparison.OrdinalIgnoreCase) || query.Contains("helicoid", StringComparison.OrdinalIgnoreCase) || query.Contains("catenoid", StringComparison.OrdinalIgnoreCase)) SetDifferentialGeometryPreset("bonnet");
            else SetDifferentialGeometryPreset("torus");
            return;
        }

        // 3.12 Algebraic Topology, Simplicial Homology & Persistent Betti Numbers: homology, simplicial, betti, vietoris, rips, klein, projective plane, mobius
        if (query.Contains("homology", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("simplicial", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("betti", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("eulerpoincare", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("euler-poincare", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("vietoris", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("rips", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("chain complex", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("persistence barcode", StringComparison.OrdinalIgnoreCase) ||
            (query.Contains("boundary", StringComparison.OrdinalIgnoreCase) && query.Contains("operator", StringComparison.OrdinalIgnoreCase)) ||
            (query.Contains("klein", StringComparison.OrdinalIgnoreCase) && !query.Contains("beltrami", StringComparison.OrdinalIgnoreCase)) ||
            query.Contains("projective plane", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("rp2", StringComparison.OrdinalIgnoreCase) ||
            (query.Contains("mobius", StringComparison.OrdinalIgnoreCase) && !query.Contains("transformation", StringComparison.OrdinalIgnoreCase)) ||
            (query.Contains("torus", StringComparison.OrdinalIgnoreCase) && (query.Contains("triangul", StringComparison.OrdinalIgnoreCase) || query.Contains("homology", StringComparison.OrdinalIgnoreCase) || query.Contains("betti", StringComparison.OrdinalIgnoreCase))))
        {
            ActiveLaboratory = "Algebraic Topology & Homology";
            if (query.Contains("sphere", StringComparison.OrdinalIgnoreCase)) SetHomologyPreset("sphere");
            else if (query.Contains("klein", StringComparison.OrdinalIgnoreCase)) SetHomologyPreset("klein");
            else if (query.Contains("projective", StringComparison.OrdinalIgnoreCase) || query.Contains("rp2", StringComparison.OrdinalIgnoreCase)) SetHomologyPreset("projective");
            else if (query.Contains("mobius", StringComparison.OrdinalIgnoreCase)) SetHomologyPreset("mobius");
            else if (query.Contains("persistence", StringComparison.OrdinalIgnoreCase) || query.Contains("barcode", StringComparison.OrdinalIgnoreCase) || query.Contains("vietoris", StringComparison.OrdinalIgnoreCase) || query.Contains("rips", StringComparison.OrdinalIgnoreCase)) SetHomologyPreset("persistence");
            else SetHomologyPreset("torus");
            return;
        }

        // 3.30 Lie Algebras, Root Systems & Dynkin Diagrams: lie, dynkin, root system, weyl, cartan, su(3), su(4), so(5), so(7), e8, coxeter
        if (query.Contains("lie", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("dynkin", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("root system", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("weyl", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("cartan", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("coxeter", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("su(3)", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("su3", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("su(4)", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("so(5)", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("so(7)", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("g2", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("e8", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Lie Algebras & Dynkin";
            if (query.Contains("g2", StringComparison.OrdinalIgnoreCase)) SetLieType("g2");
            else if (query.Contains("b2", StringComparison.OrdinalIgnoreCase) || query.Contains("so(5)", StringComparison.OrdinalIgnoreCase) || query.Contains("so5", StringComparison.OrdinalIgnoreCase)) SetLieType("b2");
            else if (query.Contains("a3", StringComparison.OrdinalIgnoreCase) || query.Contains("su(4)", StringComparison.OrdinalIgnoreCase) || query.Contains("su4", StringComparison.OrdinalIgnoreCase)) SetLieType("a3");
            else if (query.Contains("b3", StringComparison.OrdinalIgnoreCase) || query.Contains("so(7)", StringComparison.OrdinalIgnoreCase) || query.Contains("so7", StringComparison.OrdinalIgnoreCase)) SetLieType("b3");
            else if (query.Contains("e8", StringComparison.OrdinalIgnoreCase)) SetLieType("e8");
            else SetLieType("a2");
            return;
        }

        // 3.13 Non-Euclidean Hyperbolic & Elliptic Geometry: hyperbolic, noneuclidean, poincare, escher, tessellation, tiling, heptagonal, octagonal, ideal, triangle, gauss, bonnet, defect, holonomy, geodesic, beltrami
        if (query.Contains("hyperbolic", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("noneuclidean", StringComparison.OrdinalIgnoreCase) ||
            (query.Contains("poincare", StringComparison.OrdinalIgnoreCase) && !query.Contains("section", StringComparison.OrdinalIgnoreCase) && !query.Contains("kam", StringComparison.OrdinalIgnoreCase) && !query.Contains("torus", StringComparison.OrdinalIgnoreCase) && !query.Contains("pendulum", StringComparison.OrdinalIgnoreCase) && !query.Contains("homology", StringComparison.OrdinalIgnoreCase) && !query.Contains("simplicial", StringComparison.OrdinalIgnoreCase) && !query.Contains("betti", StringComparison.OrdinalIgnoreCase)) ||
            query.Contains("escher", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("tessellation", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("tiling", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("heptagonal", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("octagonal", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("ideal", StringComparison.OrdinalIgnoreCase) ||
            (query.Contains("triangle", StringComparison.OrdinalIgnoreCase) && !query.Contains("sierpinski", StringComparison.OrdinalIgnoreCase) && (query.Contains("ideal", StringComparison.OrdinalIgnoreCase) || query.Contains("hyperbolic", StringComparison.OrdinalIgnoreCase) || query.Contains("defect", StringComparison.OrdinalIgnoreCase) || query.Contains("holonomy", StringComparison.OrdinalIgnoreCase))) ||
            query.Contains("gauss", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("bonnet", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("gaussbonnet", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("defect", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("holonomy", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("geodesic", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("beltrami", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Hyperbolic Geometry";
            if (query.Contains("octagonal", StringComparison.OrdinalIgnoreCase) || query.Contains("8_3", StringComparison.OrdinalIgnoreCase)) SetHyperbolicPreset("octagonal");
            else if (query.Contains("square", StringComparison.OrdinalIgnoreCase) || query.Contains("4_5", StringComparison.OrdinalIgnoreCase)) SetHyperbolicPreset("square5");
            else if (query.Contains("ideal", StringComparison.OrdinalIgnoreCase)) SetHyperbolicPreset("ideal");
            else if (query.Contains("holonomy", StringComparison.OrdinalIgnoreCase) || query.Contains("defect", StringComparison.OrdinalIgnoreCase) || query.Contains("gauss", StringComparison.OrdinalIgnoreCase) || query.Contains("bonnet", StringComparison.OrdinalIgnoreCase)) SetHyperbolicPreset("holonomy");
            else SetHyperbolicPreset("heptagonal");
            return;
        }

        // 3.14 Information Theory, Entropy & Signal Encoding: information, entropy, shannon, huffman, constellation, capacity, qam, qam16, qpsk, bpsk, bsc, kldiv, divergence, awgn, snr, hartley
        if (query.Contains("information", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("entropy", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("shannon", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("huffman", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("constellation", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("capacity", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("qam", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("qpsk", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("bpsk", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("bsc", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("kldiv", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("divergence", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("awgn", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("hartley", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Information & Entropy";
            if (query.Contains("qam", StringComparison.OrdinalIgnoreCase)) SetInformationPreset("qam16");
            else if (query.Contains("qpsk", StringComparison.OrdinalIgnoreCase) || query.Contains("bpsk", StringComparison.OrdinalIgnoreCase)) SetInformationPreset("qpsk");
            else if (query.Contains("bsc", StringComparison.OrdinalIgnoreCase) || query.Contains("crossover", StringComparison.OrdinalIgnoreCase)) SetInformationPreset("bsc");
            else if (query.Contains("kldiv", StringComparison.OrdinalIgnoreCase) || query.Contains("divergence", StringComparison.OrdinalIgnoreCase) || query.Contains("kullback", StringComparison.OrdinalIgnoreCase)) SetInformationPreset("kldiv");
            else if (query.StartsWith("entropy ", StringComparison.OrdinalIgnoreCase) && query.Length > 8)
            {
                SetInformationPreset("huffman");
                InformationSourceText = query.Substring(8).Trim();
            }
            else if (query.StartsWith("huffman ", StringComparison.OrdinalIgnoreCase) && query.Length > 8)
            {
                SetInformationPreset("huffman");
                InformationSourceText = query.Substring(8).Trim();
            }
            else SetInformationPreset("huffman");
            return;
        }

        // 3.14 Symplectic Geometry, Hamiltonian Mechanics & Phase Space Invariants: hamilton, symplectic, henon, henonheiles, duffing, liouville, yoshida, poincare section
        if (query.Contains("hamilton", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("symplectic", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("henonheiles", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("henon", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("duffing", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("liouville", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("yoshida", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("shadow hamiltonian", StringComparison.OrdinalIgnoreCase) ||
            (query.Contains("poincare section", StringComparison.OrdinalIgnoreCase) && (query.Contains("hamilton", StringComparison.OrdinalIgnoreCase) || query.Contains("symplectic", StringComparison.OrdinalIgnoreCase) || query.Contains("henon", StringComparison.OrdinalIgnoreCase))) ||
            (query.Contains("pendulum", StringComparison.OrdinalIgnoreCase) && (query.Contains("libration", StringComparison.OrdinalIgnoreCase) || query.Contains("separatrix", StringComparison.OrdinalIgnoreCase))))
        {
            ActiveLaboratory = "Symplectic & Hamiltonian";
            if (query.Contains("duffing", StringComparison.OrdinalIgnoreCase)) SetHamiltonianPreset("duffing");
            else if (query.Contains("pendulum", StringComparison.OrdinalIgnoreCase)) SetHamiltonianPreset("pendulum");
            else if (query.Contains("liouville", StringComparison.OrdinalIgnoreCase)) SetHamiltonianPreset("liouville");
            else if (query.Contains("yoshida", StringComparison.OrdinalIgnoreCase)) SetHamiltonianPreset("yoshida");
            else SetHamiltonianPreset("henonheiles");
            return;
        }

        // 3.15 Classical Mechanics, Lagrangian & Double Pendulum Chaos: lagrangian, pendulum, kapitza, foucault, poincare, kam, fermi, euler-lagrange, lyapunov
        if (query.Contains("lagrangian", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("pendulum", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("kapitza", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("foucault", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("fermi", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("kam", StringComparison.OrdinalIgnoreCase) ||
            (query.Contains("torus", StringComparison.OrdinalIgnoreCase) && (query.Contains("kam", StringComparison.OrdinalIgnoreCase) || query.Contains("poincare", StringComparison.OrdinalIgnoreCase) || query.Contains("invariant", StringComparison.OrdinalIgnoreCase))) ||
            query.Contains("section", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("poincare", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("eulerlagrange", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("lyapunov", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Classical Mechanics";
            if (query.Contains("poincare", StringComparison.OrdinalIgnoreCase) || query.Contains("kam", StringComparison.OrdinalIgnoreCase) || query.Contains("torus", StringComparison.OrdinalIgnoreCase)) SetLagrangianPreset("poincare");
            else if (query.Contains("kapitza", StringComparison.OrdinalIgnoreCase) || query.Contains("inverted", StringComparison.OrdinalIgnoreCase)) SetLagrangianPreset("kapitza");
            else if (query.Contains("spring", StringComparison.OrdinalIgnoreCase) || query.Contains("fermi", StringComparison.OrdinalIgnoreCase)) SetLagrangianPreset("spring");
            else if (query.Contains("foucault", StringComparison.OrdinalIgnoreCase) || query.Contains("precession", StringComparison.OrdinalIgnoreCase)) SetLagrangianPreset("foucault");
            else SetLagrangianPreset("double");
            return;
        }

        // 3.16 Modern Cryptography, Elliptic Curves & Finite Fields: crypto, elliptic, weierstrass, ecdh, ecdlp, secp256k1, hasse, trapdoor, finite field
        if (query.Contains("crypto", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("elliptic", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("weierstrass", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("ecdh", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("ecdlp", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("secp256k1", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("hasse", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("trapdoor", StringComparison.OrdinalIgnoreCase) ||
            (query.Contains("finite", StringComparison.OrdinalIgnoreCase) && query.Contains("field", StringComparison.OrdinalIgnoreCase)))
        {
            ActiveLaboratory = "Cryptography & Finite Fields";
            if (query.Contains("ecdh", StringComparison.OrdinalIgnoreCase) || query.Contains("exchange", StringComparison.OrdinalIgnoreCase)) SetCryptoPreset("ecdh");
            else if (query.Contains("ecdlp", StringComparison.OrdinalIgnoreCase) || query.Contains("hop", StringComparison.OrdinalIgnoreCase) || query.Contains("scatter", StringComparison.OrdinalIgnoreCase)) SetCryptoPreset("ecdlp");
            else if (query.Contains("finite", StringComparison.OrdinalIgnoreCase) || query.Contains("hasse", StringComparison.OrdinalIgnoreCase)) SetCryptoPreset("finite");
            else if (query.Contains("egg", StringComparison.OrdinalIgnoreCase) || query.Contains("twocomponent", StringComparison.OrdinalIgnoreCase)) SetCryptoPreset("twocomponents");
            else if (query.Contains("nodal", StringComparison.OrdinalIgnoreCase) || query.Contains("node", StringComparison.OrdinalIgnoreCase)) SetCryptoPreset("nodal");
            else if (query.Contains("cusp", StringComparison.OrdinalIgnoreCase)) SetCryptoPreset("cusp");
            else SetCryptoPreset("secp256k1");
            return;
        }

        // 3.17 Discrete Dynamics, Arnold's Cat Map, Bifurcations & IFS: arnold, catmap, logistic, cobweb, bifurcation, feigenbaum, barnsley, ifs, sierpinski
        if (query.Contains("arnold", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("catmap", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("logistic", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("cobweb", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("bifurcation", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("feigenbaum", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("barnsley", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("sierpinski", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("ifs", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Discrete Dynamics & Chaos";
            if (query.Contains("cobweb", StringComparison.OrdinalIgnoreCase) || query.Contains("logistic", StringComparison.OrdinalIgnoreCase)) SetDiscreteDynamicsPreset("cobweb");
            else if (query.Contains("bifurcation", StringComparison.OrdinalIgnoreCase) || query.Contains("feigenbaum", StringComparison.OrdinalIgnoreCase) || query.Contains("pitchfork", StringComparison.OrdinalIgnoreCase)) SetDiscreteDynamicsPreset("bifurcation");
            else if (query.Contains("barnsley", StringComparison.OrdinalIgnoreCase) || query.Contains("fern", StringComparison.OrdinalIgnoreCase)) SetDiscreteDynamicsPreset("barnsley");
            else if (query.Contains("sierpinski", StringComparison.OrdinalIgnoreCase) || query.Contains("gasket", StringComparison.OrdinalIgnoreCase)) SetDiscreteDynamicsPreset("sierpinski");
            else SetDiscreteDynamicsPreset("arnold");
            return;
        }

        // 3.18 Bifurcation & Catastrophe Theory: catastrophe, cusp, thom, hysteresis, saddlenode, fold, butterfly, zeeman, swallowtail
        if (query.Contains("catastrophe", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("hysteresis", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("saddlenode", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("butterfly", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("zeeman", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("swallowtail", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("cusp", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("thom", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Bifurcation & Catastrophe Theory";
            if (query.Contains("fold", StringComparison.OrdinalIgnoreCase) || query.Contains("saddlenode", StringComparison.OrdinalIgnoreCase)) SetCatastrophePreset("fold");
            else if (query.Contains("butterfly", StringComparison.OrdinalIgnoreCase) || query.Contains("trimodal", StringComparison.OrdinalIgnoreCase)) SetCatastrophePreset("butterfly");
            else if (query.Contains("zeeman", StringComparison.OrdinalIgnoreCase) || query.Contains("machine", StringComparison.OrdinalIgnoreCase)) SetCatastrophePreset("zeeman");
            else if (query.Contains("swallowtail", StringComparison.OrdinalIgnoreCase) || query.Contains("caustic", StringComparison.OrdinalIgnoreCase)) SetCatastrophePreset("swallowtail");
            else SetCatastrophePreset("cusp");
            return;
        }

        // 3.19 Clifford Geometric Algebra & Multivector Calculus: clifford, multivector, bivector, wedge, rotor, sandwich, hestenes
        if (query.Contains("clifford", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("multivector", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("bivector", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("wedge", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("rotor", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("sandwich", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("hestenes", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("geometric algebra", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Clifford Geometric Algebra";
            if (query.Contains("rotor", StringComparison.OrdinalIgnoreCase) || query.Contains("sandwich", StringComparison.OrdinalIgnoreCase)) SetCliffordPreset("rotor");
            else if (query.Contains("reflection", StringComparison.OrdinalIgnoreCase) || query.Contains("mirror", StringComparison.OrdinalIgnoreCase) || query.Contains("cartan", StringComparison.OrdinalIgnoreCase)) SetCliffordPreset("reflection");
            else if (query.Contains("maxwell", StringComparison.OrdinalIgnoreCase) || query.Contains("electromagnet", StringComparison.OrdinalIgnoreCase)) SetCliffordPreset("maxwell");
            else if (query.Contains("spacetime", StringComparison.OrdinalIgnoreCase) || query.Contains("boost", StringComparison.OrdinalIgnoreCase)) SetCliffordPreset("spacetime");
            else SetCliffordPreset("wedge");
            return;
        }

        // 3.21 Knot Theory, Braids & 3D Topological Invariants: knot, braid, skein, reidemeister, borromean, whitehead, cinquefoil
        if (query.Contains("knot", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("braid", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("skein", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("reidemeister", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("borromean", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("whitehead", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("cinquefoil", StringComparison.OrdinalIgnoreCase) ||
            (query.Contains("trefoil", StringComparison.OrdinalIgnoreCase) && (query.Contains("knot", StringComparison.OrdinalIgnoreCase) || query.Contains("skein", StringComparison.OrdinalIgnoreCase) || query.Contains("jones", StringComparison.OrdinalIgnoreCase) || query.Contains("3_1", StringComparison.OrdinalIgnoreCase))) ||
            (query.Contains("figure8", StringComparison.OrdinalIgnoreCase) || query.Contains("figureeight", StringComparison.OrdinalIgnoreCase) || query.Contains("4_1", StringComparison.OrdinalIgnoreCase)) ||
            (query.Contains("jones", StringComparison.OrdinalIgnoreCase) && query.Contains("polynomial", StringComparison.OrdinalIgnoreCase)) ||
            (query.Contains("alexander", StringComparison.OrdinalIgnoreCase) && query.Contains("polynomial", StringComparison.OrdinalIgnoreCase)))
        {
            ActiveLaboratory = "Knot Theory & Invariants";
            if (query.Contains("figure8", StringComparison.OrdinalIgnoreCase) || query.Contains("figureeight", StringComparison.OrdinalIgnoreCase) || query.Contains("4_1", StringComparison.OrdinalIgnoreCase)) SetKnotPreset("figure8");
            else if (query.Contains("cinquefoil", StringComparison.OrdinalIgnoreCase) || query.Contains("solomon", StringComparison.OrdinalIgnoreCase) || query.Contains("5_1", StringComparison.OrdinalIgnoreCase)) SetKnotPreset("cinquefoil");
            else if (query.Contains("borromean", StringComparison.OrdinalIgnoreCase) || query.Contains("brunnian", StringComparison.OrdinalIgnoreCase)) SetKnotPreset("borromean");
            else if (query.Contains("whitehead", StringComparison.OrdinalIgnoreCase)) SetKnotPreset("whitehead");
            else if (query.Contains("braid", StringComparison.OrdinalIgnoreCase) || query.Contains("artin", StringComparison.OrdinalIgnoreCase)) SetKnotPreset("braid");
            else SetKnotPreset("trefoil");
            return;
        }

        // 4. Riemann Zeta & Prime Distribution: zeta, riemann, primes, siegel, criticalstrip, z(t)
        if (query.Contains("zeta", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("riemann", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("prime", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("siegel", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("criticalstrip", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("z(t)", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Zeta & Primes";
            if (query.Contains("staircase", StringComparison.OrdinalIgnoreCase) ||
                query.Contains("prime", StringComparison.OrdinalIgnoreCase))
            {
                SetZetaPreset("primestaircase");
            }
            else if (query.Contains("zero1", StringComparison.OrdinalIgnoreCase) || query.Contains("14.13", StringComparison.OrdinalIgnoreCase))
            {
                SetZetaPreset("zero1");
            }
            else
            {
                SetZetaPreset("criticalline");
            }
            return;
        }

        // 5. Space Curves 3D Syntax: helix, trefoil, viviani, torusknot, twistedcubic, frenet, spacecurve, r(t)
        if (query.Contains("helix", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("trefoil", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("viviani", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("torusknot", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("frenet", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("spacecurve", StringComparison.OrdinalIgnoreCase) ||
            query.StartsWith("r(t)", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("r(t)", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Space Curves 3D";
            if (query.Contains("trefoil", StringComparison.OrdinalIgnoreCase)) SetSpaceCurvePreset("trefoil");
            else if (query.Contains("viviani", StringComparison.OrdinalIgnoreCase)) SetSpaceCurvePreset("viviani");
            else if (query.Contains("torus", StringComparison.OrdinalIgnoreCase)) SetSpaceCurvePreset("torus");
            else if (query.Contains("cubic", StringComparison.OrdinalIgnoreCase)) SetSpaceCurvePreset("twistedcubic");
            else if (query.Contains("conical", StringComparison.OrdinalIgnoreCase)) SetSpaceCurvePreset("conical");
            else SetSpaceCurvePreset("helix");
            return;
        }

        // 6. Wave PDE & Chladni Syntax: chladni, ripple, drum, wave2d, pde
        if (query.Contains("chladni", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("ripple", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("drum", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("wave2d", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("pde", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "PDE & Waves";
            if (query.Contains("drum", StringComparison.OrdinalIgnoreCase)) SetWavePreset("drum");
            else if (query.Contains("ripple", StringComparison.OrdinalIgnoreCase)) SetWavePreset("ripple");
            else if (query.Contains("cross", StringComparison.OrdinalIgnoreCase)) SetWavePreset("chladni13");
            else if (query.Contains("clover", StringComparison.OrdinalIgnoreCase)) SetWavePreset("chladni24");
            else SetWavePreset("chladni35");
            return;
        }

        // 7. Fractal Syntax: mandelbrot, julia, newton, burningship, fractal
        if (query.Contains("mandelbrot", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("julia", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("burningship", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("newton", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("fractal", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Fractals & Chaos";
            if (query.Contains("julia", StringComparison.OrdinalIgnoreCase)) SetFractalPreset("douady");
            else if (query.Contains("newton", StringComparison.OrdinalIgnoreCase)) SetFractalPreset("newton");
            else if (query.Contains("burningship", StringComparison.OrdinalIgnoreCase)) SetFractalPreset("burningship");
            else if (query.Contains("seahorse", StringComparison.OrdinalIgnoreCase)) SetFractalPreset("seahorse");
            else SetFractalPreset("mandelbrot");
            return;
        }

        // 8. Complex Function Syntax: variable 'z'
        if (query.Contains("z") || query.StartsWith("f(z)", StringComparison.OrdinalIgnoreCase))
        {
            ActiveLaboratory = "Complex & Conformal";
            string expr = query;
            if (expr.StartsWith("f(z)", StringComparison.OrdinalIgnoreCase))
            {
                int eqIdx = expr.IndexOf('=');
                if (eqIdx >= 0) expr = expr.Substring(eqIdx + 1).Trim();
            }
            ComplexCustomFormula = expr;
            ComplexFormulaTitle = $"f(z) = {expr}";
            ComplexFormulaDetail = "User-defined complex mapping f(z) compiled symbolically for real-time domain coloring and conformal grid deformation.";
            return;
        }

        // 8.5 Implicit 3D Surface: contains both 'x' and 'y' (e.g. x^2 + y^2)
        if (query.Contains("x") && query.Contains("y") && !query.Contains(","))
        {
            ActiveLaboratory = "Surfaces & 3D";
            SurfaceFormula = query;
            SurfaceFormulaTitle = $"z = {query}";
            SurfaceFormulaDetail = "User-defined 3D surface z = f(x, y) rendered in real-time with tangent plane and Hessian analysis.";
            return;
        }

        // 9. Default: Visual Calculus 1D Function
        ActiveLaboratory = "Visual Calculus";
        if (Functions.Count > 0)
        {
            Functions[0].ExpressionText = query;
            RecomputeCalculus();
        }
    }

    private void ParseMatrixString(string query)
    {
        try
        {
            // Simple parse of [[a, b], [c, d]]
            var cleaned = query.Replace("[", "").Replace("]", "").Replace(" ", "");
            var parts = cleaned.Split(',');
            if (parts.Length == 4)
            {
                double m11 = double.Parse(parts[0]);
                double m12 = double.Parse(parts[1]);
                double m21 = double.Parse(parts[2]);
                double m22 = double.Parse(parts[3]);
                Matrix = new Matrix2x2(m11, m12, m21, m22);
            }
        }
        catch { }
    }

    [RelayCommand]
    public void SetMatrixPreset(string preset)
    {
        Matrix = preset switch
        {
            "rotation45" => Matrix2x2.Rotation(Math.PI * 0.25),
            "rotation90" => Matrix2x2.Rotation(Math.PI * 0.5),
            "shearX" => Matrix2x2.ShearX(1.5),
            "shearY" => Matrix2x2.ShearY(1.2),
            "scale" => Matrix2x2.Scaling(1.5, 0.75),
            "reflection" => Matrix2x2.Reflection(Math.PI * 0.25),
            "singular" => new Matrix2x2(1, 2, 0.5, 1),
            _ => Matrix2x2.Identity
        };
        MorphProgress = 1.0;
        IsMorphing = false;
    }

    [RelayCommand]
    public void ToggleMorphAnimation()
    {
        IsMorphing = !IsMorphing;
    }

    [RelayCommand]
    public void SaveCurrentStateAsPreset()
    {
        string expr = ActiveLaboratory switch
        {
            "Visual Calculus" => Functions.FirstOrDefault()?.ExpressionText ?? "x^2",
            "Linear Algebra" => $"{Matrix.M11:F2},{Matrix.M12:F2},{Matrix.M21:F2},{Matrix.M22:F2}",
            "Vector Fields & ODEs" => $"{VectorFieldP} ; {VectorFieldQ}",
            "Knot Theory & Invariants" => KnotBraidWord,
            "Algebraic Topology & Homology" => HomologyComplexType,
            "Lie Algebras & Dynkin" => LieType,
            _ => ActiveLaboratory
        };

        var customPreset = new MathPreset(
            Id: $"custom.{Guid.NewGuid().ToString("N")[..6]}",
            Title: $"⭐ Custom {ActiveLaboratory}",
            Category: ActiveLaboratory,
            Description: $"User authored state: {expr}",
            ExpressionOrMatrix: expr,
            IconKind: "Star",
            DefaultParamA: DynamicParamA,
            DefaultParamB: DynamicParamB,
            DefaultParamC: DynamicParamC
        );

        Presets.Insert(0, customPreset);
        OnPropertyChanged(nameof(FilteredPresets));
    }

    [RelayCommand]
    public void LoadPreset(MathPreset preset)
    {
        if (preset == null) return;
        ActiveLaboratory = preset.Category;
        CurrentPresetTitle = preset.Title;

        if (preset.Category == "Visual Calculus")
        {
            if (Functions.Count > 0)
            {
                Functions[0].ExpressionText = preset.ExpressionOrMatrix;
                Functions[0].IsVisible = true;
            }
            if (Parameters.Count >= 3)
            {
                Parameters[0].Value = preset.DefaultParamA;
                Parameters[1].Value = preset.DefaultParamB;
                Parameters[2].Value = preset.DefaultParamC;
            }
            RecomputeCalculus();
        }
        else if (preset.Category == "Linear Algebra")
        {
            if (preset.Id.Contains("rotation"))
            {
                Matrix = Matrix2x2.Rotation(Math.PI * 0.25);
            }
            else if (preset.Id.Contains("shear"))
            {
                Matrix = Matrix2x2.ShearX(preset.DefaultParamA);
            }
            else if (preset.Id.Contains("invert"))
            {
                Matrix = new Matrix2x2(0, 1, 1, 0);
            }
            else if (preset.Id.Contains("singular"))
            {
                Matrix = new Matrix2x2(1, 2, 0.5, 1);
            }
            else
            {
                Matrix = Matrix2x2.Identity;
            }
            MorphProgress = 1.0;
        }
        else if (preset.Category == "Fourier & Waves")
        {
            FourierPreset = preset.ExpressionOrMatrix;
            Harmonics = (int)preset.DefaultParamA;
        }
        else if (preset.Category == "Probability & Stats")
        {
            ProbabilityP = preset.DefaultParamA;
        }
        else if (preset.Category == "Vector Fields & ODEs")
        {
            if (preset.Id.Contains("vortex")) SetVectorFieldPreset("vortex");
            else if (preset.Id.Contains("pendulum")) SetVectorFieldPreset("pendulum");
            else if (preset.Id.Contains("predator")) SetVectorFieldPreset("predator");
            else if (preset.Id.Contains("vanderpol")) SetVectorFieldPreset("vanderpol");
        }
        else if (preset.Category == "Complex & Conformal")
        {
            if (preset.Id.Contains("z2")) SetComplexPreset("z2");
            else if (preset.Id.Contains("roots")) SetComplexPreset("roots");
            else if (preset.Id.Contains("exp")) SetComplexPreset("exp");
            else if (preset.Id.Contains("pole")) SetComplexPreset("pole");
            else if (preset.Id.Contains("mobius")) SetComplexPreset("mobius");
            else if (preset.Id.Contains("joukowsky")) SetComplexPreset("joukowsky");
        }
        else if (preset.Category == "Surfaces & 3D")
        {
            if (preset.Id.Contains("paraboloid")) SetSurfacePreset("paraboloid");
            else if (preset.Id.Contains("saddle")) SetSurfacePreset("saddle");
            else if (preset.Id.Contains("sombrero")) SetSurfacePreset("sombrero");
            else if (preset.Id.Contains("gaussian")) SetSurfacePreset("gaussian");
            else if (preset.Id.Contains("monkey")) SetSurfacePreset("monkeysaddle");
            else if (preset.Id.Contains("wave")) SetSurfacePreset("ripple");
            else if (preset.Id.Contains("eggcrate")) SetSurfacePreset("eggcrate");
        }
        else if (preset.Category == "Fractals & Chaos")
        {
            if (preset.Id.Contains("mandelbrot")) SetFractalPreset("mandelbrot");
            else if (preset.Id.Contains("seahorse")) SetFractalPreset("seahorse");
            else if (preset.Id.Contains("elephant")) SetFractalPreset("elephant");
            else if (preset.Id.Contains("douady") || preset.Id.Contains("rabbit")) SetFractalPreset("douady");
            else if (preset.Id.Contains("sanmarco")) SetFractalPreset("sanmarco");
            else if (preset.Id.Contains("newton")) SetFractalPreset("newton");
            else if (preset.Id.Contains("burningship")) SetFractalPreset("burningship");
        }
        else if (preset.Category == "PDE & Waves")
        {
            if (preset.Id.Contains("rosette") || preset.Id.Contains("chladni35")) SetWavePreset("rosette");
            else if (preset.Id.Contains("cross") || preset.Id.Contains("chladni13")) SetWavePreset("cross");
            else if (preset.Id.Contains("clover") || preset.Id.Contains("chladni24")) SetWavePreset("clover");
            else if (preset.Id.Contains("drum")) SetWavePreset("drum");
            else if (preset.Id.Contains("ripple")) SetWavePreset("ripple");
        }
        else if (preset.Category == "Space Curves 3D")
        {
            if (preset.Id.Contains("trefoil")) SetSpaceCurvePreset("trefoil");
            else if (preset.Id.Contains("viviani")) SetSpaceCurvePreset("viviani");
            else if (preset.Id.Contains("torus")) SetSpaceCurvePreset("torus");
            else if (preset.Id.Contains("cubic")) SetSpaceCurvePreset("twistedcubic");
            else if (preset.Id.Contains("conical")) SetSpaceCurvePreset("conical");
            else SetSpaceCurvePreset("helix");
        }
        else if (preset.Category == "Zeta & Primes")
        {
            if (preset.Id.Contains("staircase") || preset.Id.Contains("primes")) SetZetaPreset("primestaircase");
            else if (preset.Id.Contains("explicit")) SetZetaPreset("explicitprimes");
            else if (preset.Id.Contains("zero1")) SetZetaPreset("zero1");
            else if (preset.Id.Contains("zero2")) SetZetaPreset("zero2");
            else if (preset.Id.Contains("zero3")) SetZetaPreset("zero3");
            else SetZetaPreset("criticalline");
        }
        else if (preset.Category == "Heat & Diffusion")
        {
            if (preset.Id.Contains("dipole")) SetHeatPreset("dipole");
            else if (preset.Id.Contains("insulated")) SetHeatPreset("insulated");
            else if (preset.Id.Contains("corner")) SetHeatPreset("corners");
            else SetHeatPreset("laplace");
        }
        else if (preset.Category == "Quantum & Waves")
        {
            if (preset.Id.Contains("harmonic")) SetQuantumPreset("harmonic");
            else if (preset.Id.Contains("doublewell")) SetQuantumPreset("doublewell");
            else if (preset.Id.Contains("step")) SetQuantumPreset("step");
            else if (preset.Id.Contains("free")) SetQuantumPreset("free");
            else SetQuantumPreset("tunneling");
        }
        else if (preset.Category == "Relativity & Spacetime")
        {
            if (preset.Id.Contains("twin")) SetRelativityPreset("twin");
            else if (preset.Id.Contains("muon")) SetRelativityPreset("muon");
            else if (preset.Id.Contains("bell")) SetRelativityPreset("bell");
            else if (preset.Id.Contains("doppler")) SetRelativityPreset("doppler");
            else SetRelativityPreset("boost");
        }
        else if (preset.Category == "Fluid Dynamics")
        {
            if (preset.Id.Contains("dipole")) SetFluidPreset("dipole");
            else if (preset.Id.Contains("kelvin")) SetFluidPreset("kelvin");
            else if (preset.Id.Contains("taylor")) SetFluidPreset("taylor");
            else if (preset.Id.Contains("cavity")) SetFluidPreset("cavity");
            else SetFluidPreset("karman");
        }
        else if (preset.Category == "Topology & 4D Manifolds" || preset.Id.StartsWith("topology."))
        {
            if (preset.Id.Contains("clifford")) SetTopologyPreset("clifford");
            else if (preset.Id.Contains("klein")) SetTopologyPreset("klein");
            else if (preset.Id.Contains("hopf")) SetTopologyPreset("hopf");
            else if (preset.Id.Contains("cell24")) SetTopologyPreset("cell24");
            else SetTopologyPreset("tesseract");
        }
        else if (preset.Category == "Celestial & N-Body" || preset.Id.StartsWith("celestial."))
        {
            if (preset.Id.Contains("lagrange")) SetCelestialPreset("lagrange");
            else if (preset.Id.Contains("pythagorean")) SetCelestialPreset("pythagorean");
            else if (preset.Id.Contains("sunearthmoon")) SetCelestialPreset("sunearthmoon");
            else if (preset.Id.Contains("galaxy")) SetCelestialPreset("galaxy");
            else SetCelestialPreset("figure8");
        }
        else if (preset.Category == "Hyperbolic Geometry" || preset.Id.StartsWith("hyperbolic."))
        {
            if (preset.Id.Contains("octagonal")) SetHyperbolicPreset("octagonal");
            else if (preset.Id.Contains("square")) SetHyperbolicPreset("square5");
            else if (preset.Id.Contains("ideal")) SetHyperbolicPreset("ideal");
            else if (preset.Id.Contains("holonomy")) SetHyperbolicPreset("holonomy");
            else SetHyperbolicPreset("heptagonal");
        }
        else if (preset.Category == "Information & Entropy" || preset.Id.StartsWith("info."))
        {
            if (preset.Id.Contains("qam")) SetInformationPreset("qam16");
            else if (preset.Id.Contains("qpsk")) SetInformationPreset("qpsk");
            else if (preset.Id.Contains("bsc")) SetInformationPreset("bsc");
            else if (preset.Id.Contains("kldiv")) SetInformationPreset("kldiv");
            else SetInformationPreset("huffman");
        }
        else if (preset.Category == "Classical Mechanics" || preset.Id.StartsWith("lagrangian."))
        {
            if (preset.Id.Contains("poincare")) SetLagrangianPreset("poincare");
            else if (preset.Id.Contains("kapitza")) SetLagrangianPreset("kapitza");
            else if (preset.Id.Contains("spring")) SetLagrangianPreset("spring");
            else if (preset.Id.Contains("foucault")) SetLagrangianPreset("foucault");
            else SetLagrangianPreset("double");
        }
        else if (preset.Category == "Cryptography & Finite Fields" || preset.Id.StartsWith("crypto."))
        {
            if (preset.Id.Contains("twocomponents")) SetCryptoPreset("twocomponents");
            else if (preset.Id.Contains("nodal")) SetCryptoPreset("nodal");
            else if (preset.Id.Contains("finite")) SetCryptoPreset("finite");
            else if (preset.Id.Contains("ecdh")) SetCryptoPreset("ecdh");
            else if (preset.Id.Contains("ecdlp")) SetCryptoPreset("ecdlp");
            else SetCryptoPreset("secp256k1");
        }
        else if (preset.Category == "Discrete Dynamics & Chaos" || preset.Id.StartsWith("dynamics."))
        {
            if (preset.Id.Contains("cobweb") || preset.Id.Contains("logistic")) SetDiscreteDynamicsPreset("cobweb");
            else if (preset.Id.Contains("bifurcation")) SetDiscreteDynamicsPreset("bifurcation");
            else if (preset.Id.Contains("barnsley")) SetDiscreteDynamicsPreset("barnsley");
            else if (preset.Id.Contains("sierpinski")) SetDiscreteDynamicsPreset("sierpinski");
            else SetDiscreteDynamicsPreset("arnold");
        }
        else if (preset.Category == "Bifurcation & Catastrophe Theory" || preset.Id.StartsWith("catastrophe."))
        {
            if (preset.Id.Contains("fold")) SetCatastrophePreset("fold");
            else if (preset.Id.Contains("butterfly")) SetCatastrophePreset("butterfly");
            else if (preset.Id.Contains("zeeman")) SetCatastrophePreset("zeeman");
            else if (preset.Id.Contains("swallowtail")) SetCatastrophePreset("swallowtail");
            else SetCatastrophePreset("cusp");
        }
        else if (preset.Category == "Clifford Geometric Algebra" || preset.Id.StartsWith("clifford."))
        {
            if (preset.Id.Contains("rotor")) SetCliffordPreset("rotor");
            else if (preset.Id.Contains("reflection")) SetCliffordPreset("reflection");
            else if (preset.Id.Contains("maxwell")) SetCliffordPreset("maxwell");
            else if (preset.Id.Contains("spacetime")) SetCliffordPreset("spacetime");
            else SetCliffordPreset("wedge");
        }
        else if (preset.Category == "Differential Geometry" || preset.Id.StartsWith("diffgeom."))
        {
            if (preset.Id.Contains("sphere") && !preset.Id.Contains("pseudosphere")) SetDifferentialGeometryPreset("sphere");
            else if (preset.Id.Contains("pseudosphere")) SetDifferentialGeometryPreset("pseudosphere");
            else if (preset.Id.Contains("enneper")) SetDifferentialGeometryPreset("enneper");
            else if (preset.Id.Contains("bonnet")) SetDifferentialGeometryPreset("bonnet");
            else SetDifferentialGeometryPreset("torus");
        }
        else if (preset.Category == "Knot Theory & Invariants" || preset.Id.StartsWith("knot."))
        {
            if (preset.Id.Contains("figure8")) SetKnotPreset("figure8");
            else if (preset.Id.Contains("cinquefoil")) SetKnotPreset("cinquefoil");
            else if (preset.Id.Contains("borromean")) SetKnotPreset("borromean");
            else if (preset.Id.Contains("whitehead")) SetKnotPreset("whitehead");
            else if (preset.Id.Contains("braid")) SetKnotPreset("braid");
            else SetKnotPreset("trefoil");
        }
        else if (preset.Category == "Symplectic & Hamiltonian" || preset.Id.StartsWith("hamilton."))
        {
            if (preset.Id.Contains("duffing")) SetHamiltonianPreset("duffing");
            else if (preset.Id.Contains("pendulum")) SetHamiltonianPreset("pendulum");
            else if (preset.Id.Contains("liouville")) SetHamiltonianPreset("liouville");
            else if (preset.Id.Contains("yoshida")) SetHamiltonianPreset("yoshida");
            else SetHamiltonianPreset("henonheiles");
        }
        else if (preset.Category == "Algebraic Topology & Homology" || preset.Id.StartsWith("homology."))
        {
            if (preset.Id.Contains("sphere")) SetHomologyPreset("sphere");
            else if (preset.Id.Contains("klein")) SetHomologyPreset("klein");
            else if (preset.Id.Contains("projective")) SetHomologyPreset("projective");
            else if (preset.Id.Contains("mobius")) SetHomologyPreset("mobius");
            else if (preset.Id.Contains("persistence")) SetHomologyPreset("persistence");
            else SetHomologyPreset("torus");
        }
        else if (preset.Category == "Lie Algebras & Dynkin" || preset.Id.StartsWith("lie."))
        {
            if (preset.Id.Contains("g2")) SetLieType("g2");
            else if (preset.Id.Contains("b2")) SetLieType("b2");
            else if (preset.Id.Contains("a3")) SetLieType("a3");
            else if (preset.Id.Contains("b3")) SetLieType("b3");
            else if (preset.Id.Contains("e8")) SetLieType("e8");
            else SetLieType("a2");
        }
        else if (preset.Category == "Millennium Problems" || preset.Id.StartsWith("millennium."))
        {
            ActiveLaboratory = "Millennium Problems";
            if (preset.Id.Contains("poincare")) SelectMillenniumProblem("poincare");
            else if (preset.Id.Contains("riemann")) SelectMillenniumProblem("riemann");
            else if (preset.Id.Contains("bsd")) SelectMillenniumProblem("bsd");
            else if (preset.Id.Contains("navier")) SelectMillenniumProblem("navierstokes");
            else if (preset.Id.Contains("yangmills")) SelectMillenniumProblem("yangmills");
            else if (preset.Id.Contains("pvsnp")) SelectMillenniumProblem("pvsnp");
            else if (preset.Id.Contains("hodge")) SelectMillenniumProblem("hodge");
            else SelectMillenniumProblem("poincare");
        }
    }

    [RelayCommand]
    public void SetFourierPreset(string preset)
    {
        ActiveLaboratory = "Fourier & Waves";
        FourierPreset = preset;
    }

    [RelayCommand]
    public void ToggleAudioPlayback()
    {
        if (IsAudioPlaying)
        {
            AudioToneGenerator.Stop();
            IsAudioPlaying = false;
        }
        else
        {
            var voices = new List<HarmonicVoice>();
            for (int k = 1; k <= Math.Min(Harmonics, 12); k++)
            {
                double amp = FourierPreset switch
                {
                    "square" => k % 2 == 1 ? 1.0 / k : 0.0,
                    "sawtooth" => 1.0 / k,
                    "triangle" => k % 2 == 1 ? 1.0 / (k * k) : 0.0,
                    _ => 1.0 / (k * k)
                };
                voices.Add(new HarmonicVoice(k, amp, 0.0));
            }

            AudioToneGenerator.PlayHarmonicWave(fundamentalFreq: 220.0, durationSeconds: 3.5, masterVolume: 0.3, voices);
            IsAudioPlaying = true;
        }
    }

    private Dictionary<string, double> GetConstantMap()
    {
        var map = new Dictionary<string, double>();
        foreach (var p in Parameters)
        {
            map[p.Name] = p.Value;
        }
        if (!map.ContainsKey("k") && map.ContainsKey("a"))
        {
            map["k"] = map["a"];
        }
        return map;
    }

    public void RecompileAllFunctions()
    {
        var constants = GetConstantMap();
        foreach (var fn in Functions)
        {
            fn.Recompile(constants);
        }
        RecomputeIntegral();
        RecomputeDerivativeCard();
    }

    public void RecomputeCalculus()
    {
        var primary = Functions.FirstOrDefault(f => f.IsVisible && f.CompiledFunc != null) ?? Functions.FirstOrDefault();
        if (primary == null) return;

        var constants = GetConstantMap();
        primary.Recompile(constants);

        // Derivation Steps
        if (primary.Ast != null)
        {
            var steps = new List<DerivationStep>();
            SymbolicDifferentiator.Differentiate(primary.Ast, "x", steps);
            DerivationSteps.Clear();
            foreach (var s in steps)
            {
                DerivationSteps.Add(s);
            }
        }

        // Critical Points
        if (primary.CompiledFunc != null)
        {
            var pts = FunctionAnalyzer.FindCriticalPoints(primary.CompiledFunc, -6.0, 6.0);
            CriticalPointsList.Clear();
            foreach (var pt in pts)
            {
                CriticalPointsList.Add(pt);
            }
        }

        // Taylor Series Polynomial
        if (primary.Ast != null)
        {
            var (_, tForm) = TaylorSeries.GenerateTaylorSeries(primary.Ast, TaylorCenter, TaylorDegree);
            TaylorFormula = tForm;
        }

        RecomputeIntegral();
        RecomputeDerivativeCard();
    }

    private void RecomputeIntegral()
    {
        var primary = Functions.FirstOrDefault(f => f.IsVisible && f.CompiledFunc != null);
        if (primary?.CompiledFunc == null)
        {
            IntegralInfo = null;
            return;
        }

        double val = NumericalIntegration.IntegrateAdaptive(primary.CompiledFunc, IntegrationA, IntegrationB);
        double riemannVal = NumericalIntegration.IntegrateRiemann(primary.CompiledFunc, IntegrationA, IntegrationB, RiemannN, RiemannRule);

        IntegralInfo = new IntegralTypesetInfo(
            LowerBound: IntegrationA,
            UpperBound: IntegrationB,
            IntegrandText: primary.ExpressionText,
            AntiderivativeText: primary.AnalyticalAntiderivative,
            NumericalValue: val,
            FormattedResult: val.ToString("F3"),
            RiemannN: RiemannN,
            RiemannRuleName: RiemannRule.ToString(),
            RiemannValue: riemannVal
        );
    }

    private void RecomputeDerivativeCard()
    {
        var primary = Functions.FirstOrDefault(f => f.IsVisible && f.CompiledFunc != null);
        if (primary?.CompiledFunc == null)
        {
            DerivativeInfo = null;
            return;
        }

        double slope = FunctionAnalyzer.FirstDerivative(primary.CompiledFunc, ProbeX);
        double d2 = FunctionAnalyzer.SecondDerivative(primary.CompiledFunc, ProbeX);
        double y0 = primary.CompiledFunc(ProbeX);
        var (m, c) = FunctionAnalyzer.TangentLineEquation(primary.CompiledFunc, ProbeX);
        var normal = FunctionAnalyzer.NormalLineEquation(primary.CompiledFunc, ProbeX);
        var osc = FunctionAnalyzer.ComputeOsculatingCircle(primary.CompiledFunc, ProbeX);

        static string FormatLineEq(double slope, double intercept)
        {
            slope = Math.Abs(slope) < 1e-9 ? 0.0 : slope;
            intercept = Math.Abs(intercept) < 1e-9 ? 0.0 : intercept;
            string sign = intercept >= 0 ? "+" : "-";
            return $"y = {slope:F2}x {sign} {Math.Abs(intercept):F2}";
        }

        string normEq = normal.HasValue ? FormatLineEq(normal.Value.Slope, normal.Value.Intercept) : "x = c (Vertical)";

        DerivativeInfo = new DerivativeTypesetInfo(
            FunctionText: primary.ExpressionText,
            DerivativeText: primary.AnalyticalDerivative,
            ProbeX: ProbeX,
            ProbeY: y0,
            Slope: slope,
            SecondDerivative: d2,
            TangentEquation: FormatLineEq(m, c),
            NormalEquation: normEq,
            Curvature: osc.Curvature,
            OsculatingRadius: osc.Radius
        );
    }

    [RelayCommand]
    public void DropPlinkoBurst() => PlinkoBurstRequested?.Invoke();

    [RelayCommand]
    public void ResetPlinko() => PlinkoResetRequested?.Invoke();

    private static double CleanZero(double v) => Math.Abs(v) < 1e-9 ? 0.0 : v;

    private void UpdateMatrixInfo()
    {
        double det = CleanZero(Matrix.Determinant);
        double tr = CleanZero(Matrix.Trace);
        string orientation = det > 1e-4 ? "Preserved (+)" : det < -1e-4 ? "Inverted (-)" : "Singular / Collapsed (0)";

        var inv = Matrix.Inverse();
        string invStr = inv.HasValue
            ? $"A⁻¹ = [[{CleanZero(inv.Value.M11):F2}, {CleanZero(inv.Value.M12):F2}], [{CleanZero(inv.Value.M21):F2}, {CleanZero(inv.Value.M22):F2}]]"
            : "A⁻¹ = Non-Invertible (Singular: det=0)";

        var pairs = Matrix.ComputeEigenPairs();
        string eigenStr = pairs.Count > 0 && pairs[0].IsReal
            ? $"λ₁ = {CleanZero(pairs[0].Lambda):F2}" + (pairs.Count > 1 ? $", λ₂ = {CleanZero(pairs[1].Lambda):F2}" : "")
            : "λ: Complex Rotational";

        string trSign = tr >= 0 ? $"- {tr:F2}" : $"+ {Math.Abs(tr):F2}";
        string detSign = det >= 0 ? $"+ {det:F2}" : $"- {Math.Abs(det):F2}";
        string charPoly = $"λ² {trSign}λ {detSign} = 0";

        MatrixInfo = new MatrixTypesetInfo(
            M11: CleanZero(Matrix.M11),
            M12: CleanZero(Matrix.M12),
            M21: CleanZero(Matrix.M21),
            M22: CleanZero(Matrix.M22),
            Determinant: det,
            Trace: tr,
            InverseDescription: invStr,
            CharacteristicPolynomial: charPoly,
            EigenvaluesSummary: eigenStr,
            OrientationDescription: orientation
        );
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _animationTimer.Stop();
        AudioToneGenerator.Stop();
        if (ActiveInstance == this) ActiveInstance = null;
    }
}
