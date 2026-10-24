using System;
using System.Collections.Generic;

namespace FryMath.Core.CAS;

/// <summary>
/// Analytical symbolic integration engine for FryMath CAS.
/// Computes exact closed-form antiderivatives ∫ f(x) dx with step-by-step pedagogical derivation history.
/// </summary>
public static class SymbolicIntegrator
{
    public static MathNode Integrate(MathNode node, string variable = "x", List<DerivationStep>? steps = null)
    {
        var rawResult = IntegrateInternal(node, variable, steps);
        var simplified = MathSimplifier.Simplify(rawResult);

        if (steps != null && rawResult.ToString() != simplified.ToString())
        {
            steps.Add(new DerivationStep(
                "Algebraic Simplification",
                rawResult.ToString(),
                simplified.ToString(),
                "Simplify integrated terms, cancel factors, and combine constant coefficients."
            ));
        }

        return simplified;
    }

    /// <summary>
    /// Helper to parse an expression string, compute its analytical antiderivative, and format with constant of integration + C.
    /// </summary>
    public static string IntegrateToString(string expression, string variable = "x", List<DerivationStep>? steps = null)
    {
        try
        {
            var node = MathParser.Parse(expression);
            var anti = Integrate(node, variable, steps);
            string antiStr = anti.ToString();
            return antiStr == "0" ? "C" : $"{antiStr} + C";
        }
        catch
        {
            return "Unable to integrate symbolically";
        }
    }

    private static MathNode IntegrateInternal(MathNode node, string variable, List<DerivationStep>? steps)
    {
        // 1. Constant number: ∫ c dx = c * x
        if (node is NumberNode num)
        {
            if (num.Value == 0.0)
            {
                return new NumberNode(0);
            }

            var result = new BinaryOpNode(MathOp.Multiply, new NumberNode(num.Value), new VariableNode(variable));
            steps?.Add(new DerivationStep(
                "Constant Rule",
                $"∫ ({num}) d{variable}",
                result.ToString(),
                $"The integral of a constant c is c · {variable}."
            ));
            return result;
        }

        // 2. Variable node: ∫ x dx = x^2 / 2 = 0.5 * x^2, or ∫ a dx = a * x
        if (node is VariableNode v)
        {
            if (string.Equals(v.Name, variable, StringComparison.OrdinalIgnoreCase))
            {
                // ∫ x dx = x^2 / 2
                var result = new BinaryOpNode(MathOp.Divide,
                    new BinaryOpNode(MathOp.Power, new VariableNode(variable), new NumberNode(2)),
                    new NumberNode(2));
                steps?.Add(new DerivationStep(
                    "Power Rule of Integration",
                    $"∫ {variable} d{variable}",
                    $"{variable}^2 / 2",
                    $"∫ {variable}^1 d{variable} = {variable}^(1+1) / (1+1) = {variable}^2 / 2."
                ));
                return result;
            }
            else
            {
                // Independent variable/constant: ∫ a dx = a * x
                var result = new BinaryOpNode(MathOp.Multiply, new VariableNode(v.Name), new VariableNode(variable));
                steps?.Add(new DerivationStep(
                    "Constant Parameter Rule",
                    $"∫ {v.Name} d{variable}",
                    result.ToString(),
                    $"The symbol '{v.Name}' is treated as a constant with respect to {variable}."
                ));
                return result;
            }
        }

        // 3. Unary negation: ∫ -f(x) dx = - ∫ f(x) dx
        if (node is UnaryOpNode u)
        {
            if (u.Op == MathUnaryOp.Negate)
            {
                var inner = IntegrateInternal(u.Operand, variable, steps);
                return new UnaryOpNode(MathUnaryOp.Negate, inner);
            }
            return IntegrateInternal(u.Operand, variable, steps);
        }

        // 4. Binary operations
        if (node is BinaryOpNode b)
        {
            // Sum / Difference Rule: ∫ (f ± g) dx = ∫ f dx ± ∫ g dx
            if (b.Op == MathOp.Add)
            {
                var l = IntegrateInternal(b.Left, variable, steps);
                var r = IntegrateInternal(b.Right, variable, steps);
                var result = new BinaryOpNode(MathOp.Add, l, r);
                steps?.Add(new DerivationStep(
                    "Sum Rule",
                    $"∫ ({b.Left} + {b.Right}) d{variable}",
                    result.ToString(),
                    "The integral of a sum is the sum of the integrals."
                ));
                return result;
            }

            if (b.Op == MathOp.Subtract)
            {
                var l = IntegrateInternal(b.Left, variable, steps);
                var r = IntegrateInternal(b.Right, variable, steps);
                var result = new BinaryOpNode(MathOp.Subtract, l, r);
                steps?.Add(new DerivationStep(
                    "Difference Rule",
                    $"∫ ({b.Left} - {b.Right}) d{variable}",
                    result.ToString(),
                    "The integral of a difference is the difference of the integrals."
                ));
                return result;
            }

            // Multiplication: Constant multiple c * f(x)
            if (b.Op == MathOp.Multiply)
            {
                bool leftHasVar = ContainsVariable(b.Left, variable);
                bool rightHasVar = ContainsVariable(b.Right, variable);

                // Constant multiple rule: c * f(x)
                if (!leftHasVar && rightHasVar)
                {
                    var integratedRight = IntegrateInternal(b.Right, variable, steps);
                    var result = new BinaryOpNode(MathOp.Multiply, b.Left.DeepCopy(), integratedRight);
                    steps?.Add(new DerivationStep(
                        "Constant Multiple Rule",
                        $"∫ {b.Left} · ({b.Right}) d{variable}",
                        result.ToString(),
                        $"Factor constant factor {b.Left} out of the integral: ∫ c·f(x)dx = c·∫ f(x)dx."
                    ));
                    return result;
                }
                if (leftHasVar && !rightHasVar)
                {
                    var integratedLeft = IntegrateInternal(b.Left, variable, steps);
                    var result = new BinaryOpNode(MathOp.Multiply, b.Right.DeepCopy(), integratedLeft);
                    steps?.Add(new DerivationStep(
                        "Constant Multiple Rule",
                        $"∫ ({b.Left}) · {b.Right} d{variable}",
                        result.ToString(),
                        $"Factor constant factor {b.Right} out of the integral: ∫ f(x)·c dx = c·∫ f(x)dx."
                    ));
                    return result;
                }

                // If both are x: x * x = x^2 => x^3 / 3
                if (IsVariable(b.Left, variable) && IsVariable(b.Right, variable))
                {
                    var result = new BinaryOpNode(MathOp.Divide,
                        new BinaryOpNode(MathOp.Power, new VariableNode(variable), new NumberNode(3)),
                        new NumberNode(3));
                    steps?.Add(new DerivationStep(
                        "Monomial Product Rule",
                        $"∫ ({variable} · {variable}) d{variable}",
                        result.ToString(),
                        $"Recognize {variable}·{variable} = {variable}^2, apply power rule: {variable}^3 / 3."
                    ));
                    return result;
                }

                // Fallback: entire product independent of x
                if (!leftHasVar && !rightHasVar)
                {
                    return new BinaryOpNode(MathOp.Multiply, b.DeepCopy(), new VariableNode(variable));
                }
            }

            // Power Rule: x^n => x^(n+1) / (n+1)
            if (b.Op == MathOp.Power)
            {
                if (IsVariable(b.Left, variable) && !ContainsVariable(b.Right, variable))
                {
                    if (b.Right is NumberNode numExp)
                    {
                        if (Math.Abs(numExp.Value - (-1.0)) < 1e-9)
                        {
                            // ∫ x^(-1) dx = ln|x|
                            var lnResult = new FunctionCallNode("ln", new List<MathNode> { new FunctionCallNode("abs", new List<MathNode> { new VariableNode(variable) }) });
                            steps?.Add(new DerivationStep(
                                "Reciprocal Rule",
                                $"∫ {variable}^(-1) d{variable}",
                                lnResult.ToString(),
                                $"The integral of {variable}^(-1) is ln|{variable}|."
                            ));
                            return lnResult;
                        }

                        double newExp = numExp.Value + 1.0;
                        var result = new BinaryOpNode(MathOp.Divide,
                            new BinaryOpNode(MathOp.Power, new VariableNode(variable), new NumberNode(newExp)),
                            new NumberNode(newExp));
                        steps?.Add(new DerivationStep(
                            "Power Rule of Integration",
                            $"∫ {variable}^{numExp.Value} d{variable}",
                            result.ToString(),
                            $"∫ {variable}^n d{variable} = {variable}^(n+1) / (n+1) for n ≠ -1."
                        ));
                        return result;
                    }
                }

                // Exponential rule: e^(k*x) => (1/k) * e^(k*x)
                if (IsE(b.Left) && ContainsVariable(b.Right, variable))
                {
                    double k = ExtractLinearCoeff(b.Right, variable, out bool isLinear);
                    if (isLinear && k != 0.0)
                    {
                        MathNode expPart = new BinaryOpNode(MathOp.Power, new VariableNode("e"), b.Right.DeepCopy());
                        MathNode result = k == 1.0
                            ? expPart
                            : new BinaryOpNode(MathOp.Divide, expPart, new NumberNode(k));
                        steps?.Add(new DerivationStep(
                            "Exponential Integration Rule",
                            $"∫ e^({b.Right}) d{variable}",
                            result.ToString(),
                            $"∫ e^(kx) dx = (1/k) · e^(kx)."
                        ));
                        return result;
                    }
                }
            }

            // Division: f(x) / c or c / x
            if (b.Op == MathOp.Divide)
            {
                bool numHasVar = ContainsVariable(b.Left, variable);
                bool denHasVar = ContainsVariable(b.Right, variable);

                if (numHasVar && !denHasVar)
                {
                    var integratedNum = IntegrateInternal(b.Left, variable, steps);
                    return new BinaryOpNode(MathOp.Divide, integratedNum, b.Right.DeepCopy());
                }

                if (!numHasVar && IsVariable(b.Right, variable))
                {
                    // ∫ c / x dx = c * ln|x|
                    var lnNode = new FunctionCallNode("ln", new List<MathNode> { new FunctionCallNode("abs", new List<MathNode> { new VariableNode(variable) }) });
                    var result = new BinaryOpNode(MathOp.Multiply, b.Left.DeepCopy(), lnNode);
                    steps?.Add(new DerivationStep(
                        "Logarithmic Antiderivative Rule",
                        $"∫ ({b.Left} / {variable}) d{variable}",
                        result.ToString(),
                        $"∫ c/{variable} d{variable} = c · ln|{variable}|."
                    ));
                    return result;
                }
            }
        }

        // 5. Function calls: sin(kx), cos(kx), exp(kx), etc.
        if (node is FunctionCallNode f && f.Arguments.Count > 0)
        {
            var arg = f.Arguments[0];
            double k = ExtractLinearCoeff(arg, variable, out bool isLinear);

            if (isLinear && k != 0.0)
            {
                string fnName = f.Name.ToLowerInvariant();
                switch (fnName)
                {
                    case "sin":
                    {
                        // ∫ sin(kx) dx = - (1/k) * cos(kx)
                        MathNode cosPart = new FunctionCallNode("cos", new List<MathNode> { arg.DeepCopy() });
                        MathNode divPart = k == 1.0 ? cosPart : new BinaryOpNode(MathOp.Divide, cosPart, new NumberNode(k));
                        MathNode result = new UnaryOpNode(MathUnaryOp.Negate, divPart);
                        steps?.Add(new DerivationStep(
                            "Trigonometric Antiderivative (Sine)",
                            $"∫ sin({arg}) d{variable}",
                            result.ToString(),
                            $"∫ sin(kx) dx = - (1/k) · cos(kx)."
                        ));
                        return result;
                    }

                    case "cos":
                    {
                        // ∫ cos(kx) dx = (1/k) * sin(kx)
                        MathNode sinPart = new FunctionCallNode("sin", new List<MathNode> { arg.DeepCopy() });
                        MathNode result = k == 1.0 ? sinPart : new BinaryOpNode(MathOp.Divide, sinPart, new NumberNode(k));
                        steps?.Add(new DerivationStep(
                            "Trigonometric Antiderivative (Cosine)",
                            $"∫ cos({arg}) d{variable}",
                            result.ToString(),
                            $"∫ cos(kx) dx = (1/k) · sin(kx)."
                        ));
                        return result;
                    }

                    case "exp":
                    {
                        // ∫ exp(kx) dx = (1/k) * exp(kx)
                        MathNode expPart = new FunctionCallNode("exp", new List<MathNode> { arg.DeepCopy() });
                        MathNode result = k == 1.0 ? expPart : new BinaryOpNode(MathOp.Divide, expPart, new NumberNode(k));
                        steps?.Add(new DerivationStep(
                            "Exponential Function Antiderivative",
                            $"∫ exp({arg}) d{variable}",
                            result.ToString(),
                            $"∫ exp(kx) dx = (1/k) · exp(kx)."
                        ));
                        return result;
                    }

                    case "sinh":
                    {
                        MathNode coshPart = new FunctionCallNode("cosh", new List<MathNode> { arg.DeepCopy() });
                        return k == 1.0 ? coshPart : new BinaryOpNode(MathOp.Divide, coshPart, new NumberNode(k));
                    }

                    case "cosh":
                    {
                        MathNode sinhPart = new FunctionCallNode("sinh", new List<MathNode> { arg.DeepCopy() });
                        return k == 1.0 ? sinhPart : new BinaryOpNode(MathOp.Divide, sinhPart, new NumberNode(k));
                    }

                    case "sqrt" when k == 1.0 && arg is VariableNode:
                    {
                        // ∫ sqrt(x) dx = (2/3) * x^(3/2)
                        var result = new BinaryOpNode(MathOp.Multiply,
                            new NumberNode(2.0 / 3.0),
                            new BinaryOpNode(MathOp.Power, new VariableNode(variable), new NumberNode(1.5)));
                        steps?.Add(new DerivationStep(
                            "Fractional Power Rule",
                            $"∫ √{variable} d{variable}",
                            result.ToString(),
                            $"Rewrite as {variable}^(1/2): ∫ {variable}^(1/2) d{variable} = (2/3) · {variable}^(3/2)."
                        ));
                        return result;
                    }
                }
            }
        }

        // Fallback: If node has no variable dependence, it is constant
        if (!ContainsVariable(node, variable))
        {
            return new BinaryOpNode(MathOp.Multiply, node.DeepCopy(), new VariableNode(variable));
        }

        // Unknown or non-elementary closed form
        return new FunctionCallNode("∫", new List<MathNode> { node.DeepCopy(), new VariableNode(variable) });
    }

    private static bool ContainsVariable(MathNode node, string variable)
    {
        switch (node)
        {
            case VariableNode v:
                return string.Equals(v.Name, variable, StringComparison.OrdinalIgnoreCase);
            case BinaryOpNode b:
                return ContainsVariable(b.Left, variable) || ContainsVariable(b.Right, variable);
            case UnaryOpNode u:
                return ContainsVariable(u.Operand, variable);
            case FunctionCallNode f:
                foreach (var arg in f.Arguments)
                {
                    if (ContainsVariable(arg, variable)) return true;
                }
                return false;
            default:
                return false;
        }
    }

    private static bool IsVariable(MathNode node, string variable)
    {
        return node is VariableNode v && string.Equals(v.Name, variable, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsE(MathNode node)
    {
        return node is VariableNode v && string.Equals(v.Name, "e", StringComparison.OrdinalIgnoreCase);
    }

    private static double ExtractLinearCoeff(MathNode node, string variable, out bool isLinear)
    {
        if (IsVariable(node, variable))
        {
            isLinear = true;
            return 1.0;
        }

        if (node is BinaryOpNode b && b.Op == MathOp.Multiply)
        {
            if (b.Left is NumberNode nl && IsVariable(b.Right, variable))
            {
                isLinear = true;
                return nl.Value;
            }
            if (IsVariable(b.Left, variable) && b.Right is NumberNode nr)
            {
                isLinear = true;
                return nr.Value;
            }
        }

        if (node is UnaryOpNode u && u.Op == MathUnaryOp.Negate && IsVariable(u.Operand, variable))
        {
            isLinear = true;
            return -1.0;
        }

        isLinear = false;
        return 0.0;
    }
}
