using SkiaSharp;

using System.Reflection;

namespace BnbnavNetClient.Resources;

internal class SKColor(string input)
{
    private static Dictionary<string, SkiaSharp.SKColor> Colors =
        typeof(SKColors)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(p => p.FieldType == typeof(SkiaSharp.SKColor))
        .ToDictionary(p => p.Name, p => (SkiaSharp.SKColor)p.GetValue(null)!);

    public SkiaSharp.SKColor ProvideValue(IServiceProvider serviceProvider)
    {
        if (Colors.TryGetValue(input, out var namedColor))
        {
            return namedColor;
        }

        return SkiaSharp.SKColor.Parse(input);
    }   
}
