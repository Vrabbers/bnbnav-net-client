using System.Collections.Generic;

namespace BnbnavNetClient.Models;
public sealed record Landmark(string Id, Node Node, string Name, string Type)
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
}
