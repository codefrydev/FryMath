using System;
using System.Collections.Generic;

namespace FryMath.Core.CAS;

public static class MathSimplifier
{
    public static MathNode Simplify(MathNode node)
    {
        if (node is NumberNode or VariableNode) return node;

        if (node is UnaryOpNode u)
        {
            var op = Simplify(u.Operand);
            if (op is NumberNode num)
            {
                return new NumberNode(u.Op == MathUnaryOp.Negate ? -num.Value : num.Value);
            }
            if (u.Op == MathUnaryOp.Plus) return op;
            if (op is UnaryOpNode innerU && innerU.Op == MathUnaryOp.Negate)
            {
                return innerU.Operand; // -(-x) = x
            }
            return new UnaryOpNode(u.Op, op);
        }

        if (node is FunctionCallNode f)
        {
            var args = new List<MathNode>();
            bool allConst = true;
            foreach (var a in f.Arguments)
            {
                var sim = Simplify(a);
                args.Add(sim);
                if (sim is not NumberNode) allConst = false;
            }

            if (allConst && args.Count > 0)
            {
                try
                {
                    double res = f.DeepCopy() switch
                    {
                        FunctionCallNode fc => new FunctionCallNode(fc.Name, args).Evaluate(),
                        _ => 0
                    };
                    if (!double.IsNaN(res) && !double.IsInfinity(res))
                    {
                        return new NumberNode(Math.Round(res, 10));
                    }
                }
                catch { /* Ignore evaluation failure in simplification */ }
            }

            return new FunctionCallNode(f.Name, args);
        }

        if (node is BinaryOpNode b)
        {
            var l = Simplify(b.Left);
            var r = Simplify(b.Right);

            // Constant folding
            if (l is NumberNode nl && r is NumberNode nr)
            {
                try
                {
                    double res = b.Op switch
                    {
                        MathOp.Add => nl.Value + nr.Value,
                        MathOp.Subtract => nl.Value - nr.Value,
                        MathOp.Multiply => nl.Value * nr.Value,
                        MathOp.Divide when nr.Value != 0 => nl.Value / nr.Value,
                        MathOp.Power => Math.Pow(nl.Value, nr.Value),
                        _ => double.NaN
                    };
                    if (!double.IsNaN(res) && !double.IsInfinity(res))
                    {
                        return new NumberNode(Math.Round(res, 10));
                    }
                }
                catch { /* Ignore */ }
            }

            // Algebraic identities
            switch (b.Op)
            {
                case MathOp.Add:
                    // 0 + r = r
                    if (IsZero(l)) return r;
                    // l + 0 = l
                    if (IsZero(r)) return l;
                    break;

                case MathOp.Subtract:
                    // l - 0 = l
                    if (IsZero(r)) return l;
                    // 0 - r = -r
                    if (IsZero(l)) return new UnaryOpNode(MathUnaryOp.Negate, r);
                    // l - l = 0 (if structurally equal)
                    if (l.ToString() == r.ToString()) return new NumberNode(0);
                    break;

                case MathOp.Multiply:
                    // 0 * r = 0 or l * 0 = 0
                    if (IsZero(l) || IsZero(r)) return new NumberNode(0);
                    // 1 * r = r
                    if (IsOne(l)) return r;
                    // l * 1 = l
                    if (IsOne(r)) return l;
                    // -1 * r = -r
                    if (IsNegativeOne(l)) return new UnaryOpNode(MathUnaryOp.Negate, r);
                    if (IsNegativeOne(r)) return new UnaryOpNode(MathUnaryOp.Negate, l);
                    break;

                case MathOp.Divide:
                    // 0 / r = 0 (if r != 0)
                    if (IsZero(l) && !IsZero(r)) return new NumberNode(0);
                    // l / 1 = l
                    if (IsOne(r)) return l;
                    // l / l = 1 (if structurally equal and not 0)
                    if (l.ToString() == r.ToString() && !IsZero(l)) return new NumberNode(1);
                    break;

                case MathOp.Power:
                    // l ^ 0 = 1
                    if (IsZero(r)) return new NumberNode(1);
                    // l ^ 1 = l
                    if (IsOne(r)) return l;
                    // 0 ^ r = 0 (if r > 0)
                    if (IsZero(l) && r is NumberNode { Value: > 0 }) return new NumberNode(0);
                    // 1 ^ r = 1
                    if (IsOne(l)) return new NumberNode(1);
                    break;
            }

            return new BinaryOpNode(b.Op, l, r);
        }

        return node;
    }

    private static bool IsZero(MathNode node) => node is NumberNode { Value: 0.0 };
    private static bool IsOne(MathNode node) => node is NumberNode { Value: 1.0 };
    private static bool IsNegativeOne(MathNode node) => node is NumberNode { Value: -1.0 };
}
