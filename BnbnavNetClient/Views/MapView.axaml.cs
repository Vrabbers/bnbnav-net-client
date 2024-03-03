using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.ViewModels;
using DynamicData.Binding;
using ReactiveUI;
using System.Reactive;
using BnbnavNetClient.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using BnbnavNetClient.Models;
using BnbnavNetClient.Helpers;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Services.NetworkOperations;
using Splat;

namespace BnbnavNetClient.Views;

public partial class MapView : UserControl
{
    bool _pointerPressing;
    bool _disablePan;
    Point _pointerPrevPosition;
    Point _currentPointerPosition;
    Vector _viewVelocity = Vector.Zero;
    readonly List<Point> _pointerVelocities = [];
    // This list is averaged to get smooth panning.

    // For some reason, using the proper method, (i.e. ResourceDictionary.ThemeDictionaries) does not seem to work here.
    // This is a pretty crap solution, so if we find a better way it would probably be worthwhile implementing it
    IResourceDictionary _themeDict = default!;
    
    Matrix _toScreenMtx = Matrix.Identity;
    Matrix _toWorldMtx = Matrix.Identity;

    public MapViewModel MapViewModel => (MapViewModel)DataContext!;

    const int PlayerSize = 48;
    
    public MapView()
    {
        _i18N = Locator.Current.GetService<IAvaloniaI18Next>()!;

        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        ActualThemeVariantChanged += (_, _) =>
        {
            InvalidateVisual();
        };
        
        PointerPressed += (_, eventArgs) =>
        {
            //Disable all click events in Go Mode
            if (MapViewModel.CurrentUi == AvailableUi.Go)
            {
                return;
            }
            
            var pointerPos = eventArgs.GetPosition(this);
            var pointer = eventArgs.GetCurrentPoint(this);

            if (pointer.Properties.IsRightButtonPressed)
            {
                MapViewModel.LastRightClickHitTest = HitTest(pointerPos).ToList();
                return;
            }
            _pointerPressing = true;
            _pointerPrevPosition = pointerPos;

            _disablePan = false;

            if (MapViewModel.IsInEditMode)
            {
                var flags = MapViewModel.MapEditorService.EditController.PointerPressed(this, eventArgs);
                _disablePan = flags.HasFlag(Services.EditControllers.PointerPressed.DoNotPan);
            }
            else
            {
                _disablePan = false;
            }
             
            _viewVelocity = Vector.Zero;
            _pointerVelocities.Clear();
        };

        PointerMoved += (_, eventArgs) =>
        {
            //Disable all click events in Go Mode
            if (MapViewModel.CurrentUi == AvailableUi.Go)
            {
                return;
            }
            
            var pointerPos = eventArgs.GetPosition(this);
            _currentPointerPosition = pointerPos;
            
            if (MapViewModel.IsInEditMode) 
                MapViewModel.MapEditorService.EditController.PointerMoved(this, eventArgs);

            UpdateContextMenuItems();

            if (!_pointerPressing) return;
            if (_disablePan)
            {
                InvalidateVisual();
            } 
            else
            {
                //Turn off Follow Me
                MapViewModel.DisableFollowMe();
                    
                // We need to pan _more_ when scale is smaller:
                MapViewModel.Pan += (_pointerPrevPosition - pointerPos) / MapViewModel.Scale;
                    
                _pointerVelocities.Add(_pointerPrevPosition - pointerPos);

                if (_pointerVelocities.Count > 5)
                    _pointerVelocities.RemoveAt(0);

                var xAverage = _pointerVelocities.Average(i => i.X);
                var yAverage = _pointerVelocities.Average(i => i.Y);

                _viewVelocity = new Vector(xAverage, yAverage);

                if (double.Abs(_viewVelocity.Y) < 7 && double.Abs(_viewVelocity.Y) < 7)
                    _viewVelocity = Vector.Zero;

                // The actual view velocity should be the average of the last 5
                // computed velocities, due to how low-quality mouses work (low-quality
                // mouses have a tendency to move in angles snapped to 45 degrees).
            }
            _pointerPrevPosition = pointerPos;
        };

        PointerReleased += (_, eventArgs) =>
        {
            //Disable all click events in Go Mode
            if (MapViewModel.CurrentUi == AvailableUi.Go)
            {
                return;
            }
            
            _pointerPressing = false;

            if (MapViewModel.IsInEditMode) 
                MapViewModel.MapEditorService.EditController.PointerReleased(this, eventArgs);

            var hitTestResultsE = HitTest(eventArgs.GetPosition(this));
            var hitTestResults = hitTestResultsE as MapItem[] ?? hitTestResultsE.ToArray();

            _pointerVelocities.Clear(); // Make sure we're not using velocities from previous pan.
            
            if (!MapViewModel.IsInEditMode)
            {
                if (hitTestResults.LastOrDefault(x => x is Landmark) is Landmark landmark)
                {
                    MapViewModel.SelectedLandmark = landmark;
                }
            }
            
            TopLevel.GetTopLevel(this)?.RequestAnimationFrame(InertialPan);
        };

        PointerWheelChanged += (_, eventArgs) =>
        {
            var deltaScale = eventArgs.Delta.Y * MapViewModel.Scale / 10.0;
            Zoom(deltaScale, (eventArgs.GetPosition(this)));
            
            _viewVelocity = Vector.Zero; // Reset velocities
            _pointerVelocities.Clear();
        };

        //why does this happen to me :sob:
        MapViewModel
            .WhenAnyValue(x => x.Pan, x => x.Scale, x => x.Rotation, x => x.RotationOrigin)
            .Subscribe(Observer.Create<ValueTuple<Point, double, double, Vector>>(tuple =>
            {
                var pan = tuple.Item1;
                var scale = tuple.Item2;
                var rotate = tuple.Item3;
                var rotateOrigin = tuple.Item4;

                var matrix =
                    Matrix.CreateTranslation(-pan) *
                    Matrix.CreateScale(scale, scale);

                if (rotate != 0)
                {
                    // var centerOfBounds = new Vector(Bounds.Width, Bounds.Height) / (scale * 2);
                    var centerOfBounds = new Vector(Bounds.Width * rotateOrigin.X, Bounds.Height * rotateOrigin.Y);
                    
                    matrix *=
                        Matrix.CreateTranslation(-centerOfBounds) *
                        Matrix.CreateRotation(double.DegreesToRadians(rotate)) * 
                        Matrix.CreateTranslation(centerOfBounds);
                }

                _toScreenMtx = matrix;
                _toWorldMtx = matrix.Invert();

                UpdateDrawnItems();
            }));
        MapViewModel
            .WhenAnyPropertyChanged()
            .Subscribe(Observer.Create<MapViewModel?>(_ => { InvalidateVisual(); }));
        MapViewModel.WhenPropertyChanged(x => x.HighlightInterWorldNodesEnabled)
            .Subscribe(Observer.Create<PropertyValue<MapViewModel, bool>>(_ => UpdateDrawnItems()));
        
        MapViewModel.MapEditorService
            .WhenAnyValue(x => x.OngoingNetworkOperations)
            .Subscribe(Observer.Create<IReadOnlyList<NetworkOperation>?>(_ => Dispatcher.UIThread.Post(InvalidateVisual)));

        MapViewModel.MapService.WhenPropertyChanged(x => x.Players)
            .Subscribe(Observer.Create<PropertyValue<MapService, ReadOnlyDictionary<string, Player>>>(prop =>
            {
                if (prop.Value is null)
                    return;

                var anyMoved = false;
                
                // if any players have gone, we need to update visuals anyway, so skip all this
                if (!MapViewModel.MapService.PlayerGone)
                {
                    foreach (var (name, player) in prop.Value)
                    {
                        if ((MapViewModel.FollowMeEnabled && name == MapViewModel.LoggedInUsername) ||
                            (player.World == MapViewModel.ChosenWorld &&
                             Bounds.Intersects(GeometryHelper.SquareCenteredOn(ToScreen(player.Point), PlayerSize))))
                        {
                            if (!player.Moved)
                                continue;

                            anyMoved = true;
                            player.StartCalculateSnappedEdge(); // only do so if the player is on screen!
                        }

                    }

                    if (!anyMoved)
                        return; // If no one who is on screen moved, then don't update stuff.
                }
                else
                {
                    MapViewModel.MapService.PlayerGone = false; // reset value
                }

                InvalidateVisual();
                UpdateFollowMeState();
            }));
        MapViewModel.MapService.WhenPropertyChanged(x => x.Nodes)
            .Subscribe(Observer.Create<PropertyValue<MapService, ReadOnlyDictionary<string, Node>>>(_ => UpdateDrawnItems()));
        MapViewModel.MapService.WhenPropertyChanged(x => x.Edges)
            .Subscribe(Observer.Create<PropertyValue<MapService, ReadOnlyDictionary<string, Edge>>>(_ => UpdateDrawnItems()));
        MapViewModel.MapService.WhenPropertyChanged(x => x.Landmarks)
            .Subscribe(Observer.Create<PropertyValue<MapService, ReadOnlyDictionary<string, Landmark>>>(_ => UpdateDrawnItems()));
        MapViewModel.MapService.WhenPropertyChanged(x => x.CurrentRoute)
            .Subscribe(Observer.Create<PropertyValue<MapService, CalculatedRoute?>>(_ => UpdateDrawnItems()));
        MapViewModel.WhenAnyValue(x => x.CurrentUi).Subscribe(Observer.Create<AvailableUi>(_ =>
        {
            if (MapViewModel.CurrentUi != AvailableUi.Go)
            {
                MapViewModel.RotationOrigin = new Vector(0.5, 0.5);
                MapViewModel.Rotation = 0;
            }
        }));
        MapViewModel.WhenPropertyChanged(x => x.ChosenWorld)
            .Subscribe(Observer.Create<PropertyValue<MapViewModel, string>>(_ => UpdateDrawnItems()));

        MapViewModel.MapService.PlayerUpdateInteraction.RegisterHandler(interaction =>
        {
            interaction.SetOutput(Unit.Default);
            InvalidateVisual();
        });

        MapViewModel.WhenAnyValue(x => x.SelectedLandmark).Subscribe(Observer.Create<ISearchable?>(_ =>
        {
            if (MapViewModel.SelectedLandmark is not null) PanTo(MapViewModel.SelectedLandmark.Location.Point);
        }));
    }

    void InertialPan(TimeSpan time)
    {
        if (_viewVelocity.Length < 0.1) 
            return;
        MapViewModel.Pan += _viewVelocity / MapViewModel.Scale;
        _viewVelocity /= 1.1;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(InertialPan);
    }
    
    void UpdateContextMenuItems()
    {
        var seenEdges = new List<Edge>();
        MapViewModel.ContextMenuItems.Clear();

        if (MapViewModel.IsInEditMode)
        {
            MapViewModel.ContextMenuItems.AddRange(HitTest(_currentPointerPosition).SelectMany(x =>
            {
                switch (x)
                {
                    case Node node:
                        return new MenuItem[]
                        {
                            new()
                            {
                                Header = _i18N["NODE_DELETE"],
                                Command = ReactiveCommand.Create(() => { MapViewModel.QueueDelete(node); })
                            }
                        };
                    case Edge edge when !seenEdges.Contains(edge):
                    {
                        seenEdges.Add(edge);
                        if (MapViewModel.MapService.OppositeEdge(edge) is { } opposite)
                        {
                            seenEdges.Add(opposite);
                        }

                        return new MenuItem[]
                        {
                            new()
                            {
                                Header = _i18N["EDGE_DELETE", ("roadName", edge.Road.Name)],
                                Command = ReactiveCommand.Create(() => { MapViewModel.QueueDelete(edge); })
                            }
                        };
                    }
                    default:
                        return Enumerable.Empty<MenuItem>();
                }
            }));
        }
        else
        {
            ToWorld(_currentPointerPosition).Deconstruct(out var xd, out var zd);
            var x = (int)xd;
            var z = (int)zd;
            var landmark = new TemporaryLandmark($"temp@{x},{z}", new TemporaryNode(x, 0, z, MapViewModel.ChosenWorld), _i18N["DROPPED_PIN", ("x", x.ToString(_i18N.CurrentLanguage.NumberFormat)), ("z", z.ToString(_i18N.CurrentLanguage.NumberFormat))]);

            MapViewModel.ContextMenuItems.AddRange(new MenuItem[]
            {
                new()
                {
                    Header = _i18N["DIRECTIONS_TO_HERE"],
                    Command = ReactiveCommand.Create(() =>
                    {
                        MapViewModel.GoModeEndPoint = landmark;
                        MapViewModel.CurrentUi = AvailableUi.Prepare;
                    })
                },
                new()
                {
                    Header = _i18N["DIRECTIONS_FROM_HERE"],
                    Command = ReactiveCommand.Create(() =>
                    {
                        MapViewModel.GoModeStartPoint = landmark;
                        MapViewModel.CurrentUi = AvailableUi.Prepare;
                    })
                }
            });
        }

    }

    readonly IAvaloniaI18Next _i18N;

    List<(Point, Point, Edge)> DrawnEdges { get; set; } = [];
    List<(Rect, Landmark)> DrawnLandmarks { get; set; } = [];
    public List<(Rect, Node)> DrawnNodes { get; set; } = [];
    public List<Node> SpiedNodes { get; set; } = [];

    void UpdateFollowMeState()
    {
        var loggedInPlayer = MapViewModel.MapService.LoggedInPlayer;
        if (loggedInPlayer is null) return;
        
        if (MapViewModel.CurrentUi == AvailableUi.Go)
        {
            MapViewModel.RotationOrigin = new Vector(0.6, 0.8);
            MapViewModel.ChangeWorld(loggedInPlayer.World);
            PanTo(loggedInPlayer.MarkerCoordinates, 0.6, 0.8);
            MapViewModel.Rotation = loggedInPlayer.MarkerAngle - 90;
        }
        else if (MapViewModel.FollowMeEnabled)
        {
            MapViewModel.ChangeWorld(loggedInPlayer.World);
            PanTo(loggedInPlayer.MarkerCoordinates);
        }
    }

    void PanTo(Point worldCoords, double xOffset = 0.5, double yOffset = 0.5) => 
        MapViewModel.Pan = worldCoords - new Point(Bounds.Size.Width * xOffset, Bounds.Size.Height * yOffset) / MapViewModel.Scale;

    public IEnumerable<MapItem> HitTest(Point point)
    {
        foreach (var (rect, landmark) in DrawnLandmarks)
        {
            if (rect.Contains(point)) yield return landmark;
        }
        
        foreach (var (rect, node) in DrawnNodes)
        {
            if (rect.Contains(point)) yield return node;
        }

        foreach (var (a, b, edge) in DrawnEdges)
        {
            if (GeometryHelper.LineSegmentToPointDistance(a, b, point) <= ThicknessForRoadType(edge.Road.RoadType) * MapViewModel.Scale / 2)
                yield return edge;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateDrawnItems(new Rect(finalSize));
        return base.ArrangeOverride(finalSize);
    }

    void UpdateDrawnItems(Rect? boundsRect = null)
    {
        var mapService = MapViewModel.MapService;

        if (Bounds.Size == default && boundsRect is null)
        {
            return;
        }

        var bounds = boundsRect ?? Bounds;

        var drawnEdgesEnumerable = mapService.AllEdges
            .Where(edge => edge.From.World == edge.To.World && edge.To.World == MapViewModel.ChosenWorld).Select(edge =>
            {
                var (from, to) = edge.Extents(this);
                return (from, to, edge);
            }).Where(edge => GeometryHelper.LineIntersects(edge.from, edge.to, bounds));
        
        // This avoids re-allocating the whole entire drawn edge list, instead using the same one as before.
        DrawnEdges.Clear();
        foreach (var edge in drawnEdgesEnumerable)
        {
            DrawnEdges.Add(edge);
        }
        
        DrawnLandmarks = mapService.Landmarks.Values.Where(landmark => landmark.Node.World == MapViewModel.ChosenWorld).Select(landmark => (landmark.BoundingRect(this), landmark))
            .Where(landmark => bounds.Intersects(landmark.Item1)).ToList();

        DrawnNodes = mapService.Nodes.Values.Where(node => node.World == MapViewModel.ChosenWorld).Select(node => (node.BoundingRect(this), node))
            .Where(node => bounds.Intersects(node.Item1)).ToList();

        SpiedNodes = DrawnNodes.Select(nodeInfo => nodeInfo.Item2).Where(node =>
        {
            if (MapViewModel.HighlightInterWorldNodesEnabled)
            {
                return mapService.AllEdges.Where(edge => edge.From.Id == node.Id || edge.To.Id == node.Id).Any(edge => edge.From.World != edge.To.World);
            }
            return false;
        }).ToList();
        
        InvalidateVisual();
    }

    Pen PenForRoadType(RoadType type) => (Pen)(type switch
    {
        RoadType.Local => _themeDict["LocalRoadPen"]!,
        RoadType.Main => _themeDict["MainRoadPen"]!,
        RoadType.Highway => _themeDict["HighwayRoadPen"]!,
        RoadType.Expressway => _themeDict["ExpresswayRoadPen"]!,
        RoadType.Motorway => _themeDict["MotorwayRoadPen"]!,
        RoadType.Footpath => _themeDict["FootpathRoadPen"]!,
        RoadType.Waterway => _themeDict["WaterwayRoadPen"]!,
        RoadType.Private => _themeDict["PrivateRoadPen"]!,
        RoadType.Roundabout => _themeDict["RoundaboutRoadPen"]!,
        RoadType.DuongWarp => _themeDict["DuongWarpRoadPen"]!,
        _ => _themeDict["UnknownRoadPen"]!,
    });

    public double ThicknessForRoadType(RoadType type) => (double)(type == RoadType.Motorway ? _themeDict["MotorwayThickness"]! : _themeDict["RoadThickness"]!);

    public void DrawEdge(DrawingContext context, RoadType roadType, Point from, Point to, bool drawGhost = false, bool drawRoute = false)
    {
        var pen = drawRoute ? new Pen(new SolidColorBrush(new Color(255, 0, 150, 255)), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round) : PenForRoadType(roadType);

        var length = double.Sqrt(double.Pow(from.X - to.X, 2) + double.Pow(from.Y - to.Y, 2));
        var diffPoint = to - from;
        var angle = double.Atan2(diffPoint.Y, diffPoint.X);

        var matrix = Matrix.Identity *
                     Matrix.CreateRotation(angle) *
                     Matrix.CreateTranslation(from);

        pen.Thickness = ThicknessForRoadType(roadType) * MapViewModel.Scale;
        if (pen.Brush is LinearGradientBrush gradBrush)
        {
            gradBrush.StartPoint = new RelativePoint(0, -pen.Thickness / 2, RelativeUnit.Absolute);
            gradBrush.EndPoint = new RelativePoint(0, pen.Thickness / 2, RelativeUnit.Absolute);
        }
        
        using (context.PushTransform(matrix))
        using (context.PushOpacity(drawGhost ? 0.5 : 1))
            context.DrawLine(pen, new Point(0, 0), new Point(length, 0));
    }

    public void DrawLandmark(DrawingContext context, Landmark landmark, Rect rect)
    {
        var scale = MapViewModel.Scale;
        if (landmark.LandmarkType.IsLabel())
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            var (lowerScaleBound, higherScaleBound, size) = landmark.LandmarkType switch
            {
                LandmarkType.City => (0.3, 1.15, 60),
                LandmarkType.Country => (0, 0.3, 120),
                _ => throw new ArgumentOutOfRangeException(paramName: nameof(landmark))
            };

            if (scale < lowerScaleBound || scale > higherScaleBound) return;

            var text = new FormattedText(landmark.Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily), size * scale, (Brush)_themeDict["ForegroundBrush"]!);

            context.DrawText(text, rect.Center - new Point(text.Width / 2, text.Height / 2));
            return;
        }


        if (!(scale >= 0.8))
        {
            return;
        }
        
        context.DrawSvgUrl(landmark.IconUrl, rect);
    }
    
    public override void Render(DrawingContext context)
    {
        _themeDict = (IResourceDictionary)this.FindResource(ActualThemeVariant.ToString())!;
        
        context.FillRectangle((Brush)_themeDict["BackgroundBrush"]!, Bounds);

        var noRender = new List<MapItem>();
        noRender.AddRange(MapViewModel.MapEditorService.EditController.ItemsNotToRender);
        foreach (var operation in MapViewModel.MapEditorService.OngoingNetworkOperations)
        {
            noRender.AddRange(operation.ItemsNotToRender);
        }

        foreach (var (from, to, edge) in DrawnEdges)
        {
            if (noRender.Contains(edge)) continue;
            DrawEdge(context, edge.Road.RoadType, from, to, drawRoute: MapViewModel.MapService.CurrentRoute?.Edges.Contains(edge) ?? false);
        }

        foreach (var (rect, landmark) in DrawnLandmarks)
        {
            if (noRender.Contains(landmark)) continue;
            DrawLandmark(context, landmark, rect);
        }

        if (MapViewModel.CurrentUi == AvailableUi.Go)
        {
            //Draw the arrow indicator
            var instruction = MapViewModel.MapService.CurrentRoute?.CurrentInstruction;
            if (instruction is { From: not null, To: not null })
            {
                var pen = new Pen(new SolidColorBrush(new Color(255, 100, 50, 150)),
                    PenForRoadType(RoadType.Local).Thickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
                var poly = new PolylineGeometry(new[]
                {
                    ToScreen(instruction.From.Line.FlipDirection().SetLength(10).Point2),
                    ToScreen(instruction.Node.Point),
                    ToScreen(instruction.To.Line.SetLength(10).Point2),
                    ToScreen(instruction.To.Line.SetLength(10).FlipDirection().NudgeAngle(-45).SetLength(5).Point2),
                    ToScreen(instruction.To.Line.SetLength(10).Point2),
                    ToScreen(instruction.To.Line.SetLength(10).FlipDirection().NudgeAngle(45).SetLength(5).Point2),
                }, false);
                context.DrawGeometry(null, pen, poly);
            }
        }

        if (MapViewModel.IsInEditMode)
        {
            var nodeBorder = (Pen)_themeDict["NodeBorder"]!;
            var nodeBrush = (Brush)_themeDict["NodeFill"]!;
            var spiedBorder = (Pen)_themeDict["SpiedNodeBorder"]!;
            var spiedBrush = (Brush)_themeDict["SpiedNodeFill"]!;
            foreach (var (rect, node) in DrawnNodes)
            {
                if (noRender.Contains(node)) continue;

                if (SpiedNodes.Any(spied => spied.Id == node.Id))
                {
                    context.DrawRectangle(spiedBrush, spiedBorder, rect);
                }
                else
                {
                    context.DrawRectangle(nodeBrush, nodeBorder, rect);
                }
            }
            
            MapViewModel.MapEditorService.EditController.Render(this, context);
        }

        foreach (var operation in MapViewModel.MapEditorService.OngoingNetworkOperations)
        {
            operation.Render(this, context);
        }

        foreach (var player in MapViewModel.MapService.Players.Values
                     .Where(player => player.World == MapViewModel.ChosenWorld && Bounds.Contains(ToScreen(player.Point))))
        {
            var rect = GeometryHelper.SquareCenteredOn(ToScreen(player.MarkerCoordinates), PlayerSize);
            const string? uriString = "avares://BnbnavNetClient/Assets/playermark.svg";
            context.DrawSvgUrl(uriString, rect, -player.MarkerAngle + MapViewModel.Rotation);

            //Draw the player name
            var textBrush = (Brush)_themeDict["ForegroundBrush"]!;

            if (player.PlayerText is null)
            {
                player.GeneratePlayerText(FontFamily);
            }
            player.PlayerText!.SetForegroundBrush(textBrush);

            var textCenter = rect.Center + new Point(0, rect.Height / 2 + 8 + player.PlayerText.Height / 2);
            context.DrawText(player.PlayerText, textCenter - new Point(player.PlayerText.Width, player.PlayerText.Height) / 2);

            if (player.SnappedEdge is null)
                continue;
            
            var roadText = new FormattedText(player.SnappedEdge.Road.Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily), 16, new SolidColorBrush(new Color(255, 255, 255, 255)));

            var roadCenter = textCenter + new Point(0, player.PlayerText.Height / 2 + 8 + roadText.Height / 2);
            var roadRect = new Rect(roadCenter - new Point(roadText.Width + 10, roadText.Height) / 2,
                new Size(roadText.Width + 10, roadText.Height));
            var backingRect = roadRect.Inflate(3);
            context.DrawRectangle(new SolidColorBrush(new Color(255, 0, 120, 130)), null, backingRect,
                backingRect.Height / 2, backingRect.Height / 2);
            context.DrawText(roadText, roadCenter - new Point(roadText.Width / 2, roadText.Height / 2));
        }

        base.Render(context);
    }

    public Point ToWorld(Point screenCoords) =>
         _toWorldMtx.Transform(screenCoords);

    public Point ToScreen(Point worldCoords) =>
        _toScreenMtx.Transform(worldCoords);

    public void Zoom(double deltaScale, Point origin)
    {
        var newScale = double.Clamp(MapViewModel.Scale + deltaScale, 0.1, 20.0);
        
        var worldPrevPos = ToWorld(origin);
        MapViewModel.Scale = newScale;
        var worldFutureIncorrectPos = ToWorld(origin);
        var correction = worldFutureIncorrectPos - worldPrevPos;
        MapViewModel.Pan -= correction;
    }

    public FlyoutBase? OpenFlyout(ViewModel viewModel)
    {
        MapViewModel.FlyoutViewModel = viewModel;
        var flyout = FlyoutBase.GetAttachedFlyout(this);
        if (flyout is PopupFlyoutBase popup)
        {
            popup.ShowAt(this, showAtPointer: true);
        }
        else
        {
            flyout?.ShowAt(this);
        }

        if (viewModel is IOpenableAsFlyout ioaf) ioaf.Flyout = flyout;
        return flyout;
    }
}
