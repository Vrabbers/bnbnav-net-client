using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace BnbnavNetClient.Controls;
public class DayNightButton : TemplatedControl
{
    public static readonly StyledProperty<bool> IsNightModeProperty =
        AvaloniaProperty.Register<DayNightButton, bool>(nameof(IsNightMode), defaultValue: false);

    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
             RoutedEvent.Register<DayNightButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    public bool IsNightMode
    {
        get => GetValue(IsNightModeProperty);
        set => SetValue(IsNightModeProperty, value);
    }

    Button _button = null!; 
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _button = e.NameScope.Find<Button>("Button")!;
        _button.Click += ButtonClick;
    }

    void ButtonClick(object? sender, RoutedEventArgs e)
    {
        IsNightMode = !IsNightMode;
        RaiseEvent(new RoutedEventArgs(ClickEvent));
    }
}
