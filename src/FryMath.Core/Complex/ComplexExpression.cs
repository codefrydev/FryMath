using System;
using System.Collections.Generic;
using System.Numerics;
using FryMath.Core.CAS;

namespace FryMath.Core.ComplexAnalysis;

public enum ComplexPreset
{
    ZSquared,
    CubicRootsOfUnity,
    Exponential,
    Sine,
    SimplePole,
    Mobius,
    JoukowskyAirfoil
}

/// <summary>
/// Evaluates functions of a single complex variable f(z): C -> C.
/// Supports both analytical preset transforms and general complex AST evaluation.
/// </summary>
public sealed class ComplexExpression
{
    public ComplexPreset Preset { get; set; } = ComplexPreset.ZSquared;
    public string CustomFormula { get; set; } = "z^2";
    public Func<Complex, Complex> Evaluator { get; private set; } = z => z * z;

    public ComplexExpression(ComplexPreset preset = ComplexPreset.ZSquared)
    {
        SetPreset(preset);
    }

    public void SetPreset(ComplexPreset preset)
    {
        Preset = preset;
        Evaluator = preset switch
        {
            ComplexPreset.ZSquared => z => z * z,
            ComplexPreset.CubicRootsOfUnity => z => z * z * z - Complex.One,
            ComplexPreset.Exponential => z => Complex.Exp(z),
            ComplexPreset.Sine => z => Complex.Sin(z),
            ComplexPreset.SimplePole => z => z == Complex.Zero ? new Complex(1e9, 1e9) : Complex.One / z,
            ComplexPreset.Mobius => z => (z + Complex.One) == Complex.Zero ? new Complex(1e9, 1e9) : (z - Complex.One) / (z + Complex.One),
            ComplexPreset.JoukowskyAirfoil => z => z == Complex.Zero ? Complex.Zero : 0.5 * (z + Complex.One / z),
            _ => z => z * z
        };

        CustomFormula = preset switch
        {
            ComplexPreset.ZSquared => "z^2",
            ComplexPreset.CubicRootsOfUnity => "z^3 - 1",
            ComplexPreset.Exponential => "exp(z)",
            ComplexPreset.Sine => "sin(z)",
            ComplexPreset.SimplePole => "1 / z",
            ComplexPreset.Mobius => "(z - 1) / (z + 1)",
            ComplexPreset.JoukowskyAirfoil => "0.5 * (z + 1/z)",
            _ => "z^2"
        };
    }

    public void CompileCustomFormula(string formula)
    {
        CustomFormula = formula;
        try
        {
            var ast = MathParser.Parse(formula);
            Evaluator = ComplexCompiler.Compile(ast, "z");
        }
        catch
        {
            Evaluator = z => z * z;
        }
    }

    public Complex Evaluate(Complex z)
    {
        try
        {
            var res = Evaluator(z);
            if (double.IsNaN(res.Real) || double.IsNaN(res.Imaginary) ||
                double.IsInfinity(res.Real) || double.IsInfinity(res.Imaginary))
            {
                return new Complex(1e9, 1e9);
            }
            return res;
        }
        catch
        {
            return new Complex(1e9, 1e9);
        }
    }

    /// <summary>
    /// Computes numerical complex derivative: f'(z) = [f(z + h) - f(z - h)] / 2h.
    /// Used to check conformal non-degeneracy (|f'(z)| != 0).
    /// </summary>
    public Complex Derivative(Complex z, double h = 1e-5)
    {
        var fPlus = Evaluate(z + new Complex(h, 0));
        var fMinus = Evaluate(z - new Complex(h, 0));
        return (fPlus - fMinus) / (2.0 * h);
    }
}
