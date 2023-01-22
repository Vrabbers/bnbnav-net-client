using Avalonia;
namespace BnbnavNetClient.Helpers;
static class GeoHelper
{
    public static bool LineIntersects(Point from, Point to, Rect bounds)
    {
        if (bounds.Contains(from) || bounds.Contains(to))
            return true;

        //If the line is bigger than the smallest edge of the bounds, draw it, as both points may lie outside the view;
        var minDistSqr = double.Pow(double.Min(bounds.Width, bounds.Height), 2);
        var lengthSqr = DistanceSquared(from, to);

        if (lengthSqr > minDistSqr)
            return true;
        // TODO: do this properly
        return false;
    }

    public static double LineSegmentToPointDistance(Point lineA, Point lineB, Point point)
    {
        if (lineA == lineB)
            return Distance(lineA, point);
        var lengthSqr = DistanceSquared(lineA, lineB);
        var param = double.Clamp(Vector.Dot(point - lineA, lineB - lineA) / lengthSqr, 0, 1);
        var proj = lineA + param * (lineB - lineA);
        return Distance(point, proj);
    }

    public static double DistanceSquared(Point a, Point b) => double.Pow(a.X - b.X, 2) + double.Pow(a.Y - b.Y, 2);
    public static double Distance(Point a, Point b) => double.Sqrt(DistanceSquared(a, b));
}
