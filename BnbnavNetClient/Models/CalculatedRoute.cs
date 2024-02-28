using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using BnbnavNetClient.Extensions;
using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Services;
using BnbnavNetClient.Services.TextToSpeech;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace BnbnavNetClient.Models;

public sealed class CalculatedRoute : ReactiveObject, IDisposable
{
    readonly MapService _mapService;

    public event EventHandler<EventArgs>? RerouteRequested;

    public CalculatedRoute(MapService mapService)
    {
        _mapService = mapService;
    }
    
    public record Instruction(Node Node, Edge? From, Edge? To, double Distance, Instruction.InstructionTypes InstructionType, int? RoundaboutExitNumber = null, Edge? RoundaboutExit = null)
    {
        public enum InstructionTypes
        {
            Departure,
            Arrival,
            ContinueStraight,
            BearLeft,
            TurnLeft,
            SharpLeft,
            TurnAround,
            SharpRight,
            TurnRight,
            BearRight,
            ExitLeft,
            ExitRight,
            Merge,
            EnterRoundabout,
            LeaveRoundabout
        }

        public string HumanReadableString(int distance)
        {
            var t = Locator.Current.GetI18Next();
            return t["INSTRUCTION_DISTANCE_PROMPT", ("count", distance), ("instruction", InstructionString)];
        }
        
        public string InstructionString
        {
            get
            {
                var t = Locator.Current.GetI18Next();
                var args = new Dictionary<string, object?>();
                if (To is not null) args["road"] = TargetRoadName;
                if (RoundaboutExitNumber is not null) args["exit"] = RoundaboutExitNumber.Value.ToString(NumberFormatInfo.CurrentInfo);

                return InstructionType switch
                {
                    InstructionTypes.Departure => t["INSTRUCTION_DEPARTURE", args],
                    InstructionTypes.Arrival => t["INSTRUCTION_ARRIVAL", args],
                    InstructionTypes.ContinueStraight => t["INSTRUCTION_CONTINUE_STRAIGHT", args],
                    InstructionTypes.BearLeft => t["INSTRUCTION_BEAR_LEFT", args],
                    InstructionTypes.TurnLeft => t["INSTRUCTION_TURN_LEFT", args],
                    InstructionTypes.SharpLeft => t["INSTRUCTION_SHARP_LEFT", args],
                    InstructionTypes.TurnAround => t["INSTRUCTION_TURN_AROUND", args],
                    InstructionTypes.SharpRight => t["INSTRUCTION_SHARP_RIGHT", args],
                    InstructionTypes.TurnRight => t["INSTRUCTION_TURN_RIGHT", args],
                    InstructionTypes.BearRight => t["INSTRUCTION_BEAR_RIGHT", args],
                    InstructionTypes.ExitLeft => t["INSTRUCTION_EXIT_LEFT", args],
                    InstructionTypes.ExitRight => t["INSTRUCTION_EXIT_RIGHT", args],
                    InstructionTypes.Merge => t["INSTRUCTION_MERGE", args],
                    InstructionTypes.EnterRoundabout => RoundaboutExit is not null
                        ? t["INSTRUCTION_ENTER_LEAVE_ROUNDABOUT", args]
                        : t["INSTRUCTION_ENTER_ROUNDABOUT", args],
                    InstructionTypes.LeaveRoundabout => t["INSTRUCTION_LEAVE_ROUNDABOUT", args],
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        public string TargetRoadName => RoundaboutExit?.Road.Name ?? To?.Road.Name ?? InstructionString;

        public string Speech(double distance, Instruction? thenInstruction)
        {
            var t = Locator.Current.GetI18Next();
            var log = double.Log10(distance);
            var roundIncrements = (int) double.Pow(10, log) / 20;
            if (roundIncrements == 0) roundIncrements = 5;
            var blocks = (int) double.Round(distance / roundIncrements) * roundIncrements;

            switch (blocks)
            {
                case < 15:
                    var instructionString = InstructionString;
                    if (thenInstruction is not null && thenInstruction.InstructionType != InstructionTypes.LeaveRoundabout)
                        instructionString = t["INSTRUCTION_THEN", ("first", instructionString),
                            ("second", thenInstruction.InstructionString)];
                    return instructionString;
                case > 500:
                    return t["INSTRUCTION_STAY", ("count", blocks), ("road", From?.Road.Name ?? t["ROAD_UNNAMED"])];
                default:
                    return HumanReadableString(blocks);
            }
        }
    }

    public class VoicePrompt
    {
        public required Instruction Instruction { get; init; }
        public required double Distance { get; init; }
    }

    List<MapItem> Elements { get; } = new();
    public IEnumerable<Node> Nodes => Elements.Where(x => x is Node).Cast<Node>();
    public IEnumerable<Edge> Edges => Elements.Where(x => x is Edge).Cast<Edge>();
    public List<VoicePrompt> VoicePrompts { get; } = new();
    public List<Instruction> Instructions { get; } = new();
    
    public VoicePrompt? CurrentVoicePrompt { get; set; }
    
    [Reactive]
    public Instruction? CurrentInstruction { get; set; }
    
    [Reactive]
    public Instruction? ThenInstruction { get; set; }
    
    [Reactive]
    public bool DisplayThenInstruction { get; set; }

    [Reactive]
    public int BlocksToNextInstruction { get; set; }
    
    [Reactive]
    public int TotalBlocksRemaining { get; set; }
    
    [Reactive]
    public bool RouteContainsDuongWarp { get; set; }
    
    [Reactive]
    public bool RouteContainsInterWorld { get; set; }
    
    [Reactive]
    public bool RouteContainsTolls { get; set; }
    
    [Reactive]
    public bool RouteContainsFerries { get; set; }

    public bool Mute { get; set; }

    public void AddRouteSegment(Node node, Edge? edge)
    {
        Elements.Add(node);
        if (edge is not null)
        {
            Elements.Add(edge);
        }
        else
        {
            Elements.Reverse();
            FinaliseRoute();
        }
    }

    void FinaliseRoute()
    {
        RouteContainsDuongWarp = Edges.Any(x => x.Road.RoadType == RoadType.DuongWarp);
        RouteContainsInterWorld = Edges.Any(x => x.From.World != x.To.World);
        
        //Always add a departure instruction
        Instructions.Add(new Instruction(Nodes.First(), null, Edges.First(), 0, Instruction.InstructionTypes.Departure));

        double currentLength = 0;
        for (var i = 2; i < Elements.Count - 2; i += 2)
        {
            var previousEdge = (Edge) Elements[i - 1];
            var node = (Node)Elements[i];
            var nextEdge = (Edge)Elements[i + 1];

            // Only bump the length if we're staying in the same world
            // Interworld roads always have a length of 0
            if (previousEdge.From.World == previousEdge.To.World)
            {
                currentLength += previousEdge.Line.Length;
            }

            var baseInstruction = new Instruction(node, previousEdge, nextEdge, currentLength,
                Instruction.InstructionTypes.Arrival);
                
            if (nextEdge is TemporaryEdge) continue;

            //Determine if this node connects two different roads together
            var isMultiRoad = _mapService.Edges.Values.Count(x => x.To == node) !=
                _mapService.Edges.Values.Count(x => x.From == node) || _mapService.Edges.Values
                    .Where(x => x.From == node || x.To == node).Any(x => x.Road.Name != nextEdge.Road.Name);
            
            //Determine if we are turning onto the same road
            var isSameRoad = nextEdge.Road.Name == previousEdge.Road.Name;

            var turnAngle = nextEdge.Line.AngleTo(previousEdge.Line);
            if (turnAngle < 0) turnAngle += 360;
            turnAngle = 360 - turnAngle;
            if (nextEdge.Road.RoadType == RoadType.Roundabout)
            {
                //Check if we are driving through the roundabout
                if (previousEdge.Road.RoadType == RoadType.Roundabout) continue;
                
                var exitNumber = 0;
                foreach (var testEdge in Edges.SkipWhile(x => x != nextEdge))
                {
                    //Bump the exit number if, at this edge, there is at least one way out of the roundabout
                    if (_mapService.Edges.Values.Count(x => x.From == testEdge.From) > 1) 
                        exitNumber++;
                    if (testEdge.Road.RoadType != RoadType.Roundabout)
                    {
                        Instructions.Add(baseInstruction with
                        {
                            InstructionType = Instruction.InstructionTypes.EnterRoundabout,
                            RoundaboutExitNumber = exitNumber,
                            RoundaboutExit = testEdge
                        });
                        currentLength = 0;
                        break;
                    }
                }

                if (currentLength != 0)
                {
                    Instructions.Add(baseInstruction with
                    {
                        InstructionType = Instruction.InstructionTypes.EnterRoundabout
                    });
                }
                currentLength = 0;
            } 
            else if (previousEdge.Road.RoadType == RoadType.Roundabout)
            {
                Instructions.Add(baseInstruction with
                {
                    InstructionType = Instruction.InstructionTypes.LeaveRoundabout
                });
                currentLength = 0;
            }
            else
            {
                switch (turnAngle)
                {
                    case < 10 or > 350: // Continue straight
                        if (isSameRoad) continue;
                        Instructions.Add(baseInstruction with
                        {
                            InstructionType = Instruction.InstructionTypes.ContinueStraight
                        });
                        break;
                    case < 80: //Bear Left
                        if (!isMultiRoad) continue;
                        Instructions.Add(baseInstruction with
                        {
                            InstructionType = nextEdge.Road.RoadType == RoadType.Motorway ? Instruction.InstructionTypes.Merge : (previousEdge.Road.RoadType == RoadType.Motorway ? Instruction.InstructionTypes.ExitLeft : Instruction.InstructionTypes.BearLeft)
                        });
                        break;
                    case < 100: //Turn Left
                        if (!isMultiRoad) continue;
                        Instructions.Add(baseInstruction with
                        {
                            InstructionType = Instruction.InstructionTypes.TurnLeft
                        });
                        break;
                    case < 160: //Sharp Left
                        Instructions.Add(baseInstruction with
                        {
                            InstructionType = Instruction.InstructionTypes.SharpLeft
                        });
                        break;
                    case < 200: //Turn Around
                        Instructions.Add(baseInstruction with
                        {
                            InstructionType = Instruction.InstructionTypes.TurnAround
                        });
                        break;
                    case < 260: //Sharp Right
                        Instructions.Add(baseInstruction with
                        {
                            InstructionType = Instruction.InstructionTypes.SharpRight
                        });
                        break;
                    case < 280: //Turn Right
                        if (!isMultiRoad) continue;
                        Instructions.Add(baseInstruction with
                        {
                            InstructionType = Instruction.InstructionTypes.TurnRight
                        });
                        break;
                    default: //Bear Right
                        if (!isMultiRoad) continue;
                        Instructions.Add(baseInstruction with
                        {
                            InstructionType = nextEdge.Road.RoadType == RoadType.Motorway ? Instruction.InstructionTypes.Merge : (previousEdge.Road.RoadType == RoadType.Motorway ? Instruction.InstructionTypes.ExitRight : Instruction.InstructionTypes.BearRight)
                        });
                        break;
                }

                currentLength = 0;
            }
        }
        
        //Always add an arrive instruction
        Instructions.Add(new Instruction(Nodes.Last(), Edges.Last(), null, currentLength, Instruction.InstructionTypes.Arrival));

        double totalBlocks = 0;
        foreach (var instruction in Instructions.AsEnumerable().Reverse())
        {
            foreach (var targetDistance in new[]
                     {
                         10, 100, 500, instruction.Distance - 10
                     })
            {
                if (targetDistance > instruction.Distance - 10 || targetDistance < 0) continue;
                var testBlocks = totalBlocks + targetDistance;
                
                VoicePrompts.Add(new VoicePrompt
                {
                    Distance = testBlocks,
                    Instruction = instruction
                });
            }

            totalBlocks += instruction.Distance;
        }

        VoicePrompts.Reverse();
    }

    void UpdateCurrentInstruction()
    {
        lock (_currentInstructionMutex)
        {
            if (_mapService.LoggedInPlayer is null)
            {
                _ = QueueReroute();
                CurrentInstruction = null;
                ThenInstruction = null;
                DisplayThenInstruction = false;
                return;
            }

            if (!Edges.Contains(_mapService.LoggedInPlayer.SnappedEdge))
            {
                _ = QueueReroute();
                return;
            }

            CancelReroute();

            var instructionIndex = 0;
            var instructionFound = false;
            var blocksToNextInstruction = 0.0;

            foreach (var edge in Edges)
            {
                if (Instructions[instructionIndex].To == edge ||
                    (Instructions[instructionIndex].To is null && edge == Edges.Last()))
                {
                    instructionIndex++;
                    if (instructionFound)
                    {
                        BlocksToNextInstruction = (int)double.Round(blocksToNextInstruction);
                        if (instructionIndex >= Instructions.Count)
                        {
                            TotalBlocksRemaining = (int)blocksToNextInstruction;
                        }
                        else
                        {
                            TotalBlocksRemaining = (int)double.Round(
                            Instructions.SkipWhile(x => x != Instructions[instructionIndex]).Sum(x => x.Distance) +
                            blocksToNextInstruction);
                        }
                        UpdateCurrentVoicePrompt();
                        return;
                    }
                }

                if (instructionFound)
                {
                    blocksToNextInstruction += edge.Line.Length;
                }

                if (_mapService.LoggedInPlayer.SnappedEdge == edge)
                {
                    //We found the edge that the player is on
                    CurrentInstruction = Instructions[instructionIndex];
                    if (Instructions.Count > instructionIndex + 1)
                    {
                        ThenInstruction = Instructions[instructionIndex + 1];
                        DisplayThenInstruction = ThenInstruction.Distance < 15;
                    }
                    else
                    {
                        ThenInstruction = null;
                        DisplayThenInstruction = false;
                    }

                    instructionFound = true;
                    blocksToNextInstruction += new ExtendedLine(edge.To.Point, _mapService.LoggedInPlayer.Point).Length;
                }
            }

            if (instructionFound)
            {
                BlocksToNextInstruction = (int)double.Round(blocksToNextInstruction);
                TotalBlocksRemaining = BlocksToNextInstruction;
            }

            UpdateCurrentVoicePrompt();
        }
    }

    void UpdateCurrentVoicePrompt()
    {
        if (_trackedPlayer is null)
            return;

        //Update the current voice prompt if necessary
        var newPrompt = VoicePrompts.LastOrDefault(x => x.Distance > TotalBlocksRemaining);
        if (newPrompt is null || (CurrentVoicePrompt is not null &&
                                  newPrompt.Instruction == CurrentVoicePrompt.Instruction &&
                                  newPrompt.Distance >= CurrentVoicePrompt.Distance))
            return;

        CurrentVoicePrompt = newPrompt;

        if (!Mute)
        {
            var tts = Locator.Current.GetService<ITextToSpeechProvider>();
            tts?.SpeakAsync(CurrentVoicePrompt.Instruction.Speech(BlocksToNextInstruction,
                DisplayThenInstruction ? ThenInstruction : null));
        }
    }

    Player? _trackedPlayer;

    public void StartTrackingPlayer(Player player)
    {
        if (_trackedPlayer is not null) return;

        _trackedPlayer = player;
        _trackedPlayer.PlayerUpdateEvent += TrackedPlayerUpdate;
        
        UpdateCurrentInstruction();
    }

    public void StopTrackingPlayer()
    {
        if (_trackedPlayer is null)
        {
            return;
        }
        
        //Cancel any reroute in progress
        CancelReroute();

        CurrentVoicePrompt = null;

        _trackedPlayer.PlayerUpdateEvent -= TrackedPlayerUpdate;
        _trackedPlayer = null;
    }

    void TrackedPlayerUpdate(object? sender, EventArgs eventArgs)
    {
        UpdateCurrentInstruction();
    }

    CancellationTokenSource? _cancellationSource;
    readonly object _rerouteMutex = new();
    readonly object _currentInstructionMutex = new();

    async Task QueueReroute()
    {
        if (_disposed) return;

        CancellationToken cancellationToken;
        lock (_rerouteMutex)
        {
            if (_cancellationSource is not null)
            {
                return;
            }

            _cancellationSource = new CancellationTokenSource();
            cancellationToken = _cancellationSource.Token;
        }
        
        try
        {
            await Task.Delay(3000, cancellationToken);
            RerouteRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (TaskCanceledException)
        {
        }
    }

    void CancelReroute()
    {
        if (_disposed) return;
        
        lock (_rerouteMutex)
        {
            if (_cancellationSource is null)
            {
                return;
            }

            _cancellationSource.Cancel();
            _cancellationSource = null;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        
        StopTrackingPlayer();

        _cancellationSource?.Cancel();
        _cancellationSource?.Dispose();
    }

    bool _disposed;
}

public class RoutingException : Exception
{

}

public class NoSuitableEdgeException : RoutingException
{
    
}

public class DisjointNetworkException : RoutingException
{
    
}