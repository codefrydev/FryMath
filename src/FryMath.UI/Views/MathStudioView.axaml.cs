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
        }

        if (DataContext is MainWindowViewModel vm)
        {
            _currentVm = vm;
            _currentVm.PlinkoBurstRequested += OnPlinkoBurst;
            _currentVm.PlinkoResetRequested += OnPlinkoReset;
            _currentVm.VectorFieldPresetRequested += OnVectorFieldPreset;
        }
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
