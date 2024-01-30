using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using BnbnavNetClient.Extensions;
using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Views;
using Splat;

namespace BnbnavNetClient.Models;

public enum LandmarkType
{
    Unknown = 0,
    InternalTemporary,
    City,
    Country,
    AirCsStation,
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
    Tesco,
}

public static class LandmarkTypeExtensions
{
    public static string ServerName(this LandmarkType type) => type switch
    {
        LandmarkType.Unknown => "",
        LandmarkType.AirCsStation => "aircs",
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
        LandmarkType.City => "label-city",
        LandmarkType.Country => "label-country",
        LandmarkType.InternalTemporary => "internal-temporary",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static string HumanReadableName(this LandmarkType type)
    {
        var t = Locator.Current.GetI18Next();
        return type switch
        {
            LandmarkType.Unknown => "",
            LandmarkType.AirCsStation => t["LANDMARK_AIRCS"],
            LandmarkType.Airport => t["LANDMARK_AIRPORT"],
            LandmarkType.Hospital => t["LANDMARK_HOSPITAL"],
            LandmarkType.SquidTransitStation => t["LANDMARK_SQTR"],
            LandmarkType.ParkingLot => t["LANDMARK_PARKING"],
            LandmarkType.Walnut => t["LANDMARK_WALNUT"],
            LandmarkType.ImmigrationCheckpoint => t["LANDMARK_IMMI"],
            LandmarkType.TouristAttraction => t["LANDMARK_TOURIST_ATTRACTION"],
            LandmarkType.Invisible => t["LANDMARK_INVISIBLE"],
            LandmarkType.GenericBlue => t["LANDMARK_GENERIC_BLUE"],
            LandmarkType.GenericGreen => t["LANDMARK_GENERIC_GREEN"],
            LandmarkType.GenericRed => t["LANDMARK_GENERIC_RED"],
            LandmarkType.GenericGray => t["LANDMARK_GENERIC_GRAY"],
            LandmarkType.CityHall => t["LANDMARK_CITY_HALL"],
            LandmarkType.CoffeeShop => t["LANDMARK_COFFEE_SHOP"],
            LandmarkType.Store => t["LANDMARK_STORE"],
            LandmarkType.Restaurant => t["LANDMARK_RESTAURANT"],
            LandmarkType.Park => t["LANDMARK_PARK"],
            LandmarkType.Courthouse => t["LANDMARK_COURTHOUSE"],
            LandmarkType.Bank => t["LANDMARK_BANK"],
            LandmarkType.Embassy => t["LANDMARK_EMBASSY"],
            LandmarkType.PostOffice => t["LANDMARK_POST_OFFICE"],
            LandmarkType.Hotel => t["LANDMARK_HOTEL"],
            LandmarkType.FrivoloCoChocolates => t["LANDMARK_FRIVOLOCO"],
            LandmarkType.Elc => t["LANDMARK_ELC"],
            LandmarkType.Tesco => t["LANDMARK_TESCO"],
            LandmarkType.InternalTemporary => t["LANDMARK_DROPPED_PIN"],
            LandmarkType.City => t["LABEL_CITY"],
            LandmarkType.Country => t["LABEL_COUNTRY"],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static string IconUrl(this LandmarkType type) => $"avares://BnbnavNetClient/Assets/Landmarks/{type.ServerName()}.svg";

    public static bool IsLandmark(this LandmarkType type) => type != LandmarkType.Unknown && type != LandmarkType.InternalTemporary && !type.IsLabel();
    public static bool IsLabel(this LandmarkType type) => type.ServerName().StartsWith("label-", StringComparison.InvariantCulture);
}

public class Landmark : MapItem, ISearchable
{
    static readonly double LandmarkSize = 10;
    
    public Landmark(string id, Node node, string name, string type)
    {
        this.Id = id;
        this.Node = node;
        this.Name = name;
        this.Type = type;
    }

    public string Id { get; init; }
    public Node Node { get; init; }
    public string Name { get; init; }
    public string Type { get; init; }

    public LandmarkType LandmarkType => Enum.GetValues<LandmarkType>().FirstOrDefault(x => x.ServerName() == Type);
    public string? IconUrl => LandmarkType.IconUrl();
    
    public string HumanReadableType => LandmarkType.HumanReadableName();

    public ILocatable Location => Node;

    public void Deconstruct(out string id, out Node node, out string name, out string type)
    {
        id = Id;
        node = Node;
        name = Name;
        type = Type;
    }

    public Rect BoundingRect(MapView mapView)
    {
        var pos = mapView.ToScreen(new Point(Node.X, Node.Z));
        var rect = new Rect(
            pos.X - LandmarkSize * mapView.MapViewModel.Scale / 2,
            pos.Y - LandmarkSize * mapView.MapViewModel.Scale / 2,
            LandmarkSize * mapView.MapViewModel.Scale, LandmarkSize * mapView.MapViewModel.Scale);
        return rect;
    }
}

public partial class TemporaryLandmark : Landmark
{
    [GeneratedRegex(@"^\(?(?<x>-?\d+), ?(?<z>-?\d+)\)?$", RegexOptions.CultureInvariant)]
    private static partial Regex CoordinateSearchRegex();

    public TemporaryLandmark(string id, Node node, string name) : base(id, node, name, "internal-temporary")
    {
    }

    public static TemporaryLandmark? ParseCoordinateString(string coordinateString, string world)
    {
        //TODO: this will probably fail if anyone tries using , as thousands separators

        var coordinateSearch = CoordinateSearchRegex().Match(coordinateString);
        if (!coordinateSearch.Success)
        {
            return null;
        }
        
        var t = Locator.Current.GetI18Next();
        var x = int.Parse(coordinateSearch.Groups["x"].Value, t.CurrentLanguage.NumberFormat);
        var z = int.Parse(coordinateSearch.Groups["z"].Value, t.CurrentLanguage.NumberFormat);
        return new TemporaryLandmark($"temp@{x},{z}", new TemporaryNode(x, 0, z, world), t["DROPPED_PIN", ("x", x.ToString(t.CurrentLanguage.NumberFormat)), ("z", z.ToString(t.CurrentLanguage.NumberFormat))]);
    }
}