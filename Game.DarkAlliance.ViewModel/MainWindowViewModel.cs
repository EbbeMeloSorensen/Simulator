using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using Craft.Logging;
using Craft.Math;
using Craft.Simulation;
using Craft.Simulation.Bodies;
using Craft.Simulation.BodyStates;
using Craft.Simulation.Boundaries;
using Craft.Simulation.Engine;
using Craft.Simulation.Props;
using Craft.Utils;
using Craft.Utils.Linq;
using Craft.ViewModels.Geometry2D.Reborn;
using Craft.ViewModels.Geometry2D.Reborn.GeometricModels;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Craft.ViewModels.Simulation;
using Game.DarkAlliance.ViewModel.Bodies;
using Game.DarkAlliance.ViewModel.Presentation_Infrastructure;
using Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SiteComponents;
using Barrier = Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SiteComponents.Barrier;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace Game.DarkAlliance.ViewModel
{
    public class MainWindowViewModel : ViewModelBase, IFrameAware
    {
        private ISiteRenderer _siteRenderer;
        private ILogger _logger;

        private DispatcherTimer _timer;
        private double _angle = 0;
        private Scene _scene;

        private SceneViewController _sceneViewController;
        private GeometryDataStore _geometryDataStore;
        private Point3D _cameraPosition;
        private Point3D _playerLightPosition;
        private Vector3D _lookDirection;
        private Vector3D _directionalLight1;
        private Vector3D _directionalLight2;
        private string _outcome;

        public Point3D CameraPosition
        {
            get => _cameraPosition;
            set
            {
                _cameraPosition = value;
                RaisePropertyChanged();
            }
        }

        public Point3D PlayerLightPosition
        {
            get => _playerLightPosition;
            set
            {
                _playerLightPosition = value;
                RaisePropertyChanged();
            }
        }

        public Vector3D LookDirection
        {
            get => _lookDirection;
            set
            {
                _lookDirection = value;
                RaisePropertyChanged();
            }
        }

        public Vector3D DirectionalLight1
        {
            get => _directionalLight1;
            set
            {
                _directionalLight1 = value;
                RaisePropertyChanged();
            }
        }

        public Vector3D DirectionalLight2
        {
            get => _directionalLight2;
            set
            {
                _directionalLight2 = value;
                RaisePropertyChanged();
            }
        }

        public string Outcome
        {
            get { return _outcome; }
            set
            {
                _outcome = value;
                RaisePropertyChanged();
            }
        }

        private Model3D _scene3D;

        public Model3D Scene3D
        {
            get => _scene3D;
            private set
            {
                _scene3D = value;
                RaisePropertyChanged();
            }
        }

        public Engine Engine { get; }

        public GeometryEditorViewModel GeometryEditorViewModel { get; }
        public GeometryViewModel GeometryViewModel { get; }

        public MainWindowViewModel(
            ILogger logger)
        {
            _siteRenderer = new SiteRenderer();
            _logger = logger;

            Engine = new Engine(_logger);

            Engine.AnimationCompleted += (s, e) =>
            {
                Outcome = Engine.EngineCore.Outcome as string;
            };

            GeometryEditorViewModel = new GeometryEditorViewModel()
            {
                // Dette gør vi i stedet i OnFrame, fordi vi er i gang med at sadle om til Reborn
                //UpdateModelCallBack = Engine.UpdateModel
            };

            GeometryViewModel = new GeometryViewModel()
            {
                ShowCoordinateSystem = false,
                LockAspectRatio = true,
                DampFocusShifts = false
            };

            GeometryViewModel.PropertyChanged += GeometryViewModel_PropertyChanged;
            Engine.CurrentStateChanged += Engine_CurrentStateChanged;

            // Bemærk de følgende 2 callbacks, som bruges til at give bodies et andet skin end det, som er default.
            // Pågældende skin er taget fra Craft, men man kan også override, ligesom det er gjort for Flappybird, Rocket og Zelda
            ShapeSelectorCallback shapeSelectorCallback = (bs) =>
            {
                if (!(bs.Body is CircularBody))
                {
                    throw new InvalidOperationException();
                }

                var circularBody = bs.Body as CircularBody;

                switch (bs)
                {
                    case BodyStateClassic bsc:
                    {
                        var orientation = bsc.Orientation;

                        return new RotatableEllipseViewModel
                        {
                            Width = 2 * circularBody.Radius,
                            Height = 2 * circularBody.Radius,
                            Orientation = orientation
                        };
                    }
                    case BodyState:
                    {
                        return new EllipseViewModel
                        {
                            Width = 2 * circularBody.Radius,
                            Height = 2 * circularBody.Radius,
                        };
                    }
                    default:
                    {
                        throw new NotSupportedException();
                    }
                }
            };

            ShapeUpdateCallback shapeUpdateCallback = (shapeViewModel, bs) =>
            {
                // Her opdaterer vi POSITIONEN af shapeviewmodellen
                shapeViewModel.Point = new PointD(bs.Position.X, bs.Position.Y);

                // Her opdaterer vi ORIENTERINGEN af shapeviewmodellen
                if (shapeViewModel is RotatableEllipseViewModel)
                {
                    var bsc = bs as BodyStateClassic;
                    var orientation = bsc == null ? 0 : bsc.Orientation;

                    var rotatableEllipseViewModel = shapeViewModel as RotatableEllipseViewModel;
                    rotatableEllipseViewModel.Orientation = orientation;
                }
            };

            _sceneViewController = new SceneViewController(
                Engine,
                GeometryEditorViewModel,
                shapeSelectorCallback,
                shapeUpdateCallback);

            // For a start, just hardcode a scene. Later, we will read this from some data source.
            var sceneDefinition = new SiteSpecs();
            Scene3D = _siteRenderer.Build(sceneDefinition);
            _scene = GenerateScene(sceneDefinition);
            _scene.InitializeBoundaryDataStore();

            GeometryEditorViewModel.InitializeWorldWindow(
                _scene.InitialWorldWindowFocus(),
                _scene.InitialWorldWindowSize(),
                false);

            _sceneViewController.ActiveScene = _scene;

            // Prepare the geometry data store, i.e the MxCifQuadTree based repository of geometric objects
            var staticGeometryObjects = new List<object>();

            _scene.Boundaries.ForEach(boundary =>
            {
                if (!boundary.Visible) return;

                switch (boundary)
                {
                    case HorizontalLineSegment horizontalLineSegment:
                        staticGeometryObjects.Add(new LineModel
                        {
                            P1 = new Point(horizontalLineSegment.X0, horizontalLineSegment.Y),
                            P2 = new Point(horizontalLineSegment.X1, horizontalLineSegment.Y)
                        });
                        break;
                    case VerticalLineSegment verticalLineSegment:
                        staticGeometryObjects.Add(new LineModel
                        {
                            P1 = new Point(verticalLineSegment.X, verticalLineSegment.Y0),
                            P2 = new Point(verticalLineSegment.X, verticalLineSegment.Y1)
                        });
                        break;
                    case LineSegment lineSegment:
                        staticGeometryObjects.Add(new LineModel
                        {
                            P1 = new Point(lineSegment.Point1.X, lineSegment.Point1.Y),
                            P2 = new Point(lineSegment.Point2.X, lineSegment.Point2.Y)
                        });
                        break;
                    case BoundaryPoint boundaryPoint:
                        staticGeometryObjects.Add(new PointModel()
                        {
                            P = new Point(boundaryPoint.Point.X, boundaryPoint.Point.Y)
                        });
                        break;
                    case CircularBoundary circularBoundary:
                        staticGeometryObjects.Add(new CircleModel()
                        {
                            Center = new Point(circularBoundary.Center.X, circularBoundary.Center.Y),
                            Radius = circularBoundary.Radius
                        });
                        break;
                    default:
                        throw new ArgumentException();
                }
            });

            var boundingBoxes = staticGeometryObjects.Select(geometryObject =>
            {
                return geometryObject switch
                {
                    LineModel line => line.ComputeBoundingBox(),
                    PointModel point => point.ComputeBoundingBox(),
                    CircleModel circle => circle.ComputeBoundingBox(),
                    _ => throw new InvalidOperationException(),
                };
            });

            _geometryDataStore = new GeometryDataStore(
                new Craft.DataStructures.Geometry.BoundingBox(
                    boundingBoxes.Min(b => b.MinX),
                    boundingBoxes.Max(b => b.MaxX),
                    boundingBoxes.Min(b => b.MinY),
                    boundingBoxes.Max(b => b.MaxY)));

            staticGeometryObjects.ForEach(_geometryDataStore.AddStaticGeometryObject);

            Engine.CurrentStateChanged += (s, e) =>
            {
                var bodyStateOfProtagonist = e.State.BodyStates.First() as BodyStateClassic;
                var position = bodyStateOfProtagonist.Position;
                var orientation = bodyStateOfProtagonist.Orientation;

                CameraPosition = new Point3D(
                    -position.Y,
                    0.5,
                    position.X);

                LookDirection = new Vector3D(Math.Sin(orientation), 0, Math.Cos(orientation));
                DirectionalLight1 = LookDirection + new Vector3D(0, -0.5, 0);
                PlayerLightPosition = CameraPosition + LookDirection * 3 + new Vector3D(0, -1, 0);
            };
        }

        public void HandleLoaded()
        {
            var initialWorldWindowFocus = _scene.InitialWorldWindowFocus();
            var initialWorldWindowSize = _scene.InitialWorldWindowSize();

            GeometryViewModel.RequestedWorldWindow = new Craft.DataStructures.Geometry.BoundingBox(
                initialWorldWindowFocus.X - initialWorldWindowSize.Width / 2,
                initialWorldWindowFocus.X + initialWorldWindowSize.Width / 2,
                initialWorldWindowFocus.Y - initialWorldWindowSize.Height / 2,
                initialWorldWindowFocus.Y + initialWorldWindowSize.Height / 2);

            UpdateStaticGeometricObjects();
            UpdateGeometricObjects(_scene.InitialState);

            if (_scene.ViewMode == SceneViewMode.FocusOnFirstBody)
            {
                UpdateFocus(_scene.InitialState.BodyStates.First().Position);
            }

            Engine.StartOrResumeAnimation();
        }

        public void OnFrame(
            TimeSpan time,
            double dt)
        {
            // Bemærk, at man ikke bruger parametrene her
            Engine.UpdateModel();
        }

        private Scene GenerateScene(
            SiteSpecs siteSpecs)
        {
            var ballRadius = 0.1;
            var initialBallPosition = new Vector2D(1.5, 0);

            var initialState = new State();

            // Add the player to the scene
            initialState.AddBodyState(
                new BodyStateClassic(new Player(1, ballRadius, 1, false), initialBallPosition)
                {
                    Orientation = Math.PI
                });

            var name = "Exploration";
            var standardGravity = 0.0;
            var initialWorldWindowUpperLeft = new Point2D(-1.4, -1.3);
            var initialWorldWindowLowerRight = new Point2D(5, 3);
            var gravitationalConstant = 0.0;
            var coefficientOfFriction = 0.0;
            var timeFactor = 1.0;
            var handleBoundaryCollisions = true;
            var handleBodyCollisions = false;
            var deltaT = 0.001;
            var viewMode = SceneViewMode.FocusOnFirstBody;

            var scene = new Scene(
                name,
                initialWorldWindowUpperLeft,
                initialWorldWindowLowerRight,
                initialState,
                standardGravity,
                gravitationalConstant,
                coefficientOfFriction,
                timeFactor,
                handleBoundaryCollisions,
                handleBodyCollisions,
                deltaT,
                viewMode);

            var shapeId = 2;

            siteSpecs.SiteComponents.ToList().ForEach(scenePart =>
            {
                switch (scenePart)
                {
                    case Barrier barrier:
                    {
                        barrier.BoundaryPoints.AdjacentPairs().ToList().ForEach(_ =>
                        {
                            scene.AddBoundary(new LineSegment(
                                _.Item1,
                                _.Item2));
                        });
                        break;
                    }
                    case Presentation_Infrastructure.SiteComponents.NPC npc:
                    {
                        var npcRadius = 0.1;

                        scene.AddBoundary(new CircularBoundary(new Vector2D(
                            npc.Position.Z, -npc.Position.X), npcRadius, npc.Tag));

                        scene.Props.Add(new PropCircle(shapeId++, npcRadius * 2, new Vector2D(
                            npc.Position.Z, -npc.Position.X)));

                        break;
                    }
                    case Barrel barrel:
                    {
                        var barrelRadius = 0.2;

                        scene.AddBoundary(new CircularBoundary(new Vector2D(
                            barrel.Position.Z, -barrel.Position.X), barrelRadius));

                        scene.Props.Add(new PropCircle(shapeId++, barrelRadius * 2, new Vector2D(
                            barrel.Position.Z, -barrel.Position.X)));

                        break;
                    }
                }
            });

            // Denne callback returnerer en værdi, der angiver, hvad der skal ske, når en body kolliderer med en boundary
            scene.CollisionBetweenBodyAndBoundaryOccuredCallBack = _ => OutcomeOfCollisionBetweenBodyAndBoundary.Block;

            var spaceKeyWasPressed = false;

            scene.InteractionCallBack = (keyboardState, keyboardEvents, mouseClickPosition, collisions, currentState) =>
            {
                spaceKeyWasPressed = keyboardEvents.SpaceDown && keyboardState.SpaceDown;

                var currentStateOfMainBody = currentState.BodyStates.First() as BodyStateClassic;
                var currentRotationalSpeed = currentStateOfMainBody.RotationalSpeed;
                var currentArtificialSpeed = currentStateOfMainBody.ArtificialVelocity.Length;

                var newRotationalSpeed = 0.0;

                if (keyboardState.LeftArrowDown)
                {
                    newRotationalSpeed += Math.PI;
                }

                if (keyboardState.RightArrowDown)
                {
                    newRotationalSpeed -= Math.PI;
                }

                var newArtificialSpeed = 0.0;

                if (keyboardState.UpArrowDown)
                {
                    newArtificialSpeed += 1.5;
                }

                if (keyboardState.DownArrowDown)
                {
                    newArtificialSpeed -= 1.5;
                }

                currentStateOfMainBody.RotationalSpeed = newRotationalSpeed;
                currentStateOfMainBody.ArtificialVelocity = new Vector2D(newArtificialSpeed, 0);

                if (Math.Abs(newRotationalSpeed - currentRotationalSpeed) < 0.01 &&
                    Math.Abs(newArtificialSpeed - currentArtificialSpeed) < 0.01)
                {
                    return false;
                }

                return true;
            };

            var nextProbeId = 1000;
            var bodyDisposalMap = new Dictionary<int, int>();

            // Denne callback kales, når tilstanden er propageret.
            // Her kan man manipulere den propagerede tilstand, f.eks. ved at fjerne eller tilføje bodies
            scene.PostPropagationCallBack = (propagatedState, boundaryCollisionReports, bodyCollisionReports) =>
            {
                // Remove probe
                if (bodyDisposalMap.ContainsKey(propagatedState.Index))
                {
                    var projectile = propagatedState.TryGetBodyState(bodyDisposalMap[propagatedState.Index]);
                    propagatedState?.RemoveBodyState(projectile);
                }

                if (spaceKeyWasPressed)
                {
                    spaceKeyWasPressed = false;

                    bodyDisposalMap[propagatedState.Index + 100] = nextProbeId;

                    var protagonist = propagatedState.BodyStates.First() as BodyStateClassic;

                    var lookDirection = new Vector2D(
                        Math.Cos(protagonist!.Orientation),
                        -Math.Sin(protagonist!.Orientation));

                    propagatedState.AddBodyState(new BodyState(
                        new Probe(nextProbeId, 0.05, 1, false, true), protagonist!.Position)
                    {
                        NaturalVelocity = 3.0 * lookDirection
                    });

                    nextProbeId++;
                }

                var response = new PostPropagationResponse();

                if (boundaryCollisionReports.Any())
                {
                    var bcrWithTag = boundaryCollisionReports.FirstOrDefault(
                        _ => _.BodyState.Body is Probe && _.Boundary.Tag != null);

                    if (bcrWithTag != null)
                    {
                        response.Outcome = bcrWithTag.Boundary.Tag;
                        response.IndexOfLastState = propagatedState.Index + 10;
                    }
                }

                return response;
            };
            
            return scene;
        }

        private void GeometryViewModel_PropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeometryViewModel.WorldWindowExpanded))
            {
                UpdateStaticGeometricObjects();
            }
        }

        private void Engine_CurrentStateChanged(
            object? sender,
            CurrentStateChangedEventArgs e)
        {
            UpdateGeometricObjects(e.State);

            if (_scene.ViewMode == SceneViewMode.FocusOnFirstBody)
            {
                UpdateFocus(e.State.BodyStates.First().Position);
            }
        }

        private void UpdateStaticGeometricObjects()
        {
            GeometryViewModel.ClearLayer(false);

            if (_geometryDataStore != null)
            {
                GeometryViewModel.AddStaticGeometryLayer(
                    _geometryDataStore.Query(GeometryViewModel.WorldWindowExpanded));
            }
        }

        private void UpdateGeometricObjects(
            State state)
        {
            var geometricObjects = state.BodyStates.Select(bs => new CircleModel
            {
                Center = new Point(bs.Position.X, bs.Position.Y),
                Radius = (bs.Body as CircularBody)!.Radius
            });

            GeometryViewModel.ReplaceDynamicGeometryLayer(geometricObjects);
        }

        private void UpdateFocus(
            Vector2D focus)
        {
            GeometryViewModel.RequestedWorldFocus = new WorldFocusRequest
            {
                WorldPoint = new Point(focus.X, focus.Y),
                ViewportRatio = new System.Windows.Size(0.5, 0.5)
            };
        }

        private void StartLightAnimation()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(30);

            var radius = 2;
            
            _timer.Tick += (s, e) =>
            {
                _angle += 0.01;
                var x = radius * Math.Cos(_angle);
                var z = radius * Math.Sin(_angle);

                PlayerLightPosition = new Point3D(x, 1, z);
            };

            _timer.Start();
        }
    }
}