namespace BnbnavNetClient.Models;

public interface ISearchable
{
    public string Name { get; }
    public string HumanReadableType { get; }
    public ILocatable Location { get; }
}