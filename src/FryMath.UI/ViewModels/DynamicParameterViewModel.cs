using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FryMath.UI.ViewModels;

public partial class DynamicParameterViewModel : ObservableObject
{
    public string Name { get; }

    [ObservableProperty]
    private double _value;

    [ObservableProperty]
    private double _minimum;

    [ObservableProperty]
    private double _maximum;

    [ObservableProperty]
    private double _step = 0.1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseIcon))]
    private bool _isAnimating = false;

    public string PlayPauseIcon => IsAnimating ? "Pause" : "Play";

    private double _direction = 1.0;
    private double _speed = 1.5; // units per second

    public event EventHandler? ValueChanged;

    public DynamicParameterViewModel(string name, double initialValue, double min = -5.0, double max = 5.0, double step = 0.1)
    {
        Name = name;
        _value = initialValue;
        _minimum = min;
        _maximum = max;
        _step = step;
    }

    partial void OnValueChanged(double value)
    {
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void ToggleAnimation()
    {
        IsAnimating = !IsAnimating;
    }

    public void StepAnimation(double deltaSeconds)
    {
        if (!IsAnimating) return;

        double next = Value + _direction * _speed * deltaSeconds;
        if (next >= Maximum)
        {
            next = Maximum;
            _direction = -1.0;
        }
        else if (next <= Minimum)
        {
            next = Minimum;
            _direction = 1.0;
        }

        Value = Math.Round(next, 2);
    }
}
