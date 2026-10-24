using System;
using System.Collections.Generic;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FryMath.Core.CAS;

namespace FryMath.UI.ViewModels;

public partial class FunctionItemViewModel : ObservableObject
{
    public string Id { get; }

    [ObservableProperty]
    private string _name; // e.g. "f(x)", "g(x)"

    [ObservableProperty]
    private string _expressionText;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _showDerivative = false;

    [ObservableProperty]
    private IBrush _color;

    [ObservableProperty]
    private string? _syntaxError;

    [ObservableProperty]
    private string _analyticalDerivative = string.Empty;

    [ObservableProperty]
    private string _analyticalAntiderivative = string.Empty;

    public List<DerivationStep> DerivationSteps { get; } = new();
    public List<DerivationStep> AntiderivativeSteps { get; } = new();

    public MathNode? Ast { get; private set; }
    public Func<double, double>? CompiledFunc { get; private set; }
    public Func<double, double>? CompiledDerivative { get; private set; }

    public event EventHandler? FunctionChanged;

    public FunctionItemViewModel(string id, string name, string expression, IBrush color)
    {
        Id = id;
        _name = name;
        _expressionText = expression;
        _color = color;
        Recompile(new Dictionary<string, double>());
    }

    partial void OnExpressionTextChanged(string value)
    {
        FunctionChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnIsVisibleChanged(bool value)
    {
        FunctionChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnShowDerivativeChanged(bool value)
    {
        FunctionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Recompile(IReadOnlyDictionary<string, double> constants)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ExpressionText))
            {
                SyntaxError = "Empty expression";
                Ast = null;
                CompiledFunc = null;
                CompiledDerivative = null;
                AnalyticalDerivative = string.Empty;
                AnalyticalAntiderivative = string.Empty;
                DerivationSteps.Clear();
                AntiderivativeSteps.Clear();
                return;
            }

            Ast = MathParser.Parse(ExpressionText);
            CompiledFunc = ExpressionCompiler.Compile1D(Ast, "x", constants);

            DerivationSteps.Clear();
            var derivAst = SymbolicDifferentiator.Differentiate(Ast, "x", DerivationSteps);
            AnalyticalDerivative = derivAst.ToString();
            CompiledDerivative = ExpressionCompiler.Compile1D(derivAst, "x", constants);

            AntiderivativeSteps.Clear();
            try
            {
                var antiAst = SymbolicIntegrator.Integrate(Ast, "x", AntiderivativeSteps);
                AnalyticalAntiderivative = antiAst.ToString() + " + C";
            }
            catch
            {
                AnalyticalAntiderivative = "None (non-elementary)";
            }

            SyntaxError = null;
        }
        catch (Exception ex)
        {
            SyntaxError = ex.Message;
            CompiledFunc = null;
            CompiledDerivative = null;
            AnalyticalDerivative = "Error";
            AnalyticalAntiderivative = "None";
        }
    }
}
