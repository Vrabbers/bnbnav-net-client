﻿using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Models;

public enum LandmarkType
{
    Unknown = 0,
    City,
    Country,
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
    Tesco,
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
        LandmarkType.City => "label-city",
        LandmarkType.Country => "label-country",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static string HumanReadableName(this LandmarkType type)
    {
        var t = AvaloniaLocator.Current.GetRequiredService<IAvaloniaI18Next>();
        return type switch
        {
            LandmarkType.Unknown => "",
            LandmarkType.AirCSStation => t["LANDMARK_AIRCS"],
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
            LandmarkType.City => t["LABEL_CITY"],
            LandmarkType.Country => t["LABEL_COUNTRY"],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static bool IsLandmark(this LandmarkType type) => type != LandmarkType.Unknown && !type.IsLabel();
    public static bool IsLabel(this LandmarkType type) => type.ServerName().StartsWith("label-");
}

public sealed class Landmark : MapItem
{
    static readonly double LandmarkSize = 10;
    
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

    public Rect BoundingRect(MapView mapView)
    {
        var pos = mapView.ToScreen(new(Node.X, Node.Z));
        var rect = new Rect(
            pos.X - LandmarkSize * mapView.MapViewModel.Scale / 2,
            pos.Y - LandmarkSize * mapView.MapViewModel.Scale / 2,
            LandmarkSize * mapView.MapViewModel.Scale, LandmarkSize * mapView.MapViewModel.Scale);
        return rect;
    }
}
