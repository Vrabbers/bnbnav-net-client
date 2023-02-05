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
    CalculatedRoute.Instruction? _Instruction;
    public static readonly DirectProperty<InstructionImageControl, CalculatedRoute.Instruction?> InstructionProperty = AvaloniaProperty.RegisterDirect<InstructionImageControl, CalculatedRoute.Instruction?>("Instruction", o => o.Instruction, (o, v) => o.Instruction = v);

    public CalculatedRoute.Instruction? Instruction
    {
        get { return _Instruction; }
        set { SetAndRaise(InstructionProperty, ref _Instruction, value); }
    }

    public InstructionImageControl()
    {
        this.WhenAnyValue(x => x.Instruction)
            .Subscribe(Observer.Create<CalculatedRoute.Instruction?>(_ => InvalidateVisual()));
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_Instruction is not null)
        {
            var bounds = new Rect(new Point(0, 0), Bounds.Size);

            if (_Instruction.roundaboutExit is not null && _Instruction.from is not null && _Instruction.to is not null)
            {
                var innerCircleBounds = bounds.Deflate(3 * bounds.Width / 10);
                var angle = _Instruction.from.Line.FlipDirection().AngleTo(_Instruction.roundaboutExit.Line) - 90;
                var complex = Complex.FromPolarCoordinates(innerCircleBounds.Width / 2, MathHelper.ToRad(angle + 180));
                var arcEnd = new Point(-complex.Real, complex.Imaginary) + innerCircleBounds.Center;
                var arrowEnd = new ExtendedLine(arcEnd, new Point(arcEnd.X + bounds.Width / 5, arcEnd.Y)).SetAngle(angle).Point2;

                var roundaboutAngle = _Instruction.from.Line.FlipDirection().AngleTo(_Instruction.to.Line);
                var sweepDirection = roundaboutAngle < 0
                    ? SweepDirection.Clockwise
                    : SweepDirection.CounterClockwise;
                
                context.DrawEllipse(null, new Pen(new SolidColorBrush(new Color(100, 255, 255, 255)), bounds.Width / 10), innerCircleBounds.Center, innerCircleBounds.Width / 2, innerCircleBounds.Height / 2);

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

                context.DrawGeometry(null, new Pen(new SolidColorBrush(new Color(255, 255, 255, 255)), bounds.Width / 10, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), path);
            }
            else
            {
                var instructionFile = _Instruction.instructionType switch
                {
                    CalculatedRoute.Instruction.InstructionType.Departure => "depart",
                    CalculatedRoute.Instruction.InstructionType.Arrival => "arrive",
                    CalculatedRoute.Instruction.InstructionType.ContinueStraight => "continue-straight",
                    CalculatedRoute.Instruction.InstructionType.BearLeft => "bear-left",
                    CalculatedRoute.Instruction.InstructionType.TurnLeft => "turn-left",
                    CalculatedRoute.Instruction.InstructionType.SharpLeft => "sharp-left",
                    CalculatedRoute.Instruction.InstructionType.TurnAround => "turn-around",
                    CalculatedRoute.Instruction.InstructionType.SharpRight => "sharp-right",
                    CalculatedRoute.Instruction.InstructionType.TurnRight => "turn-right",
                    CalculatedRoute.Instruction.InstructionType.BearRight => "bear-right",
                    CalculatedRoute.Instruction.InstructionType.ExitLeft => "exit-left",
                    CalculatedRoute.Instruction.InstructionType.ExitRight => "exit-right",
                    CalculatedRoute.Instruction.InstructionType.Merge => "merge",
                    CalculatedRoute.Instruction.InstructionType.EnterRoundabout => "enter-roundabout",
                    CalculatedRoute.Instruction.InstructionType.LeaveRoundabout => "leave-roundabout",
                    _ => throw new ArgumentOutOfRangeException()
                };

                context.DrawSvgUrl($"avares://BnbnavNetClient/Assets/Instructions/{instructionFile}.svg", bounds);
            }
        }
    }
}