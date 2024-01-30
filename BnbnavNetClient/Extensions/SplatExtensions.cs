using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Settings;
using Splat;

namespace BnbnavNetClient.Extensions;

public static class SplatExtensions
{
    public static IAvaloniaI18Next GetI18Next(this IReadonlyDependencyResolver me) =>
        me.GetService<IAvaloniaI18Next>() ?? throw new InvalidOperationException("Avalonia I18Next must be registered");

    public static ISettingsManager GetSettingsManager(this IReadonlyDependencyResolver me) =>
        me.GetService<ISettingsManager>() ?? throw new InvalidOperationException("Settings manager must be registered");
}