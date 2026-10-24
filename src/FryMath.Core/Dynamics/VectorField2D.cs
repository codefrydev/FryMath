using System;
using System.Collections.Generic;
using FryMath.Core.CAS;
using FryMath.Core.LinearAlgebra;

namespace FryMath.Core.Dynamics;

/// <summary>
/// Represents a 2D vector field F(x, y) = &lt;P(x, y), Q(x, y)&gt; or an autonomous system of ODEs:
/// dx/dt = P(x, y), dy/dt = Q(x, y).
/// </summary>
public sealed class VectorField2D
{
    public string ExprP { get; private set; } = "-y";
    public string ExprQ { get; private set; } = "x";

    public Func<double, double, double>? FuncP { get; private set; }
    public Func<double, double, double>? FuncQ { get; private set; }

    public MathNode? AstP { get; private set; }
    public MathNode? AstQ { get; private set; }

    public string? SyntaxErrorP { get; private set; }
    public string? SyntaxErrorQ { get; private set; }

    public VectorField2D(string exprP = "-y", string exprQ = "x")
    {
        Compile(exprP, exprQ);
    }

    public void Compile(string exprP, string exprQ, IReadOnlyDictionary<string, double>? constants = null)
    {
        ExprP = exprP;
        ExprQ = exprQ;

        try
        {
            AstP = MathParser.Parse(exprP);
            FuncP = ExpressionCompiler.Compile2D(AstP, "x", "y", constants);
            SyntaxErrorP = null;
        }
        catch (Exception ex)
        {
            SyntaxErrorP = ex.Message;
            FuncP = (_, _) => 0.0;
        }

        try
        {
            AstQ = MathParser.Parse(exprQ);
            FuncQ = ExpressionCompiler.Compile2D(AstQ, "x", "y", constants);
            SyntaxErrorQ = null;
        }
        catch (Exception ex)
        {
            SyntaxErrorQ = ex.Message;
            FuncQ = (_, _) => 0.0;
        }
    }

    public Vector2D Evaluate(double x, double y)
    {
        double u = FuncP?.Invoke(x, y) ?? 0.0;
        double v = FuncQ?.Invoke(x, y) ?? 0.0;
        if (double.IsNaN(u) || double.IsInfinity(u)) u = 0.0;
        if (double.IsNaN(v) || double.IsInfinity(v)) v = 0.0;
        return new Vector2D(u, v);
    }

    /// <summary>
    /// Computes local 2D divergence: div(F) = dP/dx + dQ/dy.
    /// Positive = Source (fluid expansion), Negative = Sink (fluid contraction).
    /// </summary>
    public double ComputeDivergence(double x, double y, double h = 1e-4)
    {
        if (FuncP == null || FuncQ == null) return 0.0;
        double dp_dx = (FuncP(x + h, y) - FuncP(x - h, y)) / (2.0 * h);
        double dq_dy = (FuncQ(x, y + h) - FuncQ(x, y - h)) / (2.0 * h);
        double div = dp_dx + dq_dy;
        return double.IsNaN(div) ? 0.0 : div;
    }

    /// <summary>
    /// Computes 2D scalar curl (vorticity): curl(F) = dQ/dx - dP/dy.
    /// Measures rotational circulation / spinning tendency at (x, y).
    /// </summary>
    public double ComputeCurl(double x, double y, double h = 1e-4)
    {
        if (FuncP == null || FuncQ == null) return 0.0;
        double dq_dx = (FuncQ(x + h, y) - FuncQ(x - h, y)) / (2.0 * h);
        double dp_dy = (FuncP(x, y + h) - FuncP(x, y - h)) / (2.0 * h);
        double curl = dq_dx - dp_dy;
        return double.IsNaN(curl) ? 0.0 : curl;
    }

    /// <summary>
    /// Integrates a single trajectory forward in time using 4th Order Runge-Kutta (RK4).
    /// </summary>
    public List<Vector2D> TraceTrajectory(Vector2D start, double dt = 0.02, int steps = 300)
    {
        var pts = new List<Vector2D>(steps);
        var curr = start;
        pts.Add(curr);

        for (int i = 0; i < steps; i++)
        {
            var k1 = Evaluate(curr.X, curr.Y);
            var k2 = Evaluate(curr.X + 0.5 * dt * k1.X, curr.Y + 0.5 * dt * k1.Y);
            var k3 = Evaluate(curr.X + 0.5 * dt * k2.X, curr.Y + 0.5 * dt * k2.Y);
            var k4 = Evaluate(curr.X + dt * k3.X, curr.Y + dt * k3.Y);

            var next = curr + (k1 + k2 * 2.0 + k3 * 2.0 + k4) * (dt / 6.0);
            if (double.IsNaN(next.X) || double.IsNaN(next.Y) || next.Length > 80.0)
                break;

            curr = next;
            pts.Add(curr);
        }

        return pts;
    }
}
