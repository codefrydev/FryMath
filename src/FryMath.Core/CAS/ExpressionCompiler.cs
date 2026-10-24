using System;
using System.Collections.Generic;

namespace FryMath.Core.CAS;

/// <summary>
/// Compiles a MathNode AST into a fast delegate Func&lt;double, double&gt; for 60 FPS plotting and real-time numerical evaluation.
/// </summary>
public static class ExpressionCompiler
{
    public static Func<double, double> Compile1D(MathNode node, string variable = "x", IReadOnlyDictionary<string, double>? constants = null)
    {
        var localDict = constants != null
            ? new Dictionary<string, double>(constants, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        return x =>
        {
            localDict[variable] = x;
            try
            {
                double res = node.Evaluate(localDict);
                return double.IsNaN(res) ? 0.0 : res;
            }
            catch
            {
                return double.NaN;
            }
        };
    }

    public static Func<double, double, double> Compile2D(MathNode node, string varX = "x", string varY = "y", IReadOnlyDictionary<string, double>? constants = null)
    {
        var localDict = constants != null
            ? new Dictionary<string, double>(constants, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        return (x, y) =>
        {
            localDict[varX] = x;
            localDict[varY] = y;
            try
            {
                double res = node.Evaluate(localDict);
                return double.IsNaN(res) ? 0.0 : res;
            }
            catch
            {
                return double.NaN;
            }
        };
    }
}
