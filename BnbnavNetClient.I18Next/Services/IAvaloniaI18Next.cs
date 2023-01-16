using System.Reflection;

namespace BnbnavNetClient.I18Next.Services;
public interface IAvaloniaI18Next
{    
    string this[string key, object? arguments] { get; }

    string this[string key] => this[key, (object?)null];

    string this[string key, params (string Name, object? Value)[] arguments] => 
        this[key, TupleArrayToDict(arguments)];

    Task<string> TAsync(string key, object? arguments);

    Task<string> TAsync(string key) => TAsync(key, (object?) null);

    Task<string> TAsync(string key, params (string Name, object? Value)[] arguments) =>
        TAsync(key, TupleArrayToDict(arguments));

    void Initialize(string basePath) => 
        Initialize(new JsonResourcesFileBackend(Assembly.GetCallingAssembly(), basePath));
    
    void Initialize(JsonResourcesFileBackend backend);

    static Dictionary<string, object?> TupleArrayToDict((string Name, object? Value)[] arguments) =>
        arguments.ToDictionary(static t => t.Name, static t => t.Value);
}
