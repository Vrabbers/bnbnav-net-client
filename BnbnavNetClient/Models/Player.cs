namespace BnbnavNetClient.Models;

public sealed class Player
{
    public string Name { get; }

    public double X { get; private set; }
    public double Y { get; private set; }
    public double Z { get; private set; }

    public Edge? SnappedEdge { get; private set; }

    public double MarkerAngle { get; private set; }

    public Player(string name)
    {
        Name = name;
    }
}
