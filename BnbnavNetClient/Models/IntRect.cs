namespace BnbnavNetClient.Models;

public readonly record struct IntRect(int Left, int Top, int Right, int Bottom)
{
    public bool Contains(int x, int y) => 
        Left <= x && x <= Right && Top <= y && y <= Bottom;

    public IntRect Expand(int amt) => 
        new(Left - amt, Top - amt, Right + amt, Bottom + amt);

    public IntRect Intersect(IntRect x) =>
        new(int.Max(Left, x.Left), int.Max(Top, x.Top), int.Min(Right, x.Right), int.Min(Bottom, x.Bottom));
}