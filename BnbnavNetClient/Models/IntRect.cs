namespace BnbnavNetClient.Models;

public readonly record struct IntRect(int Left, int Top, int Right, int Bottom)
{
    public bool Contains(int x, int y) => 
        Left <= x && x <= Right && Top <= y && y <= Bottom;

    public IntRect Expand(int amt) => 
        new(Left - amt, Top - amt, Right + amt, Bottom + amt);

    public IntRect ExpandToFit(int x, int y) =>
        new(int.Min(x, Left), int.Min(y, Top), int.Max(x, Right), int.Max(y, Bottom));
    
    public (int Left, int Top) IntersectTopLeft(IntRect rect) => 
        (int.Max(Left, rect.Left), int.Max(Top, rect.Top));

    public IntRect Intersect(IntRect rect) =>
        new(int.Max(Left, rect.Left), int.Max(Top, rect.Top), int.Min(Right, rect.Right), int.Min(Bottom, rect.Bottom));
}