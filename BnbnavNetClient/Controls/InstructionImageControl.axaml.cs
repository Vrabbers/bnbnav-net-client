using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
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
            var bounds = Bounds;
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