using System;
using Avalonia;
using Avalonia.Controls.Shapes;

namespace BnbnavNetClient.Models;

public readonly struct ExtendedLine
{
    public Point Point1 { get; init; }
    public Point Point2 { get; init; }

    public double Dx => Point2.X - Point1.X;

    public double Dy => Point2.Y - Point1.Y;

    public double Angle
    {
        get
        {
            var theta = Math.Atan(-Dy / Dx) * 360.0 / Math.Tau;
            if (Dx >= 0) theta += 180;
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
}