using System.Reactive;
using Avalonia;
using Avalonia.Controls.Primitives;
using BnbnavNetClient.Models;
using ReactiveUI;

namespace BnbnavNetClient.Controls;

public class InstructionDisplayControl : TemplatedControl
{
    CalculatedRoute.Instruction? _instruction;
    public static readonly DirectProperty<InstructionDisplayControl, CalculatedRoute.Instruction?> InstructionProperty = AvaloniaProperty.RegisterDirect<InstructionDisplayControl, CalculatedRoute.Instruction?>("Instruction", o => o.Instruction, (o, v) => o.Instruction = v);
    
    int? _toNextInstruction;
    public static readonly DirectProperty<InstructionDisplayControl, int?> ToNextInstructionProperty = AvaloniaProperty.RegisterDirect<InstructionDisplayControl, int?>("ToNextInstruction", o => o.ToNextInstruction, (o, v) => o.ToNextInstruction = v);
    
    string _calculatedInstructionLength = "";
    public static readonly DirectProperty<InstructionDisplayControl, string> CalculatedInstructionLengthProperty = AvaloniaProperty.RegisterDirect<InstructionDisplayControl, string>("CalculatedInstructionLength", o => o.CalculatedInstructionLength, (o, v) => o.CalculatedInstructionLength = v);
    
    Thickness _innerMargin;
    public static readonly DirectProperty<InstructionDisplayControl, Thickness> InnerMarginProperty = AvaloniaProperty.RegisterDirect<InstructionDisplayControl, Thickness>("InnerMargin", o => o.InnerMargin, (o, v) => o.InnerMargin = v);

    public CalculatedRoute.Instruction? Instruction
    {
        get { return _instruction; }
        set { SetAndRaise(InstructionProperty, ref _instruction, value); }
    }

    public int? ToNextInstruction
    {
        get { return _toNextInstruction; }
        set { SetAndRaise(ToNextInstructionProperty, ref _toNextInstruction, value); }
    }

    public string CalculatedInstructionLength
    {
        get { return _calculatedInstructionLength; }
        set { SetAndRaise(CalculatedInstructionLengthProperty, ref _calculatedInstructionLength, value); }
    }

    public Thickness InnerMargin
    {
        get { return _innerMargin; }
        set { SetAndRaise(InnerMarginProperty, ref _innerMargin, value); }
    }

    public InstructionDisplayControl()
    {
        this.WhenAnyValue(x => x.Instruction, x => x.ToNextInstruction).Subscribe(Observer.Create<ValueTuple<CalculatedRoute.Instruction?, int?>>(
                tuple =>
                {
                    var distance = (int) double.Round(tuple.Item2 ?? tuple.Item1?.Distance ?? 0);
                    CalculatedInstructionLength = $"{distance} blk";
                }));
    }

}