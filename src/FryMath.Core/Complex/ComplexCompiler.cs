using System;
using System.Collections.Generic;
using System.Numerics;
using FryMath.Core.CAS;

namespace FryMath.Core.ComplexAnalysis;

public static class ComplexCompiler
{
    public static Func<Complex, Complex> Compile(MathNode node, string varZ = "z")
    {
        return z => EvaluateComplex(node, z, varZ);
    }

    public static Complex EvaluateComplex(MathNode node, Complex z, string varZ = "z")
    {
        return node switch
        {
            NumberNode num => new Complex(num.Value, 0.0),

            VariableNode v => v.Name.ToLowerInvariant() switch
            {
                "z" => z,
                "i" => Complex.ImaginaryOne,
                "pi" or "π" => new Complex(Math.PI, 0.0),
                "e" => new Complex(Math.E, 0.0),
                "x" => new Complex(z.Real, 0.0),
                "y" => new Complex(z.Imaginary, 0.0),
                _ => string.Equals(v.Name, varZ, StringComparison.OrdinalIgnoreCase) ? z : Complex.Zero
            },

            UnaryOpNode u => u.Op switch
            {
                MathUnaryOp.Negate => -EvaluateComplex(u.Operand, z, varZ),
                MathUnaryOp.Plus => EvaluateComplex(u.Operand, z, varZ),
                _ => EvaluateComplex(u.Operand, z, varZ)
            },

            BinaryOpNode b => EvaluateBinary(b, z, varZ),

            FunctionCallNode f => EvaluateFunction(f, z, varZ),

            _ => Complex.Zero
        };
    }

    private static Complex EvaluateBinary(BinaryOpNode b, Complex z, string varZ)
    {
        var left = EvaluateComplex(b.Left, z, varZ);
        var right = EvaluateComplex(b.Right, z, varZ);

        try
        {
            return b.Op switch
            {
                MathOp.Add => left + right,
                MathOp.Subtract => left - right,
                MathOp.Multiply => left * right,
                MathOp.Divide => right == Complex.Zero ? new Complex(1e9, 1e9) : left / right,
                MathOp.Power => Complex.Pow(left, right),
                _ => Complex.Zero
            };
        }
        catch
        {
            return new Complex(1e9, 1e9);
        }
    }

    private static Complex EvaluateFunction(FunctionCallNode f, Complex z, string varZ)
    {
        if (f.Arguments.Count == 0) return Complex.Zero;
        var arg = EvaluateComplex(f.Arguments[0], z, varZ);

        try
        {
            return f.Name.ToLowerInvariant() switch
            {
                "sin" => Complex.Sin(arg),
                "cos" => Complex.Cos(arg),
                "tan" => Complex.Tan(arg),
                "exp" => Complex.Exp(arg),
                "ln" or "log" => Complex.Log(arg),
                "sqrt" => Complex.Sqrt(arg),
                "sinh" => Complex.Sinh(arg),
                "cosh" => Complex.Cosh(arg),
                "tanh" => Complex.Tanh(arg),
                "abs" => new Complex(arg.Magnitude, 0.0),
                "arg" => new Complex(Math.Atan2(arg.Imaginary, arg.Real), 0.0),
                "conj" => new Complex(arg.Real, -arg.Imaginary),
                "re" => new Complex(arg.Real, 0.0),
                "im" => new Complex(arg.Imaginary, 0.0),
                _ => arg
            };
        }
        catch
        {
            return new Complex(1e9, 1e9);
        }
    }
}
