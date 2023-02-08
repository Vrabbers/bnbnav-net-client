using System.Diagnostics.Contracts;
using Avalonia;
using Avalonia.Controls.Shapes;
using BnbnavNetClient.Helpers;

namespace BnbnavNetClient.Models;

public readonly struct ExtendedLine
{
    public ExtendedLine(Point point1, Point point2)
    {
        Point1 = point1;
        Point2 = point2;
    }

    public enum IntersectionType
    {
        Parallel,
        Intersects,
        IntersectsInterpolated
    }
    
    public Point Point1 { get; init; }
    public Point Point2 { get; init; }

    public double Dx => Point2.X - Point1.X;

    public double Dy => Point2.Y - Point1.Y;

    public double Angle
    {
        get => MathHelper.ToDeg(double.Atan2(-Dy, Dx));
    }

    public double Length => double.Sqrt(Dx * Dx + Dy * Dy);

    public static implicit operator Line(ExtendedLine extendedLine) => new()
    {
        StartPoint = extendedLine.Point1,
        EndPoint = extendedLine.Point2
    };

    public static implicit operator ExtendedLine(Line line) => new(line.StartPoint, line.EndPoint);

    [Pure]
    public ExtendedLine SetAngle(double angle)
    {
        var angleR = MathHelper.ToRad(angle);
        var dx = double.Cos(angleR) * Length;
        var dy = -double.Sin(angleR) * Length;
        
        return this with { 
            Point2 = new Point(Point1.X + dx, Point1.Y + dy)
        };
    }

    [Pure]
    public ExtendedLine NudgeAngle(double angle) => SetAngle(angle + Angle);

    [Pure]
    public Point Lerp(double t) => 
        new(Point1.X + (Point2.X - Point1.X) * t, Point1.Y + (Point2.Y - Point1.Y) * t);

    [Pure]
    public ExtendedLine UnitLine() => this with
    {
        Point2 = new Point(Point1.X + Dx / Length, Point1.Y + Dy / Length)
    };

    [Pure]
    public ExtendedLine NormalLine() => this with
    {
        Point2 = Point1 + new Point(Dy, -Dx)
    };

    [Pure]
    public IntersectionType TryIntersect(ExtendedLine other, out Point intersectionPoint)
    {
        intersectionPoint = new Point();
        
        var a = Point2 - Point1;
        var b = other.Point1 - other.Point2;
        var c = Point1 - other.Point1;

        var denominator = a.Y * b.X - a.X * b.Y;
        if (denominator == 0 || !double.IsFinite(denominator)) return IntersectionType.Parallel;

        var reciprocal = double.ReciprocalEstimate(denominator);
        var na = (b.Y * c.X - b.X * c.Y) * reciprocal;
        intersectionPoint = Point1 + a * na;

        if (na is < 0 or > 1) return IntersectionType.IntersectsInterpolated;
        var nb = (a.X * c.Y - a.Y * c.X) * reciprocal;
        return nb is < 0 or > 1 ? IntersectionType.IntersectsInterpolated : IntersectionType.Intersects;
    }

    [Pure]
    public ExtendedLine SetLength(double length)
    {
        var unit = UnitLine();
        length /= unit.Length;
        return this with
        {
            Point2 = new Point(Point1.X + length * unit.Dx, Point1.Y + length * unit.Dy)
        };
    }

    [Pure]
    public bool RightAngleIntersection(Point point, out ExtendedLine intersection)
    {
        var intersectionLine = new ExtendedLine(point, point + new Point(5, 0)).SetAngle(NormalLine().Angle);
        _ = intersectionLine.TryIntersect(this, out var intersectionPoint);

        var testLine = new ExtendedLine(point, intersectionPoint);
        testLine = testLine.SetLength(testLine.Length + 20);
        var intersectionResult = testLine.TryIntersect(this, out intersectionPoint);

        intersection = new ExtendedLine(intersectionPoint, point);
        return intersectionResult == IntersectionType.Intersects;
    }

    public ExtendedLine FlipDirection() => new(Point2, Point1);

    [Pure]
    public double AngleTo(ExtendedLine other)
    {
        return double.Ieee754Remainder(other.Angle - Angle, 360);
    }

    [Pure]
    public ExtendedLine MovePoint1(Point point)
    {
        return new ExtendedLine
        {
            Point1 = point, 
            Point2 = point + new Point(Dx, Dy)
        };
    }

    [Pure]
    public ExtendedLine MoveCenter(Point point)
    {
        return new ExtendedLine
        {
            Point1 = point - new Point(Dx, Dy) / 2,
            Point2 = point + new Point(Dx, Dy) / 2
        };
    }
}