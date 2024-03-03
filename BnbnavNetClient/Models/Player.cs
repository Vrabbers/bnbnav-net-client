using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using BnbnavNetClient.Extensions;
using BnbnavNetClient.Services;
using Splat;

namespace BnbnavNetClient.Models;

public sealed class Player : IDisposable, ISearchable, ILocatable
{
    readonly MapService _mapService;
    readonly DispatcherTimer _timer;
    readonly object _snapMutex = new();

    public string Name { get; }

    public string HumanReadableType
    {
        get
        {
            var t = Locator.Current.GetI18Next();
            return t["PLAYER"];
        }
    }

    public ILocatable Location => this;
    public string? IconUrl => null;

    public double Xd { get; private set; }
    public double Yd { get; private set; }
    public double Zd { get; private set; }

    public bool Moved { get; private set; } = true;

    public Edge? SnappedEdge { get; private set; }

    public double MarkerAngle { get; private set; }

    public FormattedText? PlayerText { get; set; }
    
    public event EventHandler<EventArgs>? PlayerUpdateEvent;


    public Point MarkerCoordinates
    {
        get
        {
            if (SnappedEdge is null) return new Point(Xd, Zd);

            //Find the intersection point
            var playerLine = new ExtendedLine()
            {
                Point1 = new Point(Xd, Zd),
                Point2 = new Point(Xd + 1, Zd)
            };
            playerLine = playerLine.SetAngle(SnappedEdge.Line.NormalLine().Angle);
            _ = playerLine.TryIntersect(SnappedEdge.Line, out var intersectionPoint);
            return intersectionPoint;
        }
    }

    const int PosHistorySize = 12;
    readonly Point[] _posHistory = new Point[PosHistorySize];
    int _posHistoryIx;
    DateTime _lastPosTime = DateTime.MinValue;

    ExtendedLine Velocity => new()
    {
        Point1 = _posHistory[(_posHistoryIx + 1) % PosHistorySize],
        Point2 = _posHistory[_posHistoryIx % PosHistorySize]
    };

    public Player(string name, MapService mapService)
    {
        _mapService = mapService;
        Name = name;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1.0/20.0)
        };
        _timer.Tick += TimerOnTick;
        _timer.Start();
    }

    void TimerOnTick(object? o, EventArgs eventArgs)
    {
        var targetAngle = SnappedEdge is null ? Velocity.Angle : SnappedEdge.Line.Angle;
        if (targetAngle < 0)
            targetAngle += 360;
        
        Debug.Assert(MarkerAngle is >= 0 and < 360);
        Debug.Assert(targetAngle is >= 0 and < 360);

        var angleDifference = double.Ieee754Remainder(targetAngle - MarkerAngle, 360);

        Debug.Assert(angleDifference is >= -180 and <= 180);

        if (double.Abs(angleDifference) < 0.1)
        {
            Moved = false;
            return;
        }

        var newAngle = double.Ieee754Remainder(MarkerAngle + angleDifference * 0.2, 360);
        if (newAngle < 0)
            newAngle += 360;

        MarkerAngle = newAngle;

        Debug.Assert(MarkerAngle is >= 0 and < 360);

        Moved = true;
        PlayerUpdateEvent?.Invoke(this, EventArgs.Empty);
    }

    public void GeneratePlayerText(FontFamily fontFamily)
    {
        PlayerText = new FormattedText(Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(fontFamily), 16, null);
    }

    public void HandlePlayerMovedEvent(PlayerMoved evt)
    {
        var newX = evt.X;
        var newY = evt.Y;
        var newZ = evt.Z;

        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (Xd == newX && Yd == newY && Zd == newZ)
            return;
        // ReSharper restore CompareOfFloatsByEqualityOperator

        Moved = true;

        Xd = newX;
        Yd = newY;
        Zd = newZ;

        var newPoint = new Point(newX, newZ);

        if (DateTime.Now - _lastPosTime > TimeSpan.FromMilliseconds(500))
        {
            for (var i = 0; i < PosHistorySize; i++)
            {
                _posHistory[i] = newPoint;
                _posHistoryIx = 0;
            }
        }
        else
        {
            _posHistoryIx = (_posHistoryIx + 1) % PosHistorySize;
            _posHistory[_posHistoryIx] = newPoint;
        }

        _lastPosTime = DateTime.Now;

        if (SnappedEdge is not null && !CanSnapToEdge(SnappedEdge))
        {
            //Ensure the snapped edge is still valid
            SnappedEdge = null;
        }

        World = evt.World;
    }

    public void StartCalculateSnappedEdge()
    {
        Task.Factory.StartNew(CalculateSnappedEdge, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    static void CalculateSnappedEdge(object? obj)
    {
        var self = (Player)obj!;

        // if the lock is already taken, finish
        if (!Monitor.TryEnter(self._snapMutex))
            return;

        try
        {
            var shouldChangeEdge = self.SnappedEdge is null || (!self._mapService.CurrentRoute?.Edges.Contains(self.SnappedEdge) ?? false);

            if (!shouldChangeEdge)
                return;

            Edge? snapEdge = null;
            if (self.SnappedEdge is not null && self.CanSnapToEdge(self.SnappedEdge))
            {
                // stay snapped!
                return;
            }
            else if (self._mapService.CurrentRoute is not null)
            {
                // If we're in a route, try finding edges in the route only.
                snapEdge = self._mapService.CurrentRoute.Edges.FirstOrDefault(self.CanSnapToEdge);
            }
            else
            {
                // this is the more common and longer path (outside go mode) so avoid LINQ here
                foreach (var edge in self._mapService.Edges)
                {
                    if (!self.CanSnapToEdge(edge.Value))
                        continue;

                    snapEdge = edge.Value;
                    break;
                }
            }

            self.SnappedEdge = snapEdge;
        }
        finally
        {
            Monitor.Exit(self._snapMutex);
        }
    }

    bool CanSnapToEdge(Edge edge)
    {
        if (!edge.CanSnapTo)
            return false;

        // Make sure this edge is in the correct world
        if (edge.From.World != World && edge.To.World != World)
            return false;

        // TODO: Get the road thickness from resources somehow
        // We are not using GeoHelper because that takes into account the extra space at the end of a road
        if (edge.Line.SetLength(10).NormalLine().MoveCenter(new Point(Xd, Zd)).TryIntersect(edge.Line, out _) !=
            ExtendedLine.IntersectionType.Intersects)
            return false;

        var angle = edge.Line.AngleTo(Velocity);
        if (angle > 180) angle = -360 + angle;
        return double.Abs(angle) < 45;
    }

    public void HandlePlayerGoneEvent()
    {
        Moved = true;
        PlayerUpdateEvent?.Invoke(this, EventArgs.Empty);
        _timer.Stop();
    }
    
    public void Dispose()
    {
        _timer.Stop();
    }

    public int X => (int)double.Round(Xd);
    public int Y => (int)double.Round(Yd);
    public int Z => (int)double.Round(Zd);
    public string World { get; private set; } = "world";
    public Point Point => new(X, Z);
}