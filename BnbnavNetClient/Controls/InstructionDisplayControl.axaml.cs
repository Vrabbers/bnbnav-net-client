using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls.Primitives;
using BnbnavNetClient.Models;
using ReactiveUI;

namespace BnbnavNetClient.Controls;

public class InstructionDisplayControl : TemplatedControl
{
    CalculatedRoute.Instruction? _Instruction;
    public static readonly DirectProperty<InstructionDisplayControl, CalculatedRoute.Instruction?> InstructionProperty = AvaloniaProperty.RegisterDirect<InstructionDisplayControl, CalculatedRoute.Instruction?>("Instruction", o => o.Instruction, (o, v) => o.Instruction = v);
    
    int? _ToNextInstruction;
    public static readonly DirectProperty<InstructionDisplayControl, int?> ToNextInstructionProperty = AvaloniaProperty.RegisterDirect<InstructionDisplayControl, int?>("ToNextInstruction", o => o.ToNextInstruction, (o, v) => o.ToNextInstruction = v);
    
    string _CalculatedInstructionLength = "";
    public static readonly DirectProperty<InstructionDisplayControl, string> CalculatedInstructionLengthProperty = AvaloniaProperty.RegisterDirect<InstructionDisplayControl, string>("CalculatedInstructionLength", o => o.CalculatedInstructionLength, (o, v) => o.CalculatedInstructionLength = v);
    
    Thickness _InnerMargin;
    public static readonly DirectProperty<InstructionDisplayControl, Thickness> InnerMarginProperty = AvaloniaProperty.RegisterDirect<InstructionDisplayControl, Thickness>("InnerMargin", o => o.InnerMargin, (o, v) => o.InnerMargin = v);

    public CalculatedRoute.Instruction? Instruction
    {
        get { return _Instruction; }
        set { SetAndRaise(InstructionProperty, ref _Instruction, value); }
    }

    public int? ToNextInstruction
    {
        get { return _ToNextInstruction; }
        set { SetAndRaise(ToNextInstructionProperty, ref _ToNextInstruction, value); }
    }

    public string CalculatedInstructionLength
    {
        get { return _CalculatedInstructionLength; }
        set { SetAndRaise(CalculatedInstructionLengthProperty, ref _CalculatedInstructionLength, value); }
    }

    public Thickness InnerMargin
    {
        get { return _InnerMargin; }
        set { SetAndRaise(InnerMarginProperty, ref _InnerMargin, value); }
    }

    public InstructionDisplayControl()
    {
        this.WhenAnyValue(x => x.Instruction, x => x.ToNextInstruction).Subscribe(Observer.Create<ValueTuple<CalculatedRoute.Instruction?, int?>>(
                tuple =>
                {
                    var distance = (int) double.Round(tuple.Item2 ?? tuple.Item1?.distance ?? 0);
                    CalculatedInstructionLength = $"{distance} blk";
                }));
    }

}