using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FryMath.Plugin.Runner;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
