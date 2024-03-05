using System.Globalization;
using System.Reflection;

namespace BnbnavNetClient.I18Next.Services;


//consider using singleton class since this is only ever used as the instance
public interface IAvaloniaI18Next
{    
    string this[string key, Dictionary<string, object?>? arguments] { get; }

    string this[string key] => this[key, (Dictionary<string, object?>?) null];

    string this[string key, params (string Name, object? Value)[] arguments] => 
        this[key, TupleArrayToDict(arguments)];

    Task<string> Async(string key, object? arguments);

    Task<string> Async(string key) => Async(key, (object?) null);

    Task<string> Async(string key, params (string Name, object? Value)[] arguments) =>
        Async(key, TupleArrayToDict(arguments));

    bool IsRightToLeft { get; }

    IEnumerable<CultureInfo> AvailableLanguages { get; }
    CultureInfo CurrentLanguage { get; set; }

    void Initialize(string basePath, bool pseudo) => 
        Initialize(new JsonResourcesFileBackend(Assembly.GetCallingAssembly(), basePath), pseudo);
    
    void Initialize(JsonResourcesFileBackend backend, bool pseudo);

    static Dictionary<string, object?> TupleArrayToDict((string Name, object? Value)[] arguments) =>
        arguments.ToDictionary(static t => t.Name, static t => t.Value);
}
