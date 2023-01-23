using System;
using System.Collections.Generic;
using System.Linq;

namespace BnbnavNetClient.Models;

public enum LandmarkType
{
    Unknown = 0,
    AirCSStation,
    Airport,
    Hospital,
    SquidTransitStation,
    ParkingLot,
    Walnut,
    ImmigrationCheckpoint,
    TouristAttraction,
    Invisible,
    GenericBlue,
    GenericGreen,
    GenericRed,
    GenericGray,
    CityHall,
    CoffeeShop,
    Store,
    Restaurant,
    Park,
    Courthouse,
    Bank,
    Embassy,
    PostOffice,
    Hotel,
    FrivoloCoChocolates,
    Elc,
    Tesco
}

public static class LandmarkTypeExtensions
{
    public static string ServerName(this LandmarkType type) => type switch
    {
        LandmarkType.Unknown => "",
        LandmarkType.AirCSStation => "aircs",
        LandmarkType.Airport => "airport",
        LandmarkType.Hospital => "hospital",
        LandmarkType.SquidTransitStation => "squid-transit",
        LandmarkType.ParkingLot => "parking",
        LandmarkType.Walnut => "walnut",
        LandmarkType.ImmigrationCheckpoint => "immigration-check",
        LandmarkType.TouristAttraction => "tourist-attraction",
        LandmarkType.Invisible => "invisible",
        LandmarkType.GenericBlue => "generic-blue",
        LandmarkType.GenericGreen => "generic-green",
        LandmarkType.GenericRed => "generic-red",
        LandmarkType.GenericGray => "generic-gray",
        LandmarkType.CityHall => "city-hall",
        LandmarkType.CoffeeShop => "cafe",
        LandmarkType.Store => "shopping",
        LandmarkType.Restaurant => "restaurant",
        LandmarkType.Park => "park",
        LandmarkType.Courthouse => "court",
        LandmarkType.Bank => "bank",
        LandmarkType.Embassy => "embassy",
        LandmarkType.PostOffice => "postal-office",
        LandmarkType.Hotel => "hotel",
        LandmarkType.FrivoloCoChocolates => "frivoloco",
        LandmarkType.Elc => "elc",
        LandmarkType.Tesco => "tesco",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}

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

    public LandmarkType LandmarkType => Enum.GetValues<LandmarkType>().FirstOrDefault(x => x.ServerName() == Type);

    public void Deconstruct(out string Id, out Node Node, out string Name, out string Type)
    {
        Id = this.Id;
        Node = this.Node;
        Name = this.Name;
        Type = this.Type;
    }
}
