using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FryMath.Core.CAS;
using FryMath.Core.Calculus;
using FryMath.Core.LinearAlgebra;
using FryMath.UI.Theming;
using FryMath.UI.ViewModels;
using FryMath.UI.Views;
using FryWorld.Sdk.Descriptors;
using FryWorld.Sdk.Models;
using FryWorld.Sdk.Plugins;
using FryWorld.Sdk.Services;

namespace FryMath.Plugin;

/// <summary>
/// FryWorld plugin registration for the Math Studio.
/// Contributes a full-viewport studio page, floating shell overlay, Command Palette commands, Status Bar widget, and Ribbon tab.
/// </summary>
public class MathPlugin : IFryPlugin
{
    private static MainWindowViewModel? _sharedViewModel;

    public string Id => "com.fryworld.math";
    public string Name => "Math Studio";
    public Version Version => new(1, 0, 0);

    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public IReadOnlyDictionary<string, PluginSettingDefinition>? SettingsSchema =>
        new Dictionary<string, PluginSettingDefinition>
        {
            ["DefaultLaboratory"] = new()
            {
                Type = PluginSettingType.Select,
                Label = "Default Laboratory",
                Description = "Starting laboratory mode when Math Studio opens",
                DefaultValue = "Visual Calculus",
                Options = new()
                {
                    "Visual Calculus",
                    "Linear Algebra",
                    "Complex & Fractals",
                    "Fourier & Waves",
                    "Probability & Stats",
                    "Dynamical Chaos"
                }
            },
            ["ShowProbeCurvature"] = new()
            {
                Type = PluginSettingType.Boolean,
                Label = "Show Curvature in Probe",
                Description = "Display live osculating circle and curvature in Math Probe",
                DefaultValue = true
            },
            ["AudioVolume"] = new()
            {
                Type = PluginSettingType.Double,
                Label = "Harmonic Audio Volume",
                Description = "Synthesizer playback volume multiplier",
                DefaultValue = 0.4,
                MinValue = 0.0,
                MaxValue = 1.0,
                Step = 0.05
            }
        };

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        void NavigateToStudio()
        {
            if (ctx.TryGetService<IShellService>(out var shell))
            {
                shell.NavigateTo(Id);
            }
        }

        void OpenOverlay()
        {
            if (ctx.TryGetService<IShellService>(out var shell))
            {
                shell.ShowOverlay($"{Id}.overlay");
            }
        }

        // Register theme contributor with host
        if (ctx.TryGetService<FryWorld.Sdk.Theming.IThemeManager>(out var themeManager))
        {
            var contributor = new MathStudioTokenContributor();
            var sub = themeManager.RegisterTokenContributor(contributor);
            ctx.RegisterDisposable(sub);
        }

        if (ctx.TryGetService<IThemeService>(out var hostThemeService))
        {
            MathThemeService.Instance.SetTheme(hostThemeService.CurrentTheme);
            hostThemeService.ThemeChanged += t => MathThemeService.Instance.SetTheme(t);
        }

        MainWindowViewModel GetViewModel()
        {
            if (_sharedViewModel == null)
            {
                _sharedViewModel = MainWindowViewModel.ActiveInstance ?? new MainWindowViewModel(ctx);
                ctx.RegisterDisposable(_sharedViewModel);
            }
            MainWindowViewModel.ActiveInstance = _sharedViewModel;
            return _sharedViewModel;
        }

        MathStudioView CreateStudioView()
        {
            var vm = GetViewModel();
            var view = new MathStudioView { DataContext = vm };

            if (ctx.TryGetService<FryWorld.Sdk.Theming.IThemeManager>(out var tm))
            {
                tm.TrackResourceRoot(view);
            }
            MathThemeService.Instance.TrackResourceRoot(view);

            return view;
        }

        // 1. Navigation Rail Item
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = Id,
            Title = "Math Studio",
            Category = "Education & Science",
            IconKind = "Calculator",
            AccentColor = "#A855F7",
            Order = 45,
            ViewFactory = () => CreateStudioView()
        });

        // 2. Floating Draggable Shell Overlay
        ctx.RegisterOverlay(new OverlayDescriptor
        {
            Id = $"{Id}.overlay",
            Title = "∑ Math Studio",
            IconKind = "Calculator",
            DefaultWidth = 1200,
            DefaultHeight = 780,
            MinWidth = 800,
            MinHeight = 550,
            IsResizable = true,
            AutoOpenOnMount = false,
            ViewFactory = () => CreateStudioView()
        });

        // 3. Command Palette Commands
        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.math.open",
            Title = "Open Math Studio",
            Subtitle = "Interactive mathematics studio, symbolic CAS, and conceptual visual learning",
            Category = "Education & Science",
            IconKind = "Calculator",
            ShortcutKey = OperatingSystem.IsMacOS() ? "Cmd+Alt+M" : "Ctrl+Alt+M",
            Command = new RelayCommand(NavigateToStudio)
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.math.calculus",
            Title = "Open Visual Calculus Lab",
            Subtitle = "Explore tangent convergence, Riemann sums, and live Math Probe",
            Category = "Education & Science",
            IconKind = "ChartBellCurve",
            Command = new RelayCommand(() =>
            {
                GetViewModel().SelectLaboratory("Visual Calculus");
                NavigateToStudio();
            })
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.math.matrices",
            Title = "Open Linear Algebra Lab",
            Subtitle = "Interactive matrix grid transformation and eigenvector detection",
            Category = "Education & Science",
            IconKind = "VectorRectangle",
            Command = new RelayCommand(() =>
            {
                GetViewModel().SelectLaboratory("Linear Algebra");
                NavigateToStudio();
            })
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.math.overlay",
            Title = "Open Math Studio Floating Overlay",
            Subtitle = "Launch draggable picture-in-picture math workspace",
            Category = "Education & Science",
            IconKind = "DockWindow",
            Command = new RelayCommand(OpenOverlay)
        });

        // 4. Status Bar Widget
        ctx.RegisterStatusBarWidget(new StatusBarWidgetDescriptor
        {
            Id = "fryworld.status.math",
            Label = "∑ Math",
            Tooltip = "Open FryMath Studio (Interactive Mathematics & CAS)",
            AlignRight = true,
            Order = 28,
            ClickCommand = new RelayCommand(NavigateToStudio)
        });

        // 5. Dynamic Ribbon Tab
        ctx.RegisterRibbonTab(new RibbonTabDescriptor
        {
            Id = "math",
            Header = "Math",
            Actions = new()
            {
                new RibbonActionDescriptor
                {
                    Id = "fryworld.ribbon.math.calculus",
                    GroupId = "labs",
                    Label = "Calculus",
                    IconKind = "ChartBellCurve",
                    Order = 1,
                    Command = new RelayCommand(() =>
                    {
                        GetViewModel().SelectLaboratory("Visual Calculus");
                        NavigateToStudio();
                    })
                },
                new RibbonActionDescriptor
                {
                    Id = "fryworld.ribbon.math.matrices",
                    GroupId = "labs",
                    Label = "Matrices",
                    IconKind = "VectorRectangle",
                    Order = 2,
                    Command = new RelayCommand(() =>
                    {
                        GetViewModel().SelectLaboratory("Linear Algebra");
                        NavigateToStudio();
                    })
                },
                new RibbonActionDescriptor
                {
                    Id = "fryworld.ribbon.math.waves",
                    GroupId = "labs",
                    Label = "Fourier",
                    IconKind = "Waveform",
                    Order = 3,
                    Command = new RelayCommand(() =>
                    {
                        GetViewModel().SelectLaboratory("Fourier & Waves");
                        NavigateToStudio();
                    })
                },
                new RibbonActionDescriptor
                {
                    Id = "fryworld.ribbon.math.plinko",
                    GroupId = "labs",
                    Label = "Plinko",
                    IconKind = "DotsGrid",
                    Order = 4,
                    Command = new RelayCommand(() =>
                    {
                        GetViewModel().SelectLaboratory("Probability & Stats");
                        NavigateToStudio();
                    })
                },
                new RibbonActionDescriptor
                {
                    Id = "fryworld.ribbon.math.chaos",
                    GroupId = "labs",
                    Label = "Chaos",
                    IconKind = "WeatherWindy",
                    Order = 5,
                    Command = new RelayCommand(() =>
                    {
                        GetViewModel().SelectLaboratory("Dynamical Chaos");
                        NavigateToStudio();
                    })
                }
            }
        });

        return Task.CompletedTask;
    }
}
