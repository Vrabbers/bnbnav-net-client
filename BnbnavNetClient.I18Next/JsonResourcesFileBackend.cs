using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

namespace BnbnavNetClient.I18Next;

public sealed class JsonResourcesFileBackend : ITranslationBackend
{
    readonly string _basePath;
    readonly ITranslationTreeBuilderFactory _treeBuilderFactory;
    readonly Assembly _assembly;

    public JsonResourcesFileBackend(Assembly assembly, string basePath)
        : this(assembly, basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    public JsonResourcesFileBackend(Assembly assembly, string basePath, ITranslationTreeBuilderFactory treeBuilderFactory)
    {
        _basePath = basePath;
        _treeBuilderFactory = treeBuilderFactory;
        _assembly = assembly;
    }

    internal CultureInfo[] AvailableLanguages =>
        _assembly.GetManifestResourceNames()
        .Where(s => s.StartsWith(_basePath))
        .Select(s => s[(_basePath.Length + 1)..].Split('.')[0])
        .Select(s =>
        {
            try
            {
                return new CultureInfo(s); //check if the folder name is a culture
            }
            catch (ArgumentException)
            {
                return null;
            }
        })
        .Where(info => info is not null)
        .ToArray()!;

    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var builder = _treeBuilderFactory.Create();
        builder.Namespace = @namespace;

        List<string> possibleFiles = new()
        {
            $"{language.ToLower()}.{@namespace}.json"
        };

        if (language.Contains('-'))
        {
            possibleFiles.Add($"{language.Split("-")[0].ToLower()}.{@namespace}.json");
        }

        var fileStream = possibleFiles
            .Select(file => _assembly.GetManifestResourceStream($"{_basePath}.{file}")).FirstOrDefault(translationFile => translationFile is not null);

        if (fileStream is null) return builder.Build();
        
        var contents = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(fileStream);
        foreach (var (key, value) in contents!)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            builder.AddTranslation(key, value);
        }
        return builder.Build();
    }
}