﻿using BnbnavNetClient.Extensions;
using Splat;

namespace BnbnavNetClient.Models;

public enum RoadType
{
    Unknown,
    Local,
    Main,
    Highway,
    Expressway,
    Motorway,
    Footpath,
    Waterway,
    Private,
    Roundabout,
    DuongWarp
}

public static class RoadTypeExtensions
{
    public static double RoadPenalty(this RoadType type) => type switch
    {
        RoadType.Unknown => 1,
        RoadType.Local => 1,
        RoadType.Main => 0.8,
        RoadType.Highway => 0.7,
        RoadType.Expressway => 0.65,
        RoadType.Motorway => 0.6,
        RoadType.Footpath => 1.5,
        RoadType.Waterway => 1,
        RoadType.Private => 2,
        RoadType.Roundabout => 1,
        RoadType.DuongWarp => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static string HumanReadableName(this RoadType type)
    {
        var t = Locator.Current.GetI18Next();
        return type switch
        {
            RoadType.Unknown => t["ROAD_TYPE_UNKNOWN"],
            RoadType.Local => t["ROAD_TYPE_LOCAL"],
            RoadType.Main => t["ROAD_TYPE_MAIN"],
            RoadType.Highway => t["ROAD_TYPE_HIGHWAY"],
            RoadType.Expressway => t["ROAD_TYPE_EXPRESSWAY"],
            RoadType.Motorway => t["ROAD_TYPE_MOTORWAY"],
            RoadType.Footpath => t["ROAD_TYPE_FOOTPATH"],
            RoadType.Waterway => t["ROAD_TYPE_WATERWAY"],
            RoadType.Private => t["ROAD_TYPE_PRIVATE"],
            RoadType.Roundabout => t["ROAD_TYPE_ROUNDABOUT"],
            RoadType.DuongWarp => t["ROAD_TYPE_DUONGWARP"],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static string ServerName(this RoadType type) => type switch
    {
        RoadType.Unknown => "",
        RoadType.Local => "local",
        RoadType.Main => "main",
        RoadType.Highway => "highway",
        RoadType.Expressway => "expressway",
        RoadType.Motorway => "motorway",
        RoadType.Footpath => "footpath",
        RoadType.Waterway => "waterway",
        RoadType.Private => "private",
        RoadType.Roundabout => "roundabout",
        RoadType.DuongWarp => "duong-warp",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}

public class Road(string id, string name, string type)
{
    public RoadType RoadType => Type switch
    {
        "local" => RoadType.Local,
        "main" => RoadType.Main,
        "highway" => RoadType.Highway,
        "expressway" => RoadType.Expressway,
        "motorway" => RoadType.Motorway,
        "footpath" => RoadType.Footpath,
        "waterway" => RoadType.Waterway,
        "private" => RoadType.Private,
        "roundabout" => RoadType.Roundabout,
        "duong-warp" => RoadType.DuongWarp,
        _ => RoadType.Unknown
    };

    public string HumanReadableName => $"{Name} [{RoadType.HumanReadableName()}]";
    public string Id { get; protected set; } = id;
    public string Name { get; set; } = name;
    public string Type { get; set; } = type;

    public void Deconstruct(out string id, out string name, out string type)
    {
        id = Id;
        name = Name;
        type = Type;
    }
}

public class PendingRoad(string id, string name, string type) : Road(id, name, type)
{
    readonly TaskCompletionSource<string> _completionSource = new();

    public Task<string> WaitForReadyTask => _completionSource.Task;
    
    public void ProvideId(string id)
    {
        Id = id;
        _completionSource.SetResult(id);
    }

    public void SetError(Exception ex)
    {
        _completionSource.SetException(ex);
    }
}
