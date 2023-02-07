using System;
using System.Numerics;
using System.Reactive;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using BnbnavNetClient.Helpers;
using BnbnavNetClient.Models;
using ReactiveUI;

namespace BnbnavNetClient.Controls;

public class InstructionImageControl : TemplatedControl
{
    CalculatedRoute.Instruction? _instruction;
    public static readonly DirectProperty<InstructionImageControl, CalculatedRoute.Instruction?> InstructionProperty = AvaloniaProperty.RegisterDirect<InstructionImageControl, CalculatedRoute.Instruction?>("Instruction", o => o.Instruction, (o, v) => o.Instruction = v);

    public CalculatedRoute.Instruction? Instruction
    {
        get { return _instruction; }
        set { SetAndRaise(InstructionProperty, ref _instruction, value); }
    }

    public InstructionImageControl()
    {
        this.WhenAnyValue(x => x.Instruction)
            .Subscribe(Observer.Create<CalculatedRoute.Instruction?>(_ => InvalidateVisual()));
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_instruction is not null)
        {
            var bounds = new Rect(new Point(0, 0), Bounds.Size);

            if (_instruction.RoundaboutExit is not null && _instruction.From is not null && _instruction.To is not null)
            {
                var innerCircleBounds = bounds.Deflate(3 * bounds.Width / 10);
                var angle = _instruction.From.Line.FlipDirection().AngleTo(_instruction.RoundaboutExit.Line) - 90;
                var complex = Complex.FromPolarCoordinates(innerCircleBounds.Width / 2, MathHelper.ToRad(angle + 180));
                var arcEnd = new Point(-complex.Real, complex.Imaginary) + innerCircleBounds.Center;
                var arrowEnd = new ExtendedLine(arcEnd, new Point(arcEnd.X + bounds.Width / 5, arcEnd.Y)).SetAngle(angle).Point2;

                var roundaboutAngle = _instruction.From.Line.FlipDirection().AngleTo(_instruction.To.Line);
                var sweepDirection = roundaboutAngle < 0
                    ? SweepDirection.Clockwise
                    : SweepDirection.CounterClockwise;

                var color = new Color(100, 255, 255, 255);
                if (Foreground is SolidColorBrush solidColorBrush)
                    color = new Color(100, solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B);
                context.DrawEllipse(null, new Pen(new SolidColorBrush(color), bounds.Width / 10), innerCircleBounds.Center, innerCircleBounds.Width / 2, innerCircleBounds.Height / 2);

                var path = new PathGeometry
                {
                    Figures = new PathFigures
                    {
                        new()
                        {
                            StartPoint = new Point(bounds.Center.X, bounds.Bottom),
                            Segments = new PathSegments
                            {
                                new LineSegment
                                {
                                    Point = new Point(innerCircleBounds.Center.X, innerCircleBounds.Bottom)
                                },
                                new ArcSegment
                                {
                                    IsLargeArc = sweepDirection == SweepDirection.Clockwise ? double.Abs(angle) <= 90 : double.Abs(angle) >= 90,
                                    Point = arcEnd,
                                    Size = innerCircleBounds.Size / 2,
                                    SweepDirection = sweepDirection
                                },
                                new LineSegment
                                {
                                    Point = arrowEnd
                                },
                                new LineSegment
                                {
                                    Point = new ExtendedLine(arrowEnd, new Point(arrowEnd.X + bounds.Width / 10, arrowEnd.Y)).SetAngle(angle - 135).Point2
                                },
                                new LineSegment
                                {
                                    Point = arrowEnd
                                },
                                new LineSegment
                                {
                                    Point = new ExtendedLine(arrowEnd, new Point(arrowEnd.X + bounds.Width / 10, arrowEnd.Y)).SetAngle(angle + 135).Point2
                                }
                            },
                            IsClosed = false
                        }
                    }
                };

                context.DrawGeometry(null, new Pen(Foreground, bounds.Width / 10, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), path);
            }
            else
            {
                var instructionFile = _instruction.InstructionType switch
                {
                    CalculatedRoute.Instruction.InstructionTypes.Departure => "depart",
                    CalculatedRoute.Instruction.InstructionTypes.Arrival => "arrive",
                    CalculatedRoute.Instruction.InstructionTypes.ContinueStraight => "continue-straight",
                    CalculatedRoute.Instruction.InstructionTypes.BearLeft => "bear-left",
                    CalculatedRoute.Instruction.InstructionTypes.TurnLeft => "turn-left",
                    CalculatedRoute.Instruction.InstructionTypes.SharpLeft => "sharp-left",
                    CalculatedRoute.Instruction.InstructionTypes.TurnAround => "turn-around",
                    CalculatedRoute.Instruction.InstructionTypes.SharpRight => "sharp-right",
                    CalculatedRoute.Instruction.InstructionTypes.TurnRight => "turn-right",
                    CalculatedRoute.Instruction.InstructionTypes.BearRight => "bear-right",
                    CalculatedRoute.Instruction.InstructionTypes.ExitLeft => "exit-left",
                    CalculatedRoute.Instruction.InstructionTypes.ExitRight => "exit-right",
                    CalculatedRoute.Instruction.InstructionTypes.Merge => "merge",
                    CalculatedRoute.Instruction.InstructionTypes.EnterRoundabout => "enter-roundabout",
                    CalculatedRoute.Instruction.InstructionTypes.LeaveRoundabout => "leave-roundabout",
                    _ => throw new ArgumentOutOfRangeException()
                };

                context.DrawSvgUrl($"avares://BnbnavNetClient/Assets/Instructions/{instructionFile}.svg", bounds);
            }
        }
    }
}