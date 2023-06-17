using Avalonia;
using Avalonia.ReactiveUI;
using BnbnavNetClient.Services;

namespace BnbnavNetClient;

public static class AppBuilderExtensions
{
    public static AppBuilder UseBnbnavAvalonia(this AppBuilder appBuilder)
    {
        return appBuilder
            .UseReactiveUI()
            .With(new MapServiceProxy());
    }
}