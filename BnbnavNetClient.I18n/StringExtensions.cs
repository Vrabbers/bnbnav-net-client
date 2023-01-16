namespace BnbnavNetClient.i18n;

public static class StringExtensions
{
    public static string T(this string key, object? args = null)
    {
        return GlobalI18nextInstance.Instance.T(key, args);
    }

    public static async Task<string> Ta(this string key, object? args = null)
    {
        return await GlobalI18nextInstance.Instance.Ta(key, args);
    }
}