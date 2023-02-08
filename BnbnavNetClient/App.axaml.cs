using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Settings;
using BnbnavNetClient.ViewModels;
using BnbnavNetClient.Views;
using System;
using System.Globalization;
using BnbnavNetClient.Services.TextToSpeech;

namespace BnbnavNetClient;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        var settings = AvaloniaLocator.Current.GetRequiredService<ISettingsManager>();
        settings.LoadAsync().Wait();

        var pseudo = Environment.GetEnvironmentVariable("PSEUDOLOCALIZATION") == "true";
        var i18N = AvaloniaLocator.Current.GetRequiredService<IAvaloniaI18Next>();
        i18N.Initialize("BnbnavNetClient.locales", pseudo);
        i18N.CurrentLanguage = new CultureInfo(settings.Settings.Language);

        var tts = AvaloniaLocator.Current.GetRequiredService<ITextToSpeechProvider>();
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