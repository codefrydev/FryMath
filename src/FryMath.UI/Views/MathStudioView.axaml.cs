using System;
using Avalonia.Controls;
using FryMath.UI.Controls;
using FryMath.UI.ViewModels;

namespace FryMath.UI.Views;

public partial class MathStudioView : UserControl
{
    private MainWindowViewModel? _currentVm;

    public MathStudioView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_currentVm != null)
        {
            _currentVm.PlinkoBurstRequested -= OnPlinkoBurst;
            _currentVm.PlinkoResetRequested -= OnPlinkoReset;
            _currentVm.VectorFieldPresetRequested -= OnVectorFieldPreset;
            _currentVm.CanvasResetViewRequested -= OnCanvasReset;
            _currentVm.CanvasZoomInRequested -= OnCanvasZoomIn;
            _currentVm.CanvasZoomOutRequested -= OnCanvasZoomOut;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            _currentVm = vm;
            _currentVm.PlinkoBurstRequested += OnPlinkoBurst;
            _currentVm.PlinkoResetRequested += OnPlinkoReset;
            _currentVm.VectorFieldPresetRequested += OnVectorFieldPreset;
            _currentVm.CanvasResetViewRequested += OnCanvasReset;
            _currentVm.CanvasZoomInRequested += OnCanvasZoomIn;
            _currentVm.CanvasZoomOutRequested += OnCanvasZoomOut;
        }
    }

    private void OnCanvasReset()
    {
        var canvas2d = this.FindControl<MathCanvas2D>("CalculusCanvas");
        canvas2d?.ResetView();
        var fractal = this.FindControl<FractalCanvas>("FractalCanvas");
        fractal?.ResetView();
    }

    private void OnCanvasZoomIn()
    {
        var canvas2d = this.FindControl<MathCanvas2D>("CalculusCanvas");
        canvas2d?.ZoomIn();
    }

    private void OnCanvasZoomOut()
    {
        var canvas2d = this.FindControl<MathCanvas2D>("CalculusCanvas");
        canvas2d?.ZoomOut();
    }

    private void OnPlinkoBurst()
    {
        var canvas = this.FindControl<GaltonBoardCanvas>("GaltonCanvas");
        canvas?.DropBurst(35);
    }

    private void OnPlinkoReset()
    {
        var canvas = this.FindControl<GaltonBoardCanvas>("GaltonCanvas");
        canvas?.ResetSimulation();
    }

    private void OnVectorFieldPreset(string p, string q)
    {
        var canvas = this.FindControl<VectorFieldCanvas>("VectorCanvas");
        canvas?.SetPreset(p, q);
    }
}
