using System;
using System.Collections.Generic;

namespace FryMath.Core.Millennium;

public enum RiemannAttackFramework
{
    HilbertPolya,
    DeBruijnNewman,
    LiCriterion,
    ZeroHunter,
    GueChaos,
    PrimeHarmonics
}

public sealed record HilbertPolyaState(
    IReadOnlyList<double> QuantumEigenvalues,
    IReadOnlyList<double> ActualZeros,
    double SemiclassicalResidual,
    string OperatorFormula,
    string BoundaryCondition,
    string SpectralProofStatus
);

public sealed record DeBruijnRootPoint(
    int Index,
    double RealPart,
    double ImagPart,
    bool IsOnCriticalLine
);

public sealed record DeBruijnNewmanState(
    double ParameterT,
    double NewmanConstantLambda,
    IReadOnlyList<DeBruijnRootPoint> Roots,
    bool AllRootsReal,
    string RodgersTaoBound,
    string StatusDescription
);

public sealed record LiCoefficient(
    int N,
    double Value,
    double AsymptoticEstimate,
    bool IsPositive
);

public sealed record LiCriterionState(
    IReadOnlyList<LiCoefficient> Coefficients,
    int TestedCount,
    bool AllPositive,
    double MinPositivityMargin,
    string TheoremStatement
);

public sealed record GramPointInfo(
    int Index,
    double T,
    double ZValue,
    bool GramLawSatisfied
);

public sealed record ZeroHunterState(
    double TMin,
    double TMax,
    IReadOnlyList<double> FoundZeros,
    IReadOnlyList<GramPointInfo> GramPoints,
    int TheoreticalCountN,
    int VerifiedOnLineCount,
    bool AllZerosOnCriticalLine,
    string TuringVerificationCertificate
);

public sealed record GueChaosState(
    IReadOnlyList<double> NormalizedSpacings,
    double MeanSpacing,
    double WignerSurmiseRepulsion,
    double PairCorrelationIntegral,
    string ChaoticRegime
);
