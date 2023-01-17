using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Settings;
using BnbnavNetClient.ViewModels;
using BnbnavNetClient.Views;
using System;
using System.Globalization;

namespace BnbnavNetClient;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        SettingsManager.LoadFromJsonAsync().Wait();

        var pseudo = Environment.GetEnvironmentVariable("PSEUDOLOCALIZATION") == "true";
        var i18n = AvaloniaLocator.Current.GetRequiredService<IAvaloniaI18Next>();
        i18n.Initialize("BnbnavNetClient.locales", pseudo);
        i18n.CurrentLanguage = new CultureInfo(SettingsManager.Settings.Language);
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