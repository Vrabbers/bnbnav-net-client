using System.Reflection;
using System.Text.Json;
using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

namespace BnbnavNetClient.i18n;

public class JsonResourcesFileBackend : ITranslationBackend
{
    private readonly string _basePath;
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory;
    private Assembly _assembly;

    public JsonResourcesFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
        _assembly = Assembly.GetCallingAssembly();;
    }

    public JsonResourcesFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory)
    {
        _basePath = basePath;
        _treeBuilderFactory = treeBuilderFactory;
        _assembly = Assembly.GetCallingAssembly();;
    }

    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var builder = _treeBuilderFactory.Create();
        builder.Namespace = @namespace;

        List<string> possibleFiles = new();
        possibleFiles.Add($"{language.ToLower()}.{@namespace}.json");
        if (language.Contains("-"))
        {
            possibleFiles.Add($"{language.Split("-")[0].ToLower()}.{@namespace}.json");
        }

        var fileStream = possibleFiles
            .Select(file => _assembly.GetManifestResourceStream($"{_basePath}.{file}")).FirstOrDefault(translationFile => translationFile is not null);

        if (fileStream is null) return builder.Build();
        
        var contents = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(fileStream);
        foreach (var (key, value) in contents!)
        {
            builder.AddTranslation(key, value);
        }
        return builder.Build();
    }
}