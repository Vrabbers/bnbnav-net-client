using Avalonia;

namespace BnbnavNetClient.Models;

public interface ILocatable
{
    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    public string World { get; }
    public Point Point { get; }
}