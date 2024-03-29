using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BnbnavNetClient.ViewModels;
using BnbnavNetClient.Views;
using System.Globalization;
using BnbnavNetClient.Extensions;
using BnbnavNetClient.Services.TextToSpeech;
using Splat;

namespace BnbnavNetClient;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        var settings = Locator.Current.GetSettingsManager();
        settings.LoadAsync().Wait();

        var pseudo = Environment.GetEnvironmentVariable("PSEUDOLOCALIZATION") == "true";
        var i18N = Locator.Current.GetI18Next();
        i18N.Initialize("BnbnavNetClient.locales", pseudo);
        i18N.CurrentLanguage = new CultureInfo(settings.Settings.Language);

        var tts = Locator.Current.GetService<ITextToSpeechProvider>();
        if (tts is not null)
            tts.CurrentCulture = CultureInfo.CurrentUICulture;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}