using System;

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
        _ => throw new ArgumentOutOfRangeException()
    };
}

public sealed record Road(string Id, string Name, string Type)
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
}
