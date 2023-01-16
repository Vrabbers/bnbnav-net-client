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

        var backend = new JsonFileBackend("locales");
        var i18n = new I18NextNet(backend, new DefaultTranslator(backend, new TraceLogger(), new CldrPluralResolver(), new DefaultInterpolator()));
        var test = i18n.T("TEST");
        var testWithArg = i18n.T("TEST_WITH_ARG", new
        {
            age = "22"
        });
        var testPluralSingular = i18n.T("TEST-PLURAL", new
        {
            count = 1
        });
        var testPlural = i18n.T("TEST-PLURAL", new
        {
            count = 2
        });
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