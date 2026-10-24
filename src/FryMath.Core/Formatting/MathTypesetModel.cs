using System;
using System.Collections.Generic;

namespace FryMath.Core.Formatting;

public sealed record IntegralTypesetInfo(
    double LowerBound,
    double UpperBound,
    string IntegrandText,
    string AntiderivativeText,
    double NumericalValue,
    string FormattedResult,
    int RiemannN,
    string RiemannRuleName,
    double RiemannValue
);

public sealed record DerivativeTypesetInfo(
    string FunctionText,
    string DerivativeText,
    double ProbeX,
    double ProbeY,
    double Slope,
    double SecondDerivative,
    string TangentEquation,
    string NormalEquation,
    double Curvature,
    double OsculatingRadius
);

public sealed record MatrixTypesetInfo(
    double M11,
    double M12,
    double M21,
    double M22,
    double Determinant,
    double Trace,
    string? InverseDescription,
    string CharacteristicPolynomial,
    string EigenvaluesSummary,
    string OrientationDescription
);
