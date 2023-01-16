using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BnbnavNetClient.i18n;
using BnbnavNetClient.ViewModels;
using BnbnavNetClient.Views;
using I18Next.Net;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;

namespace BnbnavNetClient;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var backend = new JsonResourcesFileBackend("BnbnavNetClient.locales");
        var i18n = new I18NextNet(backend, new DefaultTranslator(backend, new TraceLogger(), new CldrPluralResolver(), new DefaultInterpolator()), new CultureInfoLanguageDetector());
        i18n.UseDetectedLanguage();
        i18n.SetFallbackLanguages("en");
        GlobalI18nextInstance.Instance = i18n;
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