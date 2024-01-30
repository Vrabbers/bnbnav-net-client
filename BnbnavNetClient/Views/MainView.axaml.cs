using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using BnbnavNetClient.Extensions;
using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Settings;
using BnbnavNetClient.ViewModels;
using Splat;

namespace BnbnavNetClient.Views;

public partial class MainView : UserControl
{
    readonly Style _whiteTextStyle;
    readonly ISettingsManager _settings;

    public MainView()
    {
        FlowDirection = Locator.Current.GetI18Next().IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        _whiteTextStyle = new Style(static x => x.OfType<TextBlock>());
        _whiteTextStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.White)));
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

        MapPanel.Children.Add(new MapView() { DataContext = vm.MapViewModel });
    }

    public async void ColorModeSwitch(object? _, RoutedEventArgs? __)
    {
        var button = DayNightButton;

        Application.Current!.RequestedThemeVariant = button.IsNightMode ? ThemeVariant.Dark : ThemeVariant.Light;
    
        if (button.IsNightMode)
        {
            //WASM seems to need a little help setting the textblock styles. hopefully they fix this sometime!
            Application.Current.Styles.Add(_whiteTextStyle);
        }
        else
        {
            Application.Current.Styles.Remove(_whiteTextStyle);
        }
        
        _settings.Settings.NightMode = button.IsNightMode;
        await _settings.SaveAsync();
    }
}