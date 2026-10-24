using System;
using System.Collections.Generic;

namespace FryMath.Core.Millennium;

public enum MillenniumProblemType
{
    PoincareConjecture,
    RiemannHypothesis,
    BirchSwinnertonDyer,
    NavierStokesSmoothness,
    YangMillsMassGap,
    PversusNP,
    HodgeConjecture
}

public sealed record MillenniumProblemInfo(
    MillenniumProblemType Type,
    string Key,
    string Title,
    string Status,
    bool IsSolved,
    string ProposerOrSolver,
    string ClayDescription,
    string MathematicalStatement,
    string SolutionOrCurrentAttack,
    string CoreFormula,
    string NumericalInvariantLabel,
    string PrimaryVariable
);

public sealed record RicciFlowState(
    double Time,
    double MinCurvature,
    double MaxCurvature,
    double AverageCurvature,
    double PerelmanEntropyW,
    int SurgeryCount,
    string TopologyClass,
    bool IsExtinct
);

public sealed record BsdMultiplePoint(int Multiplier, double X, double Y, bool IsInfinity)
{
    public override string ToString() => IsInfinity ? $"{Multiplier}P₀ = ∞" : $"{Multiplier}P₀({X:F2}, {Y:F2})";
}

public sealed record BsdEllipticState(
    double ParamA,
    double ParamB,
    double Discriminant,
    int AnalyticRank,
    double LeadingCoefficientL,
    double RealPeriodOmega,
    double RegulatorR,
    double ShaOrder,
    string MordellWeilStructure,
    IReadOnlyList<BsdMultiplePoint>? GeneratorMultiples = null
);

public sealed record NavierStokesBlowupState(
    double Time,
    double MaxVorticity,
    double BkmBlowupIntegral,
    double KineticEnergy,
    double Enstrophy,
    double EnergyDissipationRate,
    bool IsBlowupImminent,
    string RegularityStatus,
    bool IsOpenAiBlowupVerified = false,
    string ProofFormalism = "Beale-Kato-Majda Enstrophy Bound",
    double SingularityTimeTStar = 1.84,
    double SelfSimilarAlpha = 0.5
);

public sealed record YangMillsLatticeState(
    double CouplingBeta,
    double MassGapDelta,
    double StringTensionSigma,
    double AveragePlaquette,
    double WilsonLoopArea,
    string ConfinementRegime
);

public sealed record PvsNpSatState(
    int VariablesCount,
    int ClausesCount,
    double ClauseRatioAlpha,
    double SatisfiabilityProbability,
    double SearchComplexitySteps,
    string ComplexityRegime,
    string ProofBarrier
);

public sealed record HodgeDiamondState(
    string VarietyName,
    int Dimension,
    int[,] HodgeDiamond,
    int RationalHodgeClassesCount,
    int KnownAlgebraicCyclesCount,
    bool IsLefschetzApplicable,
    string HodgeStatus
);

public sealed record RiemannGueState(
    double TMin,
    double TMax,
    int ZerosCount,
    double PairCorrelationMean,
    double GueRepulsionIndex,
    double PrimeLiResidual,
    string SpectralStatus
);
