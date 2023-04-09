using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace BnbnavNetClient;

public enum MapTheme 
{
    Day,
    Night
}

public sealed class MapThemeResources : AvaloniaObject, IResourceProvider
{
    readonly IResourceProvider _day;
    readonly IResourceProvider _night;

    IResourceProvider CurrentProvider => Theme == MapTheme.Day ? _day : _night;

    public static readonly StyledProperty<MapTheme> ThemeProperty = 
        AvaloniaProperty.Register<MapThemeResources, MapTheme>(nameof(Theme));

    public MapTheme Theme 
    {
        get => GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    public IResourceHost? Owner => CurrentProvider.Owner;

    public bool HasResources => CurrentProvider.HasResources;

    public MapThemeResources()
    {
        _day = new ResourceInclude(baseUri: null) { Source = new Uri("avares://BnbnavNetClient/Resources/DayTheme.axaml") };
        _night = new ResourceInclude(baseUri: null) { Source = new Uri("avares://BnbnavNetClient/Resources/NightTheme.axaml") };
    }

    public event EventHandler? OwnerChanged
    {
        add
        {
            _day.OwnerChanged += value;
            _night.OwnerChanged += value;
        }

        remove
        {
            _day.OwnerChanged -= value;
            _night.OwnerChanged -= value;
        }
    }

    public void AddOwner(IResourceHost owner)
    {
        _day.AddOwner(owner);
        _night.AddOwner(owner);
    }

    public void RemoveOwner(IResourceHost owner)
    {
        _day.RemoveOwner(owner);
        _night.RemoveOwner(owner);
    }

    public bool TryGetResource(object key, ThemeVariant? theme, out object? value) => CurrentProvider.TryGetResource(key, theme, out value);
}
