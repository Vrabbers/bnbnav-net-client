﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using BnbnavNetClient.Helpers;
using BnbnavNetClient.Services;
using Timer = System.Timers.Timer;

namespace BnbnavNetClient.Models;

public sealed class Player : IDisposable
{
    readonly MapService _mapService;
    readonly Timer _timer;
    readonly Mutex _lastSnapMutex = new();
    
    public string Name { get; }

    public double X { get; private set; }
    public double Y { get; private set; }
    public double Z { get; private set; }

    public Edge? SnappedEdge { get; private set; }

    public double MarkerAngle { get; private set; }

    public FormattedText? PlayerText { get; set; }

    public Point MarkerCoordinates 
    {
        get
        {
            if (SnappedEdge is null) return new(X, Z);
            
            //Find the intersection point
            var playerLine = new ExtendedLine()
            {
                Point1 = new(X, Z),
                Point2 = new(X + 1, Z)
            };
            playerLine = playerLine.SetAngle(SnappedEdge.Line.NormalLine().Angle);
            _ = playerLine.TryIntersect(SnappedEdge.Line, out var intersectionPoint);
            return intersectionPoint;
        }
    }

    public List<(DateTime, Point)> PosHistory { get; set; } = new();

    public ExtendedLine Velocity => new()
    {
        Point1 = PosHistory.Last().Item2,
        Point2 = PosHistory.First().Item2
    };

    public Player(string name, MapService mapService)
    {
        _mapService = mapService;
        Name = name;

        _timer = new(50);
        _timer.Elapsed += (_, _) =>
        {
            var targetAngle = SnappedEdge is null ? Velocity.Angle : SnappedEdge.Line.Angle;

            var line1 = new ExtendedLine()
            {
                Point1 = new(0, 0),
                Point2 = new(1, 0)
            };
            var line2 = line1 with { };
            line1.SetAngle(MarkerAngle);
            line2.SetAngle(targetAngle);

            var angleDifference = line1.AngleTo(line2);
            
            switch (angleDifference)
            {
                case < 1 or > 359:
                    MarkerAngle = targetAngle;
                    break;
                case >= 180:
                    MarkerAngle -= (360 - angleDifference) * 0.1;
                    break;
                case < 180:
                    MarkerAngle += angleDifference * 0.1;
                    break;
            }
            
            PlayerUpdateEvent?.Invoke(this, EventArgs.Empty);
        };
        _timer.Enabled = true;
    }

    public void GeneratePlayerText(FontFamily fontFamily)
    {
        PlayerText = new(Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new(fontFamily), 20, null);
    }

    public void HandlePlayerMovedEvent(PlayerMoved evt)
    {
        var newX = evt.X;
        var newY = evt.Y;
        var newZ = evt.Z;

        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (X == newX && Y == newY && Z == newZ) return;
        // ReSharper restore CompareOfFloatsByEqualityOperator

        X = newX;
        Y = newY;
        Z = newZ;
        
        PosHistory.Insert(0, (DateTime.UtcNow, new(X, Z)));
        PosHistory = PosHistory.Where((x, i) => i <= 10 || DateTime.UtcNow - x.Item1 < TimeSpan.FromMilliseconds(500)).ToList();

        if (SnappedEdge is not null)
        {
            //Ensure the snapped edge is still valid
            if (!CanSnapToEdge(SnappedEdge))
            {
                SnappedEdge = null;
            }
        }

        Task.Run(() =>
        {
            if (!_lastSnapMutex.WaitOne(0)) return;

            var shouldChangeEdge = SnappedEdge is null;
            //TODO: Also change edge if the current route contains the edge to change to or if the current route does not contain the currently snapped edge
            if (shouldChangeEdge)
            {
                //TODO: Prioritise edges that are part of the current route
                SnappedEdge = _mapService.Edges.Values.FirstOrDefault(CanSnapToEdge);
            }

            _lastSnapMutex.ReleaseMutex();
        });
    }

    private bool CanSnapToEdge(Edge edge)
    {
        if (!edge.CanSnapTo) return false;
        
        // TODO: Get the road thickness from resources somehow
        // We are not using GeoHelper because that takes into account the extra space at the end of a road
        if (edge.Line.SetLength(10).NormalLine().MoveCenter(new(X, Z)).TryIntersect(edge.Line, out _) !=
            ExtendedLine.IntersectionType.Intersects) return false;
        
        var angle = edge.Line.AngleTo(Velocity);
        if (angle > 180) angle = -360 + angle;
        return double.Abs(angle) < 45;
    }

    public void HandlePlayerGoneEvent()
    {
        _timer.Enabled = false;
    }

    public event EventHandler<EventArgs>? PlayerUpdateEvent;

    public void Dispose()
    {
        _timer.Dispose();
        _lastSnapMutex.Dispose();
    }
}
