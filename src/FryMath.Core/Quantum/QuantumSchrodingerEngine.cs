using System;
using System.Numerics;

namespace FryMath.Core.Quantum;

public enum QuantumPreset
{
    TunnelingBarrier,
    HarmonicOscillator,
    DoubleWell,
    StepPotential,
    FreeWavepacket
}

public sealed class QuantumSchrodingerEngine
{
    public int Points { get; }
    public double XMin { get; }
    public double XMax { get; }
    public double Dx { get; }

    // Wavefunction: psi_r + i * psi_i
    public double[] PsiR { get; private set; }
    public double[] PsiI { get; private set; }

    // Potential energy V(x)
    public double[] Potential { get; private set; }

    // Tridiagonal solver working arrays
    private readonly double[] _diagR;
    private readonly double[] _diagI;
    private readonly double[] _rhsR;
    private readonly double[] _rhsI;
    private readonly double[] _cPrimeR;
    private readonly double[] _cPrimeI;
    private readonly double[] _dPrimeR;
    private readonly double[] _dPrimeI;

    public double HBar { get; set; } = 1.0;
    public double Mass { get; set; } = 1.0;
    public double Dt { get; set; } = 0.05;

    public QuantumSchrodingerEngine(int points = 300, double xMin = -15.0, double xMax = 15.0)
    {
        Points = Math.Max(64, points);
        XMin = xMin;
        XMax = xMax;
        Dx = (xMax - xMin) / (Points - 1);

        PsiR = new double[Points];
        PsiI = new double[Points];
        Potential = new double[Points];

        _diagR = new double[Points];
        _diagI = new double[Points];
        _rhsR = new double[Points];
        _rhsI = new double[Points];
        _cPrimeR = new double[Points];
        _cPrimeI = new double[Points];
        _dPrimeR = new double[Points];
        _dPrimeI = new double[Points];

        LoadPreset(QuantumPreset.TunnelingBarrier);
    }

    public double GetX(int index) => XMin + index * Dx;

    public int GetIndex(double x)
    {
        int idx = (int)Math.Round((x - XMin) / Dx);
        return Math.Clamp(idx, 0, Points - 1);
    }

    /// <summary>
    /// Initializes a normalized Gaussian wavepacket:
    /// ψ(x) = (1 / (2π σ²))^(1/4) * exp( - (x - x0)² / (4σ²) + i * k0 * x )
    /// </summary>
    public void InitializeWavepacket(double x0, double k0, double sigma = 1.2)
    {
        double normFactor = Math.Pow(1.0 / (2.0 * Math.PI * sigma * sigma), 0.25);

        for (int i = 0; i < Points; i++)
        {
            double x = GetX(i);
            double envelope = normFactor * Math.Exp(-((x - x0) * (x - x0)) / (4.0 * sigma * sigma));
            double phase = k0 * x;

            PsiR[i] = envelope * Math.Cos(phase);
            PsiI[i] = envelope * Math.Sin(phase);
        }

        Normalize();
    }

    /// <summary>
    /// Normalizes the wavefunction so that ∫ |ψ(x)|² dx = 1.
    /// </summary>
    public void Normalize()
    {
        double sum = 0.0;
        for (int i = 0; i < Points; i++)
        {
            sum += (PsiR[i] * PsiR[i] + PsiI[i] * PsiI[i]);
        }
        sum *= Dx;

        if (sum > 1e-12)
        {
            double scale = 1.0 / Math.Sqrt(sum);
            for (int i = 0; i < Points; i++)
            {
                PsiR[i] *= scale;
                PsiI[i] *= scale;
            }
        }
    }

    /// <summary>
    /// Calculates the total probability norm ∫ |ψ|² dx (must equal 1.000 for unitary evolution).
    /// </summary>
    public double CalculateNorm()
    {
        double sum = 0.0;
        for (int i = 0; i < Points; i++)
        {
            sum += (PsiR[i] * PsiR[i] + PsiI[i] * PsiI[i]);
        }
        return sum * Dx;
    }

    /// <summary>
    /// Calculates expectation value of position <X> = ∫ ψ* x ψ dx.
    /// </summary>
    public double CalculateExpectedPosition()
    {
        double sum = 0.0;
        for (int i = 0; i < Points; i++)
        {
            double prob = PsiR[i] * PsiR[i] + PsiI[i] * PsiI[i];
            sum += prob * GetX(i);
        }
        return sum * Dx;
    }

    /// <summary>
    /// Calculates expectation value of momentum <P> = ∫ ψ* (-i ħ ∂/∂x) ψ dx.
    /// </summary>
    public double CalculateExpectedMomentum()
    {
        double sum = 0.0;
        for (int i = 1; i < Points - 1; i++)
        {
            double dPsiRdx = (PsiR[i + 1] - PsiR[i - 1]) / (2.0 * Dx);
            double dPsiIdx = (PsiI[i + 1] - PsiI[i - 1]) / (2.0 * Dx);

            // -i ħ (d/dx)(PsiR + i PsiI) = ħ (dPsiIdx - i dPsiRdx)
            // Re( psi* [ħ (dPsiIdx - i dPsiRdx)] ) = ħ * (PsiR * dPsiIdx - PsiI * dPsiRdx)
            sum += HBar * (PsiR[i] * dPsiIdx - PsiI[i] * dPsiRdx);
        }
        return sum * Dx;
    }

    /// <summary>
    /// Calculates the probability current density J(x) = (ħ / m) * Im(ψ* ∂ψ/∂x).
    /// </summary>
    public double CalculateProbabilityCurrent(int index)
    {
        int i = Math.Clamp(index, 1, Points - 2);
        double dPsiRdx = (PsiR[i + 1] - PsiR[i - 1]) / (2.0 * Dx);
        double dPsiIdx = (PsiI[i + 1] - PsiI[i - 1]) / (2.0 * Dx);

        return (HBar / Mass) * (PsiR[i] * dPsiIdx - PsiI[i] * dPsiRdx);
    }

    /// <summary>
    /// Advances the wavefunction forward in time by Dt using the unconditionally unitary
    /// Crank-Nicolson implicit scheme (Cayley transform of the Hamiltonian):
    /// (1 + i H dt / 2ħ) ψ^(n+1) = (1 - i H dt / 2ħ) ψ^n
    /// Solved in O(N) time via complex tridiagonal elimination (Thomas algorithm).
    /// </summary>
    public void Step()
    {
        double alpha = (HBar * Dt) / (4.0 * Mass * Dx * Dx);
        double halfDtOverHBar = Dt / (2.0 * HBar);

        // Subdiagonal and superdiagonal elements: a = -i * alpha
        // A_ii = 1 + 2i * alpha + i * (dt / 2ħ) * V_i
        // B_ii = 1 - 2i * alpha - i * (dt / 2ħ) * V_i

        // 1. Construct RHS vector = B * psi^n
        for (int i = 0; i < Points; i++)
        {
            double vTerm = halfDtOverHBar * Potential[i];
            double bDiagR = 1.0;
            double bDiagI = -(2.0 * alpha + vTerm);

            // Diagonal product: (bDiagR + i bDiagI) * (PsiR[i] + i PsiI[i])
            double curR = bDiagR * PsiR[i] - bDiagI * PsiI[i];
            double curI = bDiagR * PsiI[i] + bDiagI * PsiR[i];

            // Off-diagonal contributions: +i * alpha * psi_{i-1} and +i * alpha * psi_{i+1}
            if (i > 0)
            {
                // +i * alpha * (PsiR + i PsiI) = -alpha * PsiI + i * alpha * PsiR
                curR -= alpha * PsiI[i - 1];
                curI += alpha * PsiR[i - 1];
            }

            if (i < Points - 1)
            {
                curR -= alpha * PsiI[i + 1];
                curI += alpha * PsiR[i + 1];
            }

            _rhsR[i] = curR;
            _rhsI[i] = curI;

            // Matrix A diagonal
            _diagR[i] = 1.0;
            _diagI[i] = 2.0 * alpha + vTerm;
        }

        // Off-diagonal elements for matrix A: -i * alpha
        double aOffR = 0.0;
        double aOffI = -alpha;

        // 2. Forward elimination in complex Thomas algorithm
        // c'_0 = c_0 / a_0
        Complex a0 = new Complex(_diagR[0], _diagI[0]);
        Complex c0 = new Complex(aOffR, aOffI);
        Complex d0 = new Complex(_rhsR[0], _rhsI[0]);

        Complex cPrime0 = c0 / a0;
        Complex dPrime0 = d0 / a0;

        _cPrimeR[0] = cPrime0.Real;
        _cPrimeI[0] = cPrime0.Imaginary;
        _dPrimeR[0] = dPrime0.Real;
        _dPrimeI[0] = dPrime0.Imaginary;

        for (int i = 1; i < Points; i++)
        {
            Complex aDiag = new Complex(_diagR[i], _diagI[i]);
            Complex bSub = new Complex(aOffR, aOffI);
            Complex prevCPrime = new Complex(_cPrimeR[i - 1], _cPrimeI[i - 1]);
            Complex denom = aDiag - bSub * prevCPrime;

            if (i < Points - 1)
            {
                Complex cSuper = new Complex(aOffR, aOffI);
                Complex cP = cSuper / denom;
                _cPrimeR[i] = cP.Real;
                _cPrimeI[i] = cP.Imaginary;
            }

            Complex d = new Complex(_rhsR[i], _rhsI[i]);
            Complex prevDPrime = new Complex(_dPrimeR[i - 1], _dPrimeI[i - 1]);
            Complex dP = (d - bSub * prevDPrime) / denom;

            _dPrimeR[i] = dP.Real;
            _dPrimeI[i] = dP.Imaginary;
        }

        // 3. Back-substitution
        PsiR[Points - 1] = _dPrimeR[Points - 1];
        PsiI[Points - 1] = _dPrimeI[Points - 1];

        for (int i = Points - 2; i >= 0; i--)
        {
            Complex dP = new Complex(_dPrimeR[i], _dPrimeI[i]);
            Complex cP = new Complex(_cPrimeR[i], _cPrimeI[i]);
            Complex nextPsi = new Complex(PsiR[i + 1], PsiI[i + 1]);
            Complex psi = dP - cP * nextPsi;

            PsiR[i] = psi.Real;
            PsiI[i] = psi.Imaginary;
        }

        // Enforce hard Dirichlet boundary condition at extreme boundaries
        PsiR[0] = 0.0;
        PsiI[0] = 0.0;
        PsiR[Points - 1] = 0.0;
        PsiI[Points - 1] = 0.0;
    }

    public void LoadPreset(QuantumPreset preset)
    {
        Array.Clear(Potential, 0, Points);

        switch (preset)
        {
            case QuantumPreset.TunnelingBarrier:
                // Barrier of height V0 = 4.0 between x = -0.5 and x = 0.6
                // Wavepacket with kinetic energy E ≈ k0²/2 = 2.2² / 2 = 2.42 < V0
                for (int i = 0; i < Points; i++)
                {
                    double x = GetX(i);
                    if (x >= -0.5 && x <= 0.6)
                    {
                        Potential[i] = 4.0;
                    }
                }
                InitializeWavepacket(x0: -7.0, k0: 2.2, sigma: 1.1);
                break;

            case QuantumPreset.HarmonicOscillator:
                // V(x) = 0.5 * k * x² capped at domain boundaries
                for (int i = 0; i < Points; i++)
                {
                    double x = GetX(i);
                    Potential[i] = Math.Min(0.25 * x * x, 9.0);
                }
                // Coherent state initialized off-center at x0 = -3.5
                InitializeWavepacket(x0: -3.5, k0: 0.0, sigma: 1.0);
                break;

            case QuantumPreset.DoubleWell:
                // Symmetric double well: V(x) = 0.08 * (x² - 9)² capped at 8.0
                for (int i = 0; i < Points; i++)
                {
                    double x = GetX(i);
                    double x2 = x * x;
                    Potential[i] = Math.Min(0.08 * (x2 - 9.0) * (x2 - 9.0), 8.5);
                }
                // Initialized in the left well at x0 = -3.0
                InitializeWavepacket(x0: -3.0, k0: 0.0, sigma: 0.85);
                break;

            case QuantumPreset.StepPotential:
                for (int i = 0; i < Points; i++)
                {
                    double x = GetX(i);
                    Potential[i] = x >= 0.0 ? 3.0 : 0.0;
                }
                InitializeWavepacket(x0: -7.0, k0: 2.5, sigma: 1.1);
                break;

            case QuantumPreset.FreeWavepacket:
            default:
                // Dispersion / spreading in free space
                InitializeWavepacket(x0: -4.0, k0: 2.0, sigma: 0.75);
                break;
        }
    }

    /// <summary>
    /// User interactive potential injection: adds a Gaussian barrier or well at xCenter.
    /// </summary>
    public void AddPotentialFeature(double xCenter, double height, double width = 0.8)
    {
        for (int i = 0; i < Points; i++)
        {
            double x = GetX(i);
            double dist = (x - xCenter) / width;
            Potential[i] = Math.Clamp(Potential[i] + height * Math.Exp(-dist * dist), 0.0, 20.0);
        }
    }
}
