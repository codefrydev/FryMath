using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FryMath.Core.CAS;

public enum MathOp
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Power
}

public enum MathUnaryOp
{
    Negate,
    Plus
}

/// <summary>
/// Abstract base node in the Abstract Syntax Tree (AST) for symbolic mathematical expressions.
/// </summary>
public abstract record MathNode
{
    public abstract double Evaluate(IReadOnlyDictionary<string, double>? variables = null);
    public abstract MathNode DeepCopy();
    public abstract override string ToString();
}

public sealed record NumberNode(double Value) : MathNode
{
    public override double Evaluate(IReadOnlyDictionary<string, double>? variables = null) => Value;

    public override MathNode DeepCopy() => new NumberNode(Value);

    public override string ToString()
    {
        if (Math.Abs(Value - Math.Round(Value)) < 1e-9)
        {
            return ((long)Math.Round(Value)).ToString(CultureInfo.InvariantCulture);
        }
        return Value.ToString("G6", CultureInfo.InvariantCulture);
    }
}

public sealed record VariableNode(string Name) : MathNode
{
    public override double Evaluate(IReadOnlyDictionary<string, double>? variables = null)
    {
        if (variables != null && variables.TryGetValue(Name, out var val))
        {
            return val;
        }
        if (string.Equals(Name, "pi", StringComparison.OrdinalIgnoreCase) || Name == "π")
        {
            return Math.PI;
        }
        if (string.Equals(Name, "e", StringComparison.OrdinalIgnoreCase))
        {
            return Math.E;
        }
        if (string.Equals(Name, "phi", StringComparison.OrdinalIgnoreCase) || Name == "φ")
        {
            return 1.618033988749895; // Golden ratio
        }
        throw new InvalidOperationException($"Variable '{Name}' is not defined in the evaluation context.");
    }

    public override MathNode DeepCopy() => new VariableNode(Name);

    public override string ToString() => Name;
}

public sealed record BinaryOpNode(MathOp Op, MathNode Left, MathNode Right) : MathNode
{
    public override double Evaluate(IReadOnlyDictionary<string, double>? variables = null)
    {
        double l = Left.Evaluate(variables);
        double r = Right.Evaluate(variables);
        return Op switch
        {
            MathOp.Add => l + r,
            MathOp.Subtract => l - r,
            MathOp.Multiply => l * r,
            MathOp.Divide => r == 0.0 ? (l >= 0 ? double.PositiveInfinity : double.NegativeInfinity) : l / r,
            MathOp.Power => Math.Pow(l, r),
            _ => throw new NotSupportedException($"Unsupported operator: {Op}")
        };
    }

    public override MathNode DeepCopy() => new BinaryOpNode(Op, Left.DeepCopy(), Right.DeepCopy());

    public override string ToString()
    {
        string opStr = Op switch
        {
            MathOp.Add => " + ",
            MathOp.Subtract => " - ",
            MathOp.Multiply => " * ",
            MathOp.Divide => " / ",
            MathOp.Power => "^",
            _ => " ? "
        };

        string lStr = NeedsParens(Left, Op, isRight: false) ? $"({Left})" : Left.ToString();
        string rStr = NeedsParens(Right, Op, isRight: true) ? $"({Right})" : Right.ToString();

        return $"{lStr}{opStr}{rStr}";
    }

    private static bool NeedsParens(MathNode child, MathOp parentOp, bool isRight)
    {
        if (child is not BinaryOpNode bin) return false;

        int childPrec = GetPrecedence(bin.Op);
        int parentPrec = GetPrecedence(parentOp);

        if (childPrec < parentPrec) return true;
        if (childPrec == parentPrec)
        {
            // Left-associative subtraction and division require parens on right side: a - (b - c)
            if (isRight && (parentOp == MathOp.Subtract || parentOp == MathOp.Divide)) return true;
            // Power is right-associative: (a^b)^c != a^(b^c)
            if (!isRight && parentOp == MathOp.Power) return true;
        }
        return false;
    }

    public static int GetPrecedence(MathOp op) => op switch
    {
        MathOp.Add or MathOp.Subtract => 1,
        MathOp.Multiply or MathOp.Divide => 2,
        MathOp.Power => 3,
        _ => 0
    };
}

public sealed record UnaryOpNode(MathUnaryOp Op, MathNode Operand) : MathNode
{
    public override double Evaluate(IReadOnlyDictionary<string, double>? variables = null)
    {
        double val = Operand.Evaluate(variables);
        return Op == MathUnaryOp.Negate ? -val : val;
    }

    public override MathNode DeepCopy() => new UnaryOpNode(Op, Operand.DeepCopy());

    public override string ToString()
    {
        string sign = Op == MathUnaryOp.Negate ? "-" : "+";
        return Operand is BinaryOpNode ? $"{sign}({Operand})" : $"{sign}{Operand}";
    }
}

public sealed record FunctionCallNode(string Name, IReadOnlyList<MathNode> Arguments) : MathNode
{
    public override double Evaluate(IReadOnlyDictionary<string, double>? variables = null)
    {
        if (Arguments.Count == 0) throw new InvalidOperationException($"Function '{Name}' requires arguments.");
        double arg0 = Arguments[0].Evaluate(variables);

        return Name.ToLowerInvariant() switch
        {
            "sin" => Math.Sin(arg0),
            "cos" => Math.Cos(arg0),
            "tan" => Math.Tan(arg0),
            "asin" or "arcsin" => Math.Asin(arg0),
            "acos" or "arccos" => Math.Acos(arg0),
            "atan" or "arctan" => Math.Atan(arg0),
            "sinh" => Math.Sinh(arg0),
            "cosh" => Math.Cosh(arg0),
            "tanh" => Math.Tanh(arg0),
            "exp" => Math.Exp(arg0),
            "ln" or "log" => Math.Log(arg0),
            "log10" => Math.Log10(arg0),
            "log2" => Math.Log2(arg0),
            "sqrt" => Math.Sqrt(arg0),
            "cbrt" => Math.Cbrt(arg0),
            "abs" => Math.Abs(arg0),
            "floor" => Math.Floor(arg0),
            "ceil" => Math.Ceiling(arg0),
            "round" => Math.Round(arg0),
            "sign" or "sgn" => Math.Sign(arg0),
            "atan2" => Arguments.Count > 1 ? Math.Atan2(arg0, Arguments[1].Evaluate(variables)) : Math.Atan(arg0),
            "min" => Arguments.Count > 1 ? Math.Min(arg0, Arguments[1].Evaluate(variables)) : arg0,
            "max" => Arguments.Count > 1 ? Math.Max(arg0, Arguments[1].Evaluate(variables)) : arg0,
            _ => throw new NotSupportedException($"Unknown function: {Name}")
        };
    }

    public override MathNode DeepCopy() => new FunctionCallNode(Name, Arguments.Select(a => a.DeepCopy()).ToList());

    public override string ToString() => $"{Name}({string.Join(", ", Arguments)})";
}
