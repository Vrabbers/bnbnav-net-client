using System.Collections.Generic;

namespace BnbnavNetClient.Models;
public sealed class Landmark : MapItem
{
    public readonly static Dictionary<string, string> LandmarkTypes = new()
    {
        { "aircs", "AirCS Station" },
        { "airport", "Airport" },
        { "hospital", "Hospital" },
        { "squid-transit", "Squid Transit Station" },
        { "parking", "Parking Lot" },
        { "walnut", "Walnut" },
        { "immigration-check", "Immigration Checkpoint"},
        { "tourist-attraction", "Tourist Attraction" },
        { "invisible", "Invisible" },
        { "generic-blue", "Generic Blue" },
        { "generic-green", "Generic Green" },
        { "generic-red", "Generic Red" },
        { "generic-gray", "Generic Gray" },
        { "city-hall", "City Hall" },
        { "cafe", "Coffee Shop" },
        { "shopping", "Store" },
        { "restaurant", "Restaurant" },
        { "park", "Park" },
        { "court", "Courthouse" },
        { "bank", "Bank" },
        { "embassy", "Embassy" },
        { "postal-office", "Post Office" },
        { "hotel", "Hotel" },
        { "frivoloco", "FrivoloCo Chocolates" },
        { "elc", "ELC" },
        { "tesco", "TESCO" },
    };

    public Landmark(string Id, Node Node, string Name, string Type)
    {
        this.Id = Id;
        this.Node = Node;
        this.Name = Name;
        this.Type = Type;
    }

    public string Id { get; init; }
    public Node Node { get; init; }
    public string Name { get; init; }
    public string Type { get; init; }

    public void Deconstruct(out string Id, out Node Node, out string Name, out string Type)
    {
        Id = this.Id;
        Node = this.Node;
        Name = this.Name;
        Type = this.Type;
    }
}
