using System;
using System.Collections.Generic;

namespace FryMath.Core.CAS;

public sealed record DerivationStep(string RuleName, string ExpressionBefore, string ExpressionAfter, string Rationale);

public static class SymbolicDifferentiator
{
    public static MathNode Differentiate(MathNode node, string variable = "x", List<DerivationStep>? steps = null)
    {
        var rawResult = DifferentiateInternal(node, variable, steps);
        var simplified = MathSimplifier.Simplify(rawResult);

        if (steps != null && rawResult.ToString() != simplified.ToString())
        {
            steps.Add(new DerivationStep(
                "Algebraic Simplification",
                rawResult.ToString(),
                simplified.ToString(),
                "Simplify terms, cancel common factors, and eliminate zero/identity operations."
            ));
        }

        return simplified;
    }

    private static MathNode DifferentiateInternal(MathNode node, string variable, List<DerivationStep>? steps)
    {
        switch (node)
        {
            case NumberNode:
            {
                var result = new NumberNode(0);
                steps?.Add(new DerivationStep(
                    "Constant Rule",
                    $"d/d{variable} ({node})",
                    "0",
                    "The derivative of a constant is 0."
                ));
                return result;
            }

            case VariableNode v:
            {
                if (string.Equals(v.Name, variable, StringComparison.OrdinalIgnoreCase))
                {
                    var result = new NumberNode(1);
                    steps?.Add(new DerivationStep(
                        "Variable Rule",
                        $"d/d{variable} ({v.Name})",
                        "1",
                        $"The derivative of {variable} with respect to itself is 1."
                    ));
                    return result;
                }
                else
                {
                    var result = new NumberNode(0);
                    steps?.Add(new DerivationStep(
                        "Independent Variable Rule",
                        $"d/d{variable} ({v.Name})",
                        "0",
                        $"Treat '{v.Name}' as a constant with respect to {variable}."
                    ));
                    return result;
                }
            }

            case UnaryOpNode u:
            {
                var innerDiff = DifferentiateInternal(u.Operand, variable, steps);
                return new UnaryOpNode(u.Op, innerDiff);
            }

            case BinaryOpNode b:
            {
                switch (b.Op)
                {
                    case MathOp.Add:
                    {
                        var dl = DifferentiateInternal(b.Left, variable, steps);
                        var dr = DifferentiateInternal(b.Right, variable, steps);
                        var res = new BinaryOpNode(MathOp.Add, dl, dr);
                        steps?.Add(new DerivationStep(
                            "Sum Rule",
                            $"d/d{variable} ({b.Left} + {b.Right})",
                            $"{dl} + {dr}",
                            "d/dx [u + v] = d/dx[u] + d/dx[v]"
                        ));
                        return res;
                    }

                    case MathOp.Subtract:
                    {
                        var dl = DifferentiateInternal(b.Left, variable, steps);
                        var dr = DifferentiateInternal(b.Right, variable, steps);
                        var res = new BinaryOpNode(MathOp.Subtract, dl, dr);
                        steps?.Add(new DerivationStep(
                            "Difference Rule",
                            $"d/d{variable} ({b.Left} - {b.Right})",
                            $"{dl} - {dr}",
                            "d/dx [u - v] = d/dx[u] - d/dx[v]"
                        ));
                        return res;
                    }

                    case MathOp.Multiply:
                    {
                        // Product Rule: (u * v)' = u' * v + u * v'
                        var u = b.Left;
                        var v = b.Right;
                        var du = DifferentiateInternal(u, variable, steps);
                        var dv = DifferentiateInternal(v, variable, steps);

                        var res = new BinaryOpNode(
                            MathOp.Add,
                            new BinaryOpNode(MathOp.Multiply, du, v.DeepCopy()),
                            new BinaryOpNode(MathOp.Multiply, u.DeepCopy(), dv)
                        );

                        steps?.Add(new DerivationStep(
                            "Product Rule",
                            $"d/d{variable} [({u}) * ({v})]",
                            res.ToString(),
                            "d/dx [u * v] = u' * v + u * v'"
                        ));
                        return res;
                    }

                    case MathOp.Divide:
                    {
                        // Quotient Rule: (u / v)' = (u' * v - u * v') / v^2
                        var u = b.Left;
                        var v = b.Right;
                        var du = DifferentiateInternal(u, variable, steps);
                        var dv = DifferentiateInternal(v, variable, steps);

                        var numerator = new BinaryOpNode(
                            MathOp.Subtract,
                            new BinaryOpNode(MathOp.Multiply, du, v.DeepCopy()),
                            new BinaryOpNode(MathOp.Multiply, u.DeepCopy(), dv)
                        );
                        var denominator = new BinaryOpNode(MathOp.Power, v.DeepCopy(), new NumberNode(2));
                        var res = new BinaryOpNode(MathOp.Divide, numerator, denominator);

                        steps?.Add(new DerivationStep(
                            "Quotient Rule",
                            $"d/d{variable} [({u}) / ({v})]",
                            res.ToString(),
                            "d/dx [u / v] = (u' * v - u * v') / v²"
                        ));
                        return res;
                    }

                    case MathOp.Power:
                    {
                        var u = b.Left;
                        var v = b.Right;

                        // Case 1: u^n where n is constant (Standard Power Rule + Chain Rule)
                        if (v is NumberNode num)
                        {
                            double n = num.Value;
                            var du = DifferentiateInternal(u, variable, steps);

                            // n * u^(n-1) * du
                            MathNode powerTerm = n == 1.0
                                ? (MathNode)new NumberNode(1)
                                : new BinaryOpNode(MathOp.Power, u.DeepCopy(), new NumberNode(n - 1));

                            var res = new BinaryOpNode(
                                MathOp.Multiply,
                                new BinaryOpNode(MathOp.Multiply, new NumberNode(n), powerTerm),
                                du
                            );

                            steps?.Add(new DerivationStep(
                                "Power Rule & Chain Rule",
                                $"d/d{variable} [({u})^{n}]",
                                res.ToString(),
                                $"d/dx [u^n] = n * u^(n-1) * du/dx"
                            ));
                            return res;
                        }

                        // Case 2: a^v where a is constant (Exponential Rule)
                        if (u is NumberNode baseNum && baseNum.Value > 0)
                        {
                            var dv = DifferentiateInternal(v, variable, steps);
                            // a^v * ln(a) * dv
                            var res = new BinaryOpNode(
                                MathOp.Multiply,
                                new BinaryOpNode(MathOp.Multiply, b.DeepCopy(), new NumberNode(Math.Log(baseNum.Value))),
                                dv
                            );
                            steps?.Add(new DerivationStep(
                                "Exponential Base Rule",
                                $"d/d{variable} [{baseNum.Value}^({v})]",
                                res.ToString(),
                                $"d/dx [a^v] = a^v * ln(a) * dv/dx"
                            ));
                            return res;
                        }

                        // General case: u^v = exp(v * ln(u))
                        var duGen = DifferentiateInternal(u, variable, steps);
                        var dvGen = DifferentiateInternal(v, variable, steps);

                        // u^v * (v' * ln(u) + v * u' / u)
                        var term1 = new BinaryOpNode(MathOp.Multiply, dvGen, new FunctionCallNode("ln", new[] { u.DeepCopy() }));
                        var term2 = new BinaryOpNode(MathOp.Divide, new BinaryOpNode(MathOp.Multiply, v.DeepCopy(), duGen), u.DeepCopy());
                        var bracket = new BinaryOpNode(MathOp.Add, term1, term2);
                        var generalRes = new BinaryOpNode(MathOp.Multiply, b.DeepCopy(), bracket);

                        steps?.Add(new DerivationStep(
                            "Logarithmic Differentiation",
                            $"d/d{variable} [({u})^({v})]",
                            generalRes.ToString(),
                            "d/dx [u^v] = u^v * [v' * ln(u) + v * u' / u]"
                        ));
                        return generalRes;
                    }
                }
                break;
            }

            case FunctionCallNode f:
            {
                if (f.Arguments.Count == 0) return new NumberNode(0);
                var arg = f.Arguments[0];
                var darg = DifferentiateInternal(arg, variable, steps);

                string fn = f.Name.ToLowerInvariant();
                MathNode derivCore = fn switch
                {
                    "sin" => new FunctionCallNode("cos", new[] { arg.DeepCopy() }),
                    "cos" => new UnaryOpNode(MathUnaryOp.Negate, new FunctionCallNode("sin", new[] { arg.DeepCopy() })),
                    "tan" => new BinaryOpNode(MathOp.Divide, new NumberNode(1),
                                new BinaryOpNode(MathOp.Power, new FunctionCallNode("cos", new[] { arg.DeepCopy() }), new NumberNode(2))),
                    "exp" => new FunctionCallNode("exp", new[] { arg.DeepCopy() }),
                    "ln" or "log" => new BinaryOpNode(MathOp.Divide, new NumberNode(1), arg.DeepCopy()),
                    "sqrt" => new BinaryOpNode(MathOp.Divide, new NumberNode(1),
                                new BinaryOpNode(MathOp.Multiply, new NumberNode(2), new FunctionCallNode("sqrt", new[] { arg.DeepCopy() }))),
                    "sinh" => new FunctionCallNode("cosh", new[] { arg.DeepCopy() }),
                    "cosh" => new FunctionCallNode("sinh", new[] { arg.DeepCopy() }),
                    "tanh" => new BinaryOpNode(MathOp.Subtract, new NumberNode(1),
                                new BinaryOpNode(MathOp.Power, new FunctionCallNode("tanh", new[] { arg.DeepCopy() }), new NumberNode(2))),
                    "asin" or "arcsin" => new BinaryOpNode(MathOp.Divide, new NumberNode(1),
                                new FunctionCallNode("sqrt", new[] {
                                    new BinaryOpNode(MathOp.Subtract, new NumberNode(1),
                                        new BinaryOpNode(MathOp.Power, arg.DeepCopy(), new NumberNode(2)))
                                })),
                    "acos" or "arccos" => new UnaryOpNode(MathUnaryOp.Negate,
                                new BinaryOpNode(MathOp.Divide, new NumberNode(1),
                                new FunctionCallNode("sqrt", new[] {
                                    new BinaryOpNode(MathOp.Subtract, new NumberNode(1),
                                        new BinaryOpNode(MathOp.Power, arg.DeepCopy(), new NumberNode(2)))
                                }))),
                    "atan" or "arctan" => new BinaryOpNode(MathOp.Divide, new NumberNode(1),
                                new BinaryOpNode(MathOp.Add, new NumberNode(1),
                                    new BinaryOpNode(MathOp.Power, arg.DeepCopy(), new NumberNode(2)))),
                    _ => throw new NotSupportedException($"Derivative of function '{f.Name}' is not supported.")
                };

                // Chain Rule: d/dx [f(u)] = f'(u) * u'
                var res = new BinaryOpNode(MathOp.Multiply, derivCore, darg);

                steps?.Add(new DerivationStep(
                    $"Chain Rule: d/d{variable} [{f.Name}(u)]",
                    $"d/d{variable} [{f}]",
                    res.ToString(),
                    $"d/dx [{f.Name}(u)] = {f.Name}'(u) * du/dx"
                ));
                return res;
            }
        }

        return new NumberNode(0);
    }
}
