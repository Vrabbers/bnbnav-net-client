using Avalonia;
using BnbnavNetClient.I18Next.Services;
using Splat;

namespace BnbnavNetClient.I18Next;
public sealed class TrString : AvaloniaObject
{
    readonly IAvaloniaI18Next _tr;

    string _key = string.Empty;
    public string Key 
    {
        get => _key;
        set => SetAndRaise(KeyProperty, ref _key, value);
    }
    public static readonly DirectProperty<TrString, string> KeyProperty =
        AvaloniaProperty.RegisterDirect<TrString, string>(nameof(Key), me => me.Key, (me, val) => me.Key = val);

    int? _count;
    public int? Count
    {
        get => _count;
        set => SetAndRaise(CountProperty, ref _count, value);
    }
    public static readonly DirectProperty<TrString, int?> CountProperty =
        AvaloniaProperty.RegisterDirect<TrString, int?>(nameof(Count), me => me.Count, (me, val) => me.Count = val);

    IEnumerable<TrArgument> _arguments = new List<TrArgument>();
    public IEnumerable<TrArgument> Arguments
    {
        get => _arguments;
        set => SetAndRaise(ArgumentsProperty, ref _arguments, value);
    }
    public static readonly DirectProperty<TrString, IEnumerable<TrArgument>> ArgumentsProperty =
        AvaloniaProperty.RegisterDirect<TrString, IEnumerable<TrArgument>>(nameof(Arguments), 
            me => me.Arguments, (me, val) => me.Arguments = val);

    public TrString()
    {
        _tr = Locator.Current.GetService<IAvaloniaI18Next>() ?? throw new InvalidOperationException("AvaloniaI18Next must be registered for TrString to be constructed");
    }

    public override string ToString()
    {
        if (Count is null && !Arguments.Any())
            return _tr[Key];

        Dictionary<string, object?> args = new();
        if (Count is not null)
            args.Add("count", Count);

        foreach (var arg in Arguments)
            args.Add(arg.Name, arg.Value);

        return _tr[Key, args];
    }

    public static implicit operator string(TrString str) => str.ToString();
}
public sealed class TrArgument : AvaloniaObject
{
    string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetAndRaise(NameProperty, ref _name, value);
    }
    public static readonly DirectProperty<TrArgument, string> NameProperty =
        AvaloniaProperty.RegisterDirect<TrArgument, string>(nameof(Name), me => me.Name, (me, val) => me.Name = val);

    object? _value;
    public object? Value 
    {
        get => _value;
        set => SetAndRaise(ValueProperty, ref _value, value);
    }
    public static readonly DirectProperty<TrArgument, object?> ValueProperty =
        AvaloniaProperty.RegisterDirect<TrArgument, object?>(nameof(Value), me => me.Value, (me, val) => me.Value = val);
}
