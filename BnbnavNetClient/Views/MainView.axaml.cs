using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using BnbnavNetClient.ViewModels;

namespace BnbnavNetClient.Views;

public partial class MainView : UserControl
{
    readonly Style _whiteTextStyle;

    public MainView()
    {
        _whiteTextStyle = new Style(static x => x.OfType<TextBlock>());
        _whiteTextStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.White)));
        InitializeComponent();
    }

    public async void ViewLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        await vm.InitMapService();

        MapPanel.Children.Add(new MapView() { DataContext = vm.MapViewModel });
    }


    bool _isNightMode = false;

    public void ColorModeSwitch(object sender, RoutedEventArgs e)
    {
        _isNightMode = !_isNightMode;
        ((FluentTheme)App.Current!.Styles[0]).Mode = _isNightMode ? FluentThemeMode.Dark : FluentThemeMode.Light;
    
        if (_isNightMode)
        {
            //WASM seems to need a little help setting the textblock styles. hopefully they fix this sometime!
            App.Current!.Styles.Add(_whiteTextStyle);
        }
        else
        {
            App.Current!.Styles.Remove(_whiteTextStyle);
        }

        ((MapThemeResources)App.Current!.Resources.MergedDictionaries[0]).Theme = _isNightMode ? MapTheme.Night : MapTheme.Day;
    }
}