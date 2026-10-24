using System;
using System.Text;
using FryMath.Core.CAS;
using FryMath.Core.LinearAlgebra;

namespace FryMath.Core.Formatting;

public static class MathUnicodeFormatter
{
    private static readonly string SuperscriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";
    private static readonly string SubscriptDigits = "₀₁₂₃₄₅₆₇₈₉";

    public static string ToSuperscript(int n)
    {
        if (n == 0) return "⁰";
        var sb = new StringBuilder();
        if (n < 0)
        {
            sb.Append('⁻');
            n = -n;
        }
        string s = n.ToString();
        foreach (char c in s)
        {
            if (char.IsDigit(c))
            {
                sb.Append(SuperscriptDigits[c - '0']);
            }
        }
        return sb.ToString();
    }

    public static string ToSubscript(int n)
    {
        var sb = new StringBuilder();
        if (n < 0)
        {
            sb.Append('₋');
            n = -n;
        }
        string s = n.ToString();
        foreach (char c in s)
        {
            if (char.IsDigit(c))
            {
                sb.Append(SubscriptDigits[c - '0']);
            }
        }
        return sb.ToString();
    }

    public static string FormatIntegral(string integrand, double a, double b, string var = "x")
    {
        return $"∫ [{a:F2} → {b:F2}] ({integrand}) d{var}";
    }

    public static string FormatMatrix2x2(Matrix2x2 m)
    {
        return $"⎡ {m.M11,6:F2}  {m.M12,6:F2} ⎤\n⎣ {m.M21,6:F2}  {m.M22,6:F2} ⎦";
    }

    public static string FormatDerivative(string expr, string var = "x", int order = 1)
    {
        string d = order == 1 ? $"d/d{var}" : $"d{ToSuperscript(order)}/d{var}{ToSuperscript(order)}";
        return $"{d} [{expr}]";
    }
}

public static class LatexFormatter
{
    public static string ToLatex(MathNode node)
    {
        return node switch
        {
            NumberNode num => num.ToString(),
            VariableNode v => v.Name.ToLowerInvariant() switch
            {
                "pi" => @"\pi",
                "theta" => @"\theta",
                "phi" => @"\phi",
                "lambda" => @"\lambda",
                "omega" => @"\omega",
                _ => v.Name
            },
            UnaryOpNode u => u.Op == MathUnaryOp.Negate ? $"-{ToLatex(u.Operand)}" : ToLatex(u.Operand),
            BinaryOpNode b => b.Op switch
            {
                MathOp.Add => $"{ToLatex(b.Left)} + {ToLatex(b.Right)}",
                MathOp.Subtract => $"{ToLatex(b.Left)} - {ToLatex(b.Right)}",
                MathOp.Multiply => $"{ToLatex(b.Left)} \\cdot {ToLatex(b.Right)}",
                MathOp.Divide => $"\\frac{{{ToLatex(b.Left)}}}{{{ToLatex(b.Right)}}}",
                MathOp.Power => $"{{{ToLatex(b.Left)}}}^{{{ToLatex(b.Right)}}}",
                _ => b.ToString()
            },
            FunctionCallNode f => f.Name.ToLowerInvariant() switch
            {
                "sin" or "cos" or "tan" or "exp" or "ln" or "sinh" or "cosh" or "tanh" =>
                    $"\\{f.Name}({string.Join(", ", f.Arguments.Select(ToLatex))})",
                "sqrt" => $"\\sqrt{{{ToLatex(f.Arguments[0])}}}",
                _ => $"{f.Name}({string.Join(", ", f.Arguments.Select(ToLatex))})"
            },
            _ => node.ToString()
        };
    }
}
