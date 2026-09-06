using System;
using Avalonia.Controls;
using FluentSend.ViewModels;

namespace FluentSend.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        try
        {
            ExtendClientAreaToDecorationsHint = true;
        }
        catch
        {
        }
        ApplyBackdrop(0);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainViewModel vm)
        {
            vm.Settings.PropertyChanged += OnSettingsChanged;
            ApplyTheme(vm.Settings.ThemeIndex);
            ApplyBackdrop(vm.Settings.BackdropIndex);
        }
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;
        switch (e.PropertyName)
        {
            case nameof(SettingsPageViewModel.ThemeIndex):
                ApplyTheme(vm.Settings.ThemeIndex);
                break;
            case nameof(SettingsPageViewModel.BackdropIndex):
                ApplyBackdrop(vm.Settings.BackdropIndex);
                break;
        }
    }

    private void ApplyTheme(int themeIndex)
    {
        switch (themeIndex)
        {
            case 1:
                RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
                break;
            case 2:
                RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                break;
            default:
                RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
                break;
        }
    }

    private void ApplyBackdrop(int backdropIndex)
    {
        WindowTransparencyLevel level = backdropIndex switch
        {
            2 => WindowTransparencyLevel.AcrylicBlur,
            3 => WindowTransparencyLevel.None,
            _ => WindowTransparencyLevel.Mica,
        };
        TransparencyLevelHint = new[] { level };
    }
}
