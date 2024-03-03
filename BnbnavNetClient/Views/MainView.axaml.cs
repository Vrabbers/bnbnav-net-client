using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using BnbnavNetClient.Extensions;
using BnbnavNetClient.Settings;
using BnbnavNetClient.ViewModels;
using ReactiveUI;
using Splat;

namespace BnbnavNetClient.Views;

public partial class MainView : UserControl
{
    readonly ISettingsManager _settings;

    public MainView()
    {
        FlowDirection = Locator.Current.GetI18Next().IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        _settings = Locator.Current.GetSettingsManager();
        InitializeComponent();
    }

    public async void ViewLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        if (_settings.Settings.NightMode)
        {
            DayNightButton.IsNightMode = true;
            ColorModeSwitch(null, null);
        }

        vm.UserControlButton = UserControlButton;
        
        await vm.InitMapService();

        MapPanel.Children.Add(new MapView { DataContext = vm.MapViewModel });
        WorldSelectComboBox.IsVisible = true;
        vm.RaisePropertyChanged(nameof(MainViewModel.PanText));
    }

    public async void ColorModeSwitch(object? _, RoutedEventArgs? __)
    {
        var button = DayNightButton;

        Application.Current!.RequestedThemeVariant = button.IsNightMode ? ThemeVariant.Dark : ThemeVariant.Light;
        
        _settings.Settings.NightMode = button.IsNightMode;
        await _settings.SaveAsync();
    }

    void EditModeButtonClick(object? _, RoutedEventArgs __)
    {
        // This is so that the button's checked state does not visually change.
        // When the dialog is cancelled, the value of the property it is bound to does not actually change (it is
        // already false), so the property changed event does not fire and the two states are desynchronized.
        EditModeButton.IsChecked = !EditModeButton.IsChecked;
    }
}