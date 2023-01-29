using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia;
using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.Models;

public class CalculatedRoute : ReactiveObject
{
    readonly MapService _mapService;

    public CalculatedRoute(MapService mapService)
    {
        _mapService = mapService;
    }
    
    public record Instruction(Node node, Edge? from, Edge? to, double distance, Instruction.InstructionType instructionType, int? roundaboutExitNumber = null, Edge? roundaboutExit = null)
    {
        public enum InstructionType
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

        public double TurnAngle => 0;
        public double? RoundaboutExitAngle => roundaboutExit is not null ? from?.Line.AngleTo(roundaboutExit.Line) : null;

        public string HumanReadableString(int distance)
        {
            var t = AvaloniaLocator.Current.GetRequiredService<IAvaloniaI18Next>();
            return t["INSTRUCTION_DISTANCE_PROMPT", ("count", distance), ("instruction", InstructionString)];
        }
        
        public string InstructionString
        {
            get
            {
                var t = AvaloniaLocator.Current.GetRequiredService<IAvaloniaI18Next>();
                var args = new Dictionary<string, object?>();
                if (to is not null) args["road"] = TargetRoadName;
                if (roundaboutExitNumber is not null) args["exit"] = roundaboutExitNumber.ToString();

                return instructionType switch
                {
                    InstructionType.Departure => t["INSTRUCTION_DEPARTURE", args],
                    InstructionType.Arrival => t["INSTRUCTION_ARRIVAL", args],
                    InstructionType.ContinueStraight => t["INSTRUCTION_CONTINUE_STRAIGHT", args],
                    InstructionType.BearLeft => t["INSTRUCTION_BEAR_LEFT", args],
                    InstructionType.TurnLeft => t["INSTRUCTION_TURN_LEFT", args],
                    InstructionType.SharpLeft => t["INSTRUCTION_SHARP_LEFT", args],
                    InstructionType.TurnAround => t["INSTRUCTION_TURN_AROUND", args],
                    InstructionType.SharpRight => t["INSTRUCTION_SHARP_RIGHT", args],
                    InstructionType.TurnRight => t["INSTRUCTION_TURN_RIGHT", args],
                    InstructionType.BearRight => t["INSTRUCTION_BEAR_RIGHT", args],
                    InstructionType.ExitLeft => t["INSTRUCTION_EXIT_LEFT", args],
                    InstructionType.ExitRight => t["INSTRUCTION_EXIT_RIGHT", args],
                    InstructionType.Merge => t["INSTRUCTION_MERGE", args],
                    InstructionType.EnterRoundabout => roundaboutExit is not null
                        ? t["INSTRUCTION_ENTER_ROUNDABOUT", args]
                        : t["INSTRUCTION_ENTER_LEAVE_ROUNDABOUT", args],
                    InstructionType.LeaveRoundabout => t["INSTRUCTION_LEAVE_ROUNDABOUT", args],
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        public string TargetRoadName => roundaboutExit?.Road.Name ?? to?.Road.Name ?? InstructionString;
    };

    List<MapItem> Elements { get; } = new();
    public IEnumerable<Node> Nodes => Elements.Where(x => x is Node).Cast<Node>();
    public IEnumerable<Edge> Edges => Elements.Where(x => x is Edge).Cast<Edge>();
    public List<Instruction> Instructions { get; } = new();
    
    [Reactive]
    public Instruction? CurrentInstruction { get; set; }

    [Reactive]
    public int BlocksToNextInstruction { get; set; }
    
    [Reactive]
    public int TotalBlocksRemaining { get; set; }

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

    private void FinaliseRoute()
    {
        //Always add a departure instruction
        Instructions.Add(new Instruction(Nodes.First(), null, Edges.First(), 0, Instruction.InstructionType.Departure));

        double currentLength = 0;
        for (var i = 2; i < Elements.Count - 2; i += 2)
        {
            var previousEdge = (Edge) Elements[i - 1];
            var node = (Node)Elements[i];
            var nextEdge = (Edge)Elements[i + 1];
            currentLength += previousEdge.Line.Length;

            var baseInstruction = new Instruction(node, previousEdge, nextEdge, currentLength,
                Instruction.InstructionType.Arrival);
                
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
                            instructionType = Instruction.InstructionType.EnterRoundabout,
                            roundaboutExitNumber = exitNumber,
                            roundaboutExit = testEdge
                        });
                        currentLength = 0;
                        break;
                    }
                }

                if (currentLength != 0)
                {
                    Instructions.Add(baseInstruction with
                    {
                        instructionType = Instruction.InstructionType.EnterRoundabout
                    });
                }
                currentLength = 0;
            } 
            else if (previousEdge.Road.RoadType == RoadType.Roundabout)
            {
                Instructions.Add(baseInstruction with
                {
                    instructionType = Instruction.InstructionType.LeaveRoundabout
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
                            instructionType = Instruction.InstructionType.ContinueStraight
                        });
                        break;
                    case < 80: //Bear Left
                        if (!isMultiRoad) continue;
                        Instructions.Add(baseInstruction with
                        {
                            instructionType = nextEdge.Road.RoadType == RoadType.Motorway ? Instruction.InstructionType.Merge : (previousEdge.Road.RoadType == RoadType.Motorway ? Instruction.InstructionType.ExitLeft : Instruction.InstructionType.BearLeft)
                        });
                        break;
                    case < 100: //Turn Left
                        if (!isMultiRoad) continue;
                        Instructions.Add(baseInstruction with
                        {
                            instructionType = Instruction.InstructionType.TurnLeft
                        });
                        break;
                    case < 160: //Sharp Left
                        Instructions.Add(baseInstruction with
                        {
                            instructionType = Instruction.InstructionType.SharpLeft
                        });
                        break;
                    case < 200: //Turn Around
                        Instructions.Add(baseInstruction with
                        {
                            instructionType = Instruction.InstructionType.TurnAround
                        });
                        break;
                    case < 260: //Sharp Right
                        Instructions.Add(baseInstruction with
                        {
                            instructionType = Instruction.InstructionType.SharpRight
                        });
                        break;
                    case < 280: //Turn Right
                        if (!isMultiRoad) continue;
                        Instructions.Add(baseInstruction with
                        {
                            instructionType = Instruction.InstructionType.TurnRight
                        });
                        break;
                    default: //Bear Right
                        if (!isMultiRoad) continue;
                        Instructions.Add(baseInstruction with
                        {
                            instructionType = nextEdge.Road.RoadType == RoadType.Motorway ? Instruction.InstructionType.Merge : (previousEdge.Road.RoadType == RoadType.Motorway ? Instruction.InstructionType.ExitRight : Instruction.InstructionType.BearRight)
                        });
                        break;
                }

                currentLength = 0;
            }
        }
        
        //Always add an arrive instruction
        Instructions.Add(new Instruction(Nodes.Last(), Edges.Last(), null, currentLength, Instruction.InstructionType.Arrival));
    }

    void UpdateCurrentInstruction()
    {
        if (_mapService.LoggedInPlayer is null)
        {
            CurrentInstruction = null;
            return;
        }

        if (!Edges.Contains(_mapService.LoggedInPlayer.SnappedEdge))
        {
            
            return;
        }

        var instructionIndex = 0;
        var instructionFound = false;
        var blocksToNextInstruction = 0.0;
        
        foreach (var edge in Edges)
        {
            if (Instructions[instructionIndex].to == edge || (Instructions[instructionIndex].to is null && edge == Edges.Last()))
            {
                instructionIndex++;
                if (instructionFound)
                {
                    BlocksToNextInstruction = (int)double.Round(blocksToNextInstruction);
                    TotalBlocksRemaining = (int) double.Round(Edges.SkipWhile(x => x != edge).Sum(x => x.Line.Length) +
                                           blocksToNextInstruction);
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
                instructionFound = true;
                blocksToNextInstruction += new ExtendedLine(edge.To.Point, _mapService.LoggedInPlayer.Point).Length;
            }
        }
        
        // We will arrive here if we are near the end of the route
        if (instructionFound)
        {
            BlocksToNextInstruction = (int)double.Round(blocksToNextInstruction);
            TotalBlocksRemaining = BlocksToNextInstruction;
        }
    }

    Player? _trackedPlayer;

    public void StartTrackingPlayer(Player player)
    {
        if (_trackedPlayer is not null) return;

        _trackedPlayer = player;
        _trackedPlayer.PlayerUpdateEvent += TrackedPlayerUpdate;
    }

    public void StopTrackingPlayer()
    {
        if (_trackedPlayer is null)
        {
            return;
        }

        _trackedPlayer.PlayerUpdateEvent -= TrackedPlayerUpdate;
        _trackedPlayer = null;
    }

    void TrackedPlayerUpdate(object? sender, EventArgs eventArgs)
    {
        UpdateCurrentInstruction();
    }
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