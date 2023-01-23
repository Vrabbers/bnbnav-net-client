using System;
using Avalonia;
using Avalonia.Controls.Shapes;

namespace BnbnavNetClient.Models;

public readonly struct ExtendedLine
{
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
        get
        {
            var theta = Math.Atan(-Dy / Dx) * 360.0 / Math.Tau;
            if (Dx <= 0) theta += 180;
            var thetaNormalized = theta < 0 ? theta + 360 : theta;
            if (Math.Abs(thetaNormalized - 360) < 0.01)
                return 0;
            return thetaNormalized;
        }
    }

    public double Length => Math.Sqrt(Dx * Dx + Dy * Dy);

    public static implicit operator Line(ExtendedLine extendedLine) => new()
    {
        StartPoint = extendedLine.Point1,
        EndPoint = extendedLine.Point2
    };

    public static implicit operator ExtendedLine(Line line) => new()
    {
        Point1 = line.StartPoint,
        Point2 = line.EndPoint
    };

    public ExtendedLine SetAngle(double angle)
    {
        var angleR = angle * Math.Tau / 360.0;
        var dx = Math.Cos(angleR) * Length;
        var dy = -Math.Sin(angleR) * Length;
        
        return this with { 
            Point2 = new(Point1.X + dx, Point1.Y + dy)
        };
    }

    public Point Lerp(double t) => 
        new(Point1.X + (Point2.X - Point1.X) * t, Point1.Y + (Point2.Y - Point1.Y) * t);

    public ExtendedLine UnitLine() => this with
    {
        Point2 = new(Point1.X + Dx / Length, Point1.Y + Dy / Length)
    };

    public ExtendedLine NormalLine() => this with
    {
        Point2 = Point1 + new Point(Dy, -Dx)
    };

    public IntersectionType TryIntersect(ExtendedLine other, out Point intersectionPoint)
    {
        intersectionPoint = new();
        
        var a = Point1 - Point2;
        var b = other.Point1 - other.Point2;
        var c = Point1 - other.Point1;

        var denominator = a.Y * b.X - a.X * b.Y;
        if (denominator == 0 || !Double.IsFinite(denominator)) return IntersectionType.Parallel;

        var reciprocal = 1 / denominator;
        var na = (b.Y * c.X - b.X * c.Y) * reciprocal;
        intersectionPoint = Point1 + a * na;

        if (na is < 0 or > 1) return IntersectionType.IntersectsInterpolated;
        var nb = (a.X * c.Y - a.Y * c.X) * reciprocal;
        return nb is < 0 or > 1 ? IntersectionType.IntersectsInterpolated : IntersectionType.Intersects;
    }

    public ExtendedLine SetLength(double length)
    {
        var unit = UnitLine();
        length /= unit.Length;
        return this with
        {
            Point2 = new(Point1.X + length * unit.Dx, Point1.Y + length * unit.Dy)
        };
    }

    public ExtendedLine FlipDirection() => new()
    {
        Point1 = Point2,
        Point2 = Point1
    };

    public double AngleTo(ExtendedLine other)
    {
        var delta = other.Angle - Angle;
        var normalised = delta < 0 ? delta + 360 : delta;
        if (Math.Abs(delta - 360) < 0.01) return 0;
        return normalised;
    }
}