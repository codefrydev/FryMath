using System;
using System.Collections.Generic;
using System.Text;
using FryMath.Core.CAS;

namespace FryMath.Core.Calculus;

public static class TaylorSeries
{
    public static (Func<double, double> PolynomialFunc, string Formula) GenerateTaylorSeries(
        MathNode node,
        double centerA = 0.0,
        int degree = 4,
        string variable = "x")
    {
        degree = Math.Clamp(degree, 0, 12);
        var coeffs = new double[degree + 1];

        var currDeriv = node;
        double factorial = 1.0;

        for (int k = 0; k <= degree; k++)
        {
            if (k > 1) factorial *= k;

            double valAtCenter = 0.0;
            try
            {
                var dict = new Dictionary<string, double> { [variable] = centerA };
                valAtCenter = currDeriv.Evaluate(dict);
            }
            catch
            {
                valAtCenter = 0.0;
            }

            coeffs[k] = valAtCenter / factorial;

            if (k < degree)
            {
                currDeriv = SymbolicDifferentiator.Differentiate(currDeriv, variable);
            }
        }

        Func<double, double> polyFunc = x =>
        {
            double sum = 0.0;
            double dx = x - centerA;
            double power = 1.0;

            for (int k = 0; k <= degree; k++)
            {
                sum += coeffs[k] * power;
                power *= dx;
            }
            return sum;
        };

        var sb = new StringBuilder();
        bool first = true;
        for (int k = 0; k <= degree; k++)
        {
            double c = Math.Round(coeffs[k], 4);
            if (Math.Abs(c) < 1e-6) continue;

            if (c > 0 && !first) sb.Append(" + ");
            else if (c < 0) sb.Append(first ? "-" : " - ");

            double absC = Math.Abs(c);
            if (k == 0)
            {
                sb.Append(absC);
            }
            else
            {
                if (Math.Abs(absC - 1.0) > 1e-6) sb.Append(absC);
                string term = centerA == 0.0 ? variable : $"({variable} - {centerA})";
                sb.Append(k == 1 ? term : $"{term}^{k}");
            }

            first = false;
        }

        string formula = sb.Length == 0 ? "0" : sb.ToString();
        return (polyFunc, formula);
    }
}
